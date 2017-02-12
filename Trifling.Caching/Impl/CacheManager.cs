// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Impl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

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
        #region Private members

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

        #endregion Private members

        #region Constructors

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

        #endregion Constructors

        #region Common caching operations

        /// <summary>
        /// Removes a cache entry from the cache engine.
        /// </summary>
        /// <param name="key">The unique identifier of the cache entry to remove from cache.</param>
        /// <returns>If the cache entry was found and removed, then returns true. Otherwise returns false.</returns>
        public bool Remove(CacheEntryKey key)
        {
            return this._cacheEngine.Remove(key);
        }

        #endregion Common caching operations

        #region Single value caching

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

        #endregion Single value caching

        #region Set caching

        /// <summary>
        /// Caches the given enumeration of <paramref name="setItems"/> as a set in the cache.
        /// </summary>
        /// <remarks>
        /// Items of a set are not guaranteed to retain ordering when retrieved from cache. The
        /// implementation of <see cref="RetrieveSet{T}"/> returns a sorted set even if the input
        /// was not sorted.
        /// </remarks>
        /// <typeparam name="T">The type of object being cached. All items of the set must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="setItems">The individual items to store as a set.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the set was successfully created with all <paramref name="setItems"/> values cached.</returns>
        public bool CacheAsSet<T>(CacheEntryKey cacheEntryKey, IEnumerable<T> setItems, TimeSpan expiry)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var processedValues = this.ApplySerialization(setItems);

                // write the byte data to the cache engine.
                return this._cacheEngine.CacheAsSet(cacheEntryKey, processedValues, expiry);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.CacheAsSet(cacheEntryKey, setItems.Cast<IConvertible>(), expiry);
        }

        /// <summary>
        /// Adds a single new entry into an existing cached set.
        /// </summary>
        /// <typeparam name="T">The type of object being cached. All existing items of the set must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and add to.</param>
        /// <param name="value">The new individual item to store in the existing set.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be added to the cached set. Otherwise true.</returns>
        public bool AddToSet<T>(CacheEntryKey cacheEntryKey, T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var newValue = this.ApplySerializationToValue(value);

                // write the byte data to the cache engine.
                return this._cacheEngine.AddToSet(cacheEntryKey, newValue);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.AddToSet(cacheEntryKey, (IConvertible)value);
        }

        /// <summary>
        /// Removes any matching entries with the same value from an existing cached set.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing set and remove.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached set. Otherwise true.</returns>
        public bool RemoveFromSet<T>(CacheEntryKey cacheEntryKey, T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var newValue = this.ApplySerializationToValue(value);

                // remove the matching byte array data from the cache engine.
                return this._cacheEngine.RemoveFromSet(cacheEntryKey, newValue);
            }

            // for these types, the value is not serialised in the cache.
            // remove the values as-is from the cache engine.
            return this._cacheEngine.RemoveFromSet(cacheEntryKey, (IConvertible)value);
        }

        /// <summary>
        /// Fetches a stored set from the cache and returns it as a set. If the key was found then the set 
        /// is returned. If not found then a null is returned.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located set from the cache if the key was found. Otherwise null.</returns>
        public ISet<T> RetrieveSet<T>(CacheEntryKey cacheEntryKey)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                var retrievedSet = this._cacheEngine.RetrieveSet(cacheEntryKey);

                // if the values were compressed and serialised then we must reverse that.
                return new SortedSet<T>(
                    retrievedSet.Select(this.ApplyDeserializationToValue<T>));
            }

            // for these types, the value is not serialised in the cache.
            // return the values as-is from the cache engine.
            return new SortedSet<T>(
                    this._cacheEngine.RetrieveSet(cacheEntryKey).Cast<T>());
        }

        #endregion Set caching

        #region List caching

        /// <summary>
        /// Caches the given enumeration of <paramref name="listItems"/> values as a list in the cache.
        /// </summary>
        /// <typeparam name="T">The type of object being cached. All items of the list must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="listItems">The individual items to store as a list.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the list was successfully created with all <paramref name="listItems"/> values cached.</returns>
        public bool CacheAsList<T>(CacheEntryKey cacheEntryKey, IEnumerable<T> listItems, TimeSpan expiry)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var processedValues = this.ApplySerialization(listItems);

                // write the byte data to the cache engine.
                return this._cacheEngine.CacheAsList(cacheEntryKey, processedValues, expiry);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.CacheAsList(cacheEntryKey, listItems.Cast<IConvertible>(), expiry);
        }

        /// <summary>
        /// Fetches a stored list from the cache and returns it as a <see cref="IList{T}"/>. If the key was 
        /// found then the list is returned. If not found then a null is returned.
        /// </summary>
        /// <typeparam name="T">The type of object that was cached in a list.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located list from the cache if the key was found. Otherwise null.</returns>
        public IList<T> RetrieveList<T>(CacheEntryKey cacheEntryKey)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                var retrievedList = this._cacheEngine.RetrieveList(cacheEntryKey);

                // if the values were compressed and serialised then we must reverse that.
                return new List<T>(
                    retrievedList.Select(this.ApplyDeserializationToValue<T>));
            }

            // for these types, the value is not serialised in the cache.
            // return the values as-is from the cache engine.
            return new List<T>(
                    this._cacheEngine.RetrieveList(cacheEntryKey).Cast<T>());
        }

        /// <summary>
        /// Appends a new value to the end of an existing cached list.
        /// </summary>
        /// <typeparam name="T">The type of object being appended to the cached list. All items of the list must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that the 
        /// <paramref name="value"/> will be appended to.</param>
        /// <param name="value">The value to append to the cached list.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be appended. Otherwise true.</returns>
        public bool AppendToList<T>(CacheEntryKey cacheEntryKey, T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var newValue = this.ApplySerializationToValue(value);

                // write the byte data to the cache engine.
                return this._cacheEngine.AppendToList(cacheEntryKey, newValue);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.AppendToList(cacheEntryKey, (IConvertible)value);
        }

        /// <summary>
        /// Truncates values from the cached list so that only the values in the range specified remain.
        /// </summary>
        /// <example>
        /// <para>To remove the first two entries, specify <paramref name="firstIndexKept"/>=2 and <paramref name="lastIndexKept"/>=-1.</para>
        /// <para>To remove the last five entries, specify <paramref name="firstIndexKept"/>=0 and <paramref name="lastIndexKept"/>=-6.</para>
        /// <para>To remove the first and last entries, specify <paramref name="firstIndexKept"/>=1 and <paramref name="lastIndexKept"/>=-2.</para>
        /// </example>
        /// <param name="cacheEntryKey">The unique key of the cached list to attempt to shrink.</param>
        /// <param name="firstIndexKept">The zero-based value of the first value from the list that must be kept. Negative 
        /// values refer to the position from the end of the list (i.e. -1 is the last list entry and -2 is the second last entry).</param>
        /// <param name="lastIndexKept">The zero-based value of the last value from the list that must be kept. Negative 
        /// values refer to the position from the end of the list (i.e. -1 is the last list entry and -2 is the second last entry).</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the list cannot be shrunk. Otherwise true.</returns>
        public bool ShrinkList(CacheEntryKey cacheEntryKey, long firstIndexKept, long lastIndexKept)
        {
            return this._cacheEngine.ShrinkList(cacheEntryKey, firstIndexKept, lastIndexKept);
        }

        /// <summary>
        /// Removes any matching entries with the same value from an existing cached list.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached list.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cached list to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing list and remove.</param>
        /// <returns>Returns -1 list doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached list. Otherwise returns the number of removed items.</returns>
        public long RemoveFromList<T>(CacheEntryKey cacheEntryKey, T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var newValue = this.ApplySerializationToValue(value);

                // remove the matching byte array data from the cache engine.
                return this._cacheEngine.RemoveFromList(cacheEntryKey, newValue);
            }

            // for these types, the value is not serialised in the cache.
            // remove the values as-is from the cache engine.
            return this._cacheEngine.RemoveFromList(cacheEntryKey, (IConvertible)value);
        }

        /// <summary>
        /// Removes all items from an existing cached list.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that must be cleared.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the list cannot be cleared. Otherwise true.</returns>
        public bool ClearList(CacheEntryKey cacheEntryKey)
        {
            return this._cacheEngine.ClearList(cacheEntryKey);
        }

        #endregion List caching

        #region Dictionary caching

        /// <summary>
        /// Caches the given dictionary of items as a new dictionary in the cache engine.
        /// </summary>
        /// <typeparam name="T">The type of object being cached. All values of the dictionary must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which will contain the dictionary.</param>
        /// <param name="dictionaryItems">The items to cache as a dictionary.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the dictionary was successfully created with all <paramref name="dictionaryItems"/> values cached.</returns>
        public bool CacheAsDictionary<T>(CacheEntryKey cacheEntryKey, IDictionary<string, T> dictionaryItems, TimeSpan expiry)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var processedValues = this.ApplySerialization(dictionaryItems);

                // write the byte data to the cache engine.
                return this._cacheEngine.CacheAsDictionary(cacheEntryKey, processedValues, expiry);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.CacheAsDictionary(cacheEntryKey, dictionaryItems.ToDictionary(x => x.Key, x => (IConvertible)x.Value), expiry);
        }

        /// <summary>
        /// Adds a new dictionary entry for the given value into an existing cached dictionary with 
        /// the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <typeparam name="T">The type of object being added to the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary that the 
        /// <paramref name="value"/> will be added to.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being added.</param>
        /// <param name="value">The value to add into the cached dictionary.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be added. Otherwise true.</returns>
        public bool AddToDictionary<T>(CacheEntryKey cacheEntryKey, string dictionaryKey, T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var newValue = this.ApplySerializationToValue(value);

                // write the byte data to the cache engine.
                return this._cacheEngine.AddToDictionary(cacheEntryKey, dictionaryKey, newValue);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.AddToDictionary(cacheEntryKey, dictionaryKey, (IConvertible)value);
        }

        /// <summary>
        /// Updates an existing dictionary entry with the given value in an existing cached dictionary for 
        /// the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <typeparam name="T">The type of object being updated in the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being updated.</param>
        /// <param name="value">The value to update in the cached dictionary.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the <paramref name="dictionaryKey"/> cannot
        /// be found or the value cannot be updated. Otherwise true.</returns>
        public bool UpdateDictionaryEntry<T>(CacheEntryKey cacheEntryKey, string dictionaryKey, T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var newValue = this.ApplySerializationToValue(value);

                // write the byte data to the cache engine.
                return this._cacheEngine.UpdateDictionaryEntry(cacheEntryKey, dictionaryKey, newValue);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.UpdateDictionaryEntry(cacheEntryKey, dictionaryKey, (IConvertible)value);
        }

        /// <summary>
        /// Removes a dictionary entry from an existing cached dictionary for the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being removed.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the <paramref name="dictionaryKey"/> cannot
        /// be removed. Otherwise true.</returns>
        public bool RemoveFromDictionary(CacheEntryKey cacheEntryKey, string dictionaryKey)
        {
            return this._cacheEngine.RemoveFromDictionary(cacheEntryKey, dictionaryKey);
        }

        /// <summary>
        /// Retrieves all entries in a cached dictionary as a new <see cref="IDictionary{TKey, TValue}"/>. 
        /// </summary>
        /// <typeparam name="T">The type of object which was written in the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <returns>Returns the located dictionary from the cache if the key was found. Otherwise null.</returns>
        public IDictionary<string, T> RetrieveDictionary<T>(CacheEntryKey cacheEntryKey)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                var retrievedByteArrayDictionary = this._cacheEngine.RetrieveDictionary(cacheEntryKey);

                // if the values were compressed and serialised then we must reverse that.
                return retrievedByteArrayDictionary
                    .ToDictionary(x => x.Key, x => this.ApplyDeserializationToValue<T>(x.Value));
            }

            // for these types, the value is not serialised in the cache.
            // return the values as-is from the cache engine.
            var retrievedDictionary = this._cacheEngine.RetrieveDictionary<IConvertible>(cacheEntryKey);
            return retrievedDictionary
                .ToDictionary(x => x.Key, x => (T)x.Value);
        }

        /// <summary>
        /// Retrieves a single entry from a cached dictionary located by the <paramref name="dictionaryKey"/>. 
        /// </summary>
        /// <typeparam name="T">The type of object which was written in the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being sought.</param>
        /// <param name="value">Returns the value found in the dictionary cache. If not found the default value is returned.</param>
        /// <returns>Returns true if the value was located in the cached dictionary. Otherwise false.</returns>
        public bool RetrieveDictionaryEntry<T>(CacheEntryKey cacheEntryKey, string dictionaryKey, out T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                byte[] retrievedBytes;
                if (!this._cacheEngine.RetrieveDictionaryEntry(cacheEntryKey, dictionaryKey, out retrievedBytes))
                {
                    // value could not be read.
                    value = default(T);
                    return false;
                }

                // if the value was compressed and serialised then we must reverse that.
                value = this.ApplyDeserializationToValue<T>(retrievedBytes);
                return true;
            }

            // for these types, the value is not serialised in the cache.
            IConvertible retrievedValue;
            if (!this._cacheEngine.RetrieveDictionaryEntry(cacheEntryKey, dictionaryKey, out retrievedValue))
            {
                value = default(T);
                return false;
            }

            // return the values as-is from the cache engine.
            value = (T)retrievedValue;
            return true;
        }

        #endregion Dictionary caching

        #region Queue caching

        /// <summary>
        /// Caches the given enumeration of <paramref name="queuedItems"/> values as a queue in the cache.
        /// </summary>
        /// <typeparam name="T">The type of object being cached. All items of the queue must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="queuedItems">The individual items to store as a queue.</param>
        /// <param name="expiry">The time period that the data will be valid. May be set to never expire by setting <see cref="TimeSpan.MaxValue"/>.</param>
        /// <returns>Returns true if the queue was successfully created with all <paramref name="queuedItems"/> values cached.</returns>
        public bool CacheAsQueue<T>(CacheEntryKey cacheEntryKey, IEnumerable<T> queuedItems, TimeSpan expiry)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var processedValues = this.ApplySerialization(queuedItems);

                // write the byte data to the cache engine.
                return this._cacheEngine.CacheAsQueue(cacheEntryKey, processedValues, expiry);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.CacheAsQueue(cacheEntryKey, queuedItems.Cast<IConvertible>(), expiry);
        }

        /// <summary>
        /// Pushes a new value to the end of an existing cached queue.
        /// </summary>
        /// <typeparam name="T">The type of object being pushed to the cached queue. All items of the queue must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">The value to append to the cached queue.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be pushed to the queue. Otherwise true.</returns>
        public bool PushQueue<T>(CacheEntryKey cacheEntryKey, T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                // pre-process this data by applying serialisation and compression (if configured).
                var processedValues = this.ApplySerializationToValue(value);

                // write the byte data to the cache engine.
                return this._cacheEngine.PushQueue(cacheEntryKey, processedValues);
            }

            // for these types, the value is not serialised in the cache.
            // write the values as-is to the cache engine.
            return this._cacheEngine.PushQueue(cacheEntryKey, (IConvertible)value);
        }

        /// <summary>
        /// Pops the next value in the cached queue and returns the value.
        /// </summary>
        /// <typeparam name="T">The type of the objects stored in the cached queue. All items of the queue must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">Returns the next value from the cached queue. If not found then a default value is returned.</param>
        /// <returns>Returns true if the next value in the cached queue was successfully returned in <paramref name="value"/>. Otherwise false.</returns>
        public bool PopQueue<T>(CacheEntryKey cacheEntryKey, out T value)
        {
            var ti = typeof(T).GetTypeInfo();
            if (!ti.GetInterfaces().Contains(typeof(IConvertible)))
            {
                byte[] retrievedBytes;
                if (!this._cacheEngine.PopQueue(cacheEntryKey, out retrievedBytes))
                {
                    // value could not be read.
                    value = default(T);
                    return false;
                }

                // if the value was compressed and serialised then we must reverse that.
                value = this.ApplyDeserializationToValue<T>(retrievedBytes);
                return true;
            }

            // for these types, the value is not serialised in the cache.
            IConvertible retrievedValue;
            if (!this._cacheEngine.PopQueue(cacheEntryKey, out retrievedValue))
            {
                value = default(T);
                return false;
            }

            // return the values as-is from the cache engine.
            value = (T)retrievedValue;
            return true;
        }

        /// <summary>
        /// Removes all items from an existing cached queue.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue that must be cleared.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the queue cannot be cleared. Otherwise true.</returns>
        public bool ClearQueue(CacheEntryKey cacheEntryKey)
        {
            return this._cacheEngine.ClearQueue(cacheEntryKey);
        }

        #endregion Queue caching

        #region Private methods

        /// <summary>
        /// Applies serialisation (and optionally compression) to the values in <paramref name="items"/> and returns them.
        /// </summary>
        /// <typeparam name="T">The type of value being serialised.</typeparam>
        /// <param name="items">The values that each must be serialised separately.</param>
        /// <returns>Returns an enumeration of serialised values for each of the input <paramref name="items"/>.</returns>
        private IEnumerable<byte[]> ApplySerialization<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                yield return this.ApplySerializationToValue(item);
            }
        }

        /// <summary>
        /// Applies serialisation (and optionally compression) to the dictionary values in <paramref name="items"/>
        /// and returns a new dictionary of string keys and byte array values.
        /// </summary>
        /// <typeparam name="T">The type of value being serialised.</typeparam>
        /// <param name="items">The dictionary which must have the values serialised separately.</param>
        /// <returns>Returns new dictionary retaining the keys and providing serialised values.</returns>
        private IDictionary<string, byte[]> ApplySerialization<T>(IDictionary<string, T> items)
        {
            var newDictionary = new Dictionary<string, byte[]>(items.Count);

            foreach (var item in items)
            {
                newDictionary.Add(item.Key, this.ApplySerializationToValue(item.Value));
            }

            return newDictionary;
        }

        /// <summary>
        /// Applies serialisation (an optionally compression) to a single value.
        /// </summary>
        /// <typeparam name="T">The type of value being serialised.</typeparam>
        /// <param name="value">The value to serialise.</param>
        /// <returns>Returns a serialised byte array for the <paramref name="value"/> provided.</returns>
        private byte[] ApplySerializationToValue<T>(T value)
        {
            // if the cache manager configuration specified a compressor, then apply compression
            // before caching the value.
            if (this._compressor != null)
            {
                using (var serialisedStream = new MemoryStream())
                {
                    this._binarySerializer.SerializeToStream(value, serialisedStream);

                    using (var compressedStream = new MemoryStream())
                    {
                        this._compressor.CompressStream(serialisedStream, compressedStream);

                        return compressedStream.ToArray();
                    }
                }
            }
            else
            {
                return this._binarySerializer.Serialize<T>(value);
            }
        }

        /// <summary>
        /// Reverses previously-applied serialisation (and optionally compression) on a single value.
        /// </summary>
        /// <typeparam name="T">The type of value being deserialised.</typeparam>
        /// <param name="value">The value to deserialise.</param>
        /// <returns>Returns an instance of <typeparamref name="T"/> for the <paramref name="value"/> provided.</returns>
        private T ApplyDeserializationToValue<T>(byte[] value)
        {
            // if the cache manager configuration specified a compressor, then apply decompression
            // before deserialising the return value.
            if (this._compressor != null)
            {
                using (var compressedStream = new MemoryStream(value))
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
                return this._binarySerializer.Deserialize<T>(value);
            }
        }

        #endregion Private methods
    }
}
