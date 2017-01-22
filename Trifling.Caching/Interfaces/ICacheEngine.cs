// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Interfaces
{
    using System;

    /// <summary>
    /// An implementation of a caching engine.
    /// </summary>
    public interface ICacheEngine
    {
        /// <summary>
        /// Called once after initialisation to configure the new instance of the cache engine.
        /// </summary>
        /// <param name="configuration">The configuration options for the cache engine. Which properties
        /// are used from the configuration are dependent on the implementation.</param>
        void Initialise(CacheEngineConfiguration configuration);

        /// <summary>
        /// Removes a cache entry from the cache engine.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to remove from cache.</param>
        /// <returns>If the cache entry was found and removed, then returns true. Otherwise returns false.</returns>
        bool Remove(string cacheEntryKey);

        /// <summary>
        /// Retrieves the cache entry with the matching <paramref name="cacheEntryKey"/>. If no cache entry is
        /// found or if the cache entry has expired, then a null value is returned.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to retrieve.</param>
        /// <returns>Returns the located cache entry or null if not found.</returns>
        byte[] Retrieve(string cacheEntryKey);

        /// <summary>
        /// Caches a new cache entry or overwrites an existing cache entry with the matching 
        /// <paramref name="cacheEntryKey"/>.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to write.</param>
        /// <param name="value">The value to place in cache.</param>
        /// <param name="expiry">The time span that the cache entry will be cached before becoming eligible to 
        /// be deleted by the caching engine.</param>
        void Cache(string cacheEntryKey, byte[] value, TimeSpan expiry);
    }
}
