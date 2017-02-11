// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Impl
{
    using System;
    using System.IO;

    using Microsoft.Extensions.Options;

    using Trifling.Caching;
    using Trifling.Caching.Interfaces;
    using Trifling.Compression.Interfaces;
    using Trifling.Serialization.Interfaces;

    /// <summary>
    /// A manager for simplifying access to a cache engine.
    /// </summary>
    public class CacheManager : ICacheManager
    {
        /// <summary>
        /// An instance of a <see cref="ICacheEngine"/> to store and retrieve cached values. 
        /// </summary>
        private readonly ICacheEngine _cacheEngine;

        /// <summary>
        /// A serialiser for creating byte arrays from objects and v
        /// </summary>
        private readonly IBinarySerializer _binarySerializer;

        /// <summary>
        /// A factory for creating instances of <see cref="ICompressor"/>.
        /// </summary>
        private readonly ICompressorFactory _compressorFactory;

        /// <summary>
        /// The configuration options for this cache manager.
        /// </summary>
        private readonly CacheManagerConfiguration _configuration;

        /// <summary>
        /// The implementation of <see cref="ICompressor"/> to use when preparing data to be written
        /// or read from the cache engine.  This can be null indicating that compression is not used.
        /// </summary>
        private readonly ICompressor _compressor;

        /// <summary>
        /// Initialises a new instance of the <see cref="CacheManager"/> class with the given dependencies. 
        /// </summary>
        /// <param name="cacheEngine">The cache engine that will perform the caching operations.</param>
        /// <param name="cacheManagerConfiguration">The configuration options of the cache manager.</param>
        /// <param name="binarySerializer">A serialiser that will convert objects to byte arrays for storage in the cache engine.</param>
        /// <param name="compressorFactory">A factory for creating instances of <see cref="ICompressor"/>.</param>
        public CacheManager(
            ICacheEngine cacheEngine, 
            IOptions<CacheManagerConfiguration> cacheManagerConfiguration,
            IBinarySerializer binarySerializer,
            ICompressorFactory compressorFactory)
        {
            this._configuration = cacheManagerConfiguration?.Value;
            this._cacheEngine = cacheEngine;
            this._cacheEngine.Initialise(cacheManagerConfiguration.Value?.CacheEngineConfiguration);
            this._binarySerializer = binarySerializer;
            this._compressorFactory = compressorFactory;

            // if the configuration stipulates that G-Zip or Deflate compression should be used, then
            // setup the running compressor as the specified type.
            if (this._configuration != null)
            {
                if (this._configuration.UseDeflateCompression)
                {
                    this._compressor =
                        this._compressorFactory
                            .Create<IDeflateCompressor>(this._configuration.CompressionConfiguration);

                    return;
                }
                else if (this._configuration.UseGzipCompression)
                {
                    this._compressor =
                        this._compressorFactory
                            .Create<IGzipCompressor>(this._configuration.CompressionConfiguration);

                    return;
                }
            }

            // if the compressor instance wasn't set above then default to null.
            this._compressor = null;
        }

        /// <summary>
        /// Caches a new cache entry or overwrites an existing cache entry with the matching <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique identifier of the cache entry to write.</param>
        /// <param name="value">The value to place in cache.</param>
        /// <param name="expiry">The time span that the cache entry will be cached before becoming eligible to 
        /// be deleted by the caching engine.</param>
        /// <returns>Returns the value that was written to cache.</returns>
        /// <typeparam name="T">The type of the value being cached.</typeparam>
        public T Cache<T>(CacheEntryKey key, T value, TimeSpan expiry)
        {
            byte[] cacheValue = null;

            // if the cache manager configuration specified a compressor, then apply compression
            // before caching the value.
            if (this._compressor != null)
            {
                using (var serialisedStream = new MemoryStream())
                {
                    this._binarySerializer.SerializeToStream<T>(value, serialisedStream);

                    using (var compressedStream = new MemoryStream())
                    {
                        this._compressor.CompressStream(serialisedStream, compressedStream);

                        cacheValue = compressedStream.ToArray();
                    }
                }
            }
            else
            {
                cacheValue = this._binarySerializer.Serialize<T>(value);
            }

            if (cacheValue == null || cacheValue.Length < 1)
            {
                // if the value being cached is empty then rather delete the current key.
                this._cacheEngine.Remove(key);
            }
            else
            {
                // write the byte data to the cache engine.
                this._cacheEngine.Cache(key, cacheValue, expiry);
            }

            // return the de-serialised clone of the given value.
            return this._binarySerializer.Deserialize<T>(cacheValue);
        }

        /// <summary>
        /// Removes a cache entry from the cache engine.
        /// </summary>
        /// <param name="key">The unique identifier of the cache entry to remove from cache.</param>
        /// <returns>If the cache entry was found and removed, then returns true. Otherwise returns false.</returns>
        public bool Remove(CacheEntryKey key)
        {
            return this._cacheEngine.Remove(key);
        }

        /// <summary>
        /// Attempts to retrieve the cached value. If not found or if expired, then uses the provided
        /// <paramref name="valueIfNotFound"/>, caches that value and returns that value.
        /// </summary>
        /// <param name="key">The unique identifier of the cache entry to retrieve or re-cache.</param>
        /// <param name="valueIfNotFound">The default value to use only if the specified cache entry key is 
        /// not found in cache or has expired.</param>
        /// <returns>Returns the value from the cache with the matching <paramref name="key"/>.</returns>
        /// <typeparam name="T">The type of the value being retrieved from cache. If the type doesn't match then an exception is thrown.</typeparam>
        public T Retrieve<T>(CacheEntryKey key, T valueIfNotFound = default(T))
        {
            var retrievedData = this._cacheEngine.Retrieve(key);

            if (retrievedData == null)
            {
                return valueIfNotFound;
            }

            // if the cache manager configuration specified a compressor, then apply compression
            // before caching the value.
            if (this._compressor != null)
            {
                using (var compressedStream = new MemoryStream(retrievedData))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        this._compressor.DecompressStream(compressedStream, decompressedStream);

                        return this._binarySerializer.DeserializeFromStream<T>(decompressedStream);
                    }
                }
            }
            else
            {
                return this._binarySerializer.Deserialize<T>(retrievedData);
            }
        }

        /// <summary>
        /// Attempts to retrieve the cached value. If not found or if expired, then executes the provided
        /// <paramref name="valueFunction"/> to get a value and caches that value and returns that value.
        /// </summary>
        /// <param name="key">The unique identifier of the cache entry to retrieve or re-cache.</param>
        /// <param name="valueFunction">The function to execute to retrieve the new value for re-caching 
        /// only if the specified cache entry key is not found in cache or has expired.</param>
        /// <param name="expiry">The time span that the re-cached entry will be cached before becoming eligible to 
        /// be deleted by the caching engine. This is only used if re-caching is needed.</param>
        /// <returns>Returns the value from the cache with the matching <paramref name="key"/>.</returns>
        /// <typeparam name="T">The type of the value being retrieved from cache. If the type doesn't match then an exception is thrown.</typeparam>
        public T RetrieveOrRecache<T>(CacheEntryKey key, Func<T> valueFunction, TimeSpan expiry)
        {
            var retrievedData = this._cacheEngine.Retrieve(key);

            if (retrievedData == null)
            {
                // the value was not cached. We must execute the call to get a value and cache that value.
                var newValue = valueFunction();

                // serialize the new value
                if (this._compressor != null)
                {
                    // compression is set-up, so compress the value before caching.
                    using (var serialisedStream = new MemoryStream())
                    {
                        this._binarySerializer.SerializeToStream<T>(newValue, serialisedStream);

                        using (var compressedStream = new MemoryStream())
                        {
                            this._compressor.CompressStream(serialisedStream, compressedStream);

                            retrievedData = compressedStream.ToArray();
                        }
                    }
                }
                else
                {
                    // no compression is defined, so serialize the value as-is.
                    retrievedData = this._binarySerializer.Serialize<T>(newValue);
                }

                // only cache the value if there is something to cache.
                if (retrievedData != null && retrievedData.Length > 0)
                {
                    this._cacheEngine.Cache(key, retrievedData, expiry);
                }

                // here ends the scenario where the cache key did not exist - it does now.
                return newValue;
            }

            // if the cache manager configuration specified a compressor, then apply de-compression
            // before returning the value.
            if (this._compressor != null)
            {
                using (var compressedStream = new MemoryStream(retrievedData))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        this._compressor.DecompressStream(compressedStream, decompressedStream);

                        return this._binarySerializer.DeserializeFromStream<T>(decompressedStream);
                    }
                }
            }
            else
            {
                return this._binarySerializer.Deserialize<T>(retrievedData);
            }
        }
    }
}
