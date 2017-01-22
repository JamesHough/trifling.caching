// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Interfaces
{
    using System;

    /// <summary>
    /// Provides access to a cache engine.
    /// </summary>
    public interface ICacheManager
    {
        /// <summary>
        /// Caches a new cache entry or overwrites an existing cache entry with the matching <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique identifier of the cache entry to write.</param>
        /// <param name="value">The value to place in cache.</param>
        /// <param name="expiry">The time span that the cache entry will be cached before becoming eligible to 
        /// be deleted by the caching engine.</param>
        /// <returns>Returns the value that was written to cache.</returns>
        /// <typeparam name="T">The type of the value being cached.</typeparam>
        T Cache<T>(CacheEntryKey key, T value, TimeSpan expiry);

        /// <summary>
        /// Attempts to retrieve the cached value. If not found or if expired, then executes the provided
        /// <paramref name="valueFunction"/> to get a value and caches that value and returns that value.
        /// </summary>
        /// <param name="key">The unique identifier of the cache entry to retrieve or re-cache.</param>
        /// <param name="valueIfNotFound">The default value to use only if the specified cache entry key is 
        /// not found in cache or has expired.</param>
        /// <returns>Returns the value from the cache with the matching <paramref name="key"/>.</returns>
        /// <typeparam name="T">The type of the value being retrieved from cache. If the type doesn't match then an exception is thrown.</typeparam>
        T Retrieve<T>(CacheEntryKey key, T valueIfNotFound = default(T));

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
        T RetrieveOrRecache<T>(CacheEntryKey key, Func<T> valueFunction, TimeSpan expiry);

        /// <summary>
        /// Removes a cache entry from the cache engine.
        /// </summary>
        /// <param name="key">The unique identifier of the cache entry to remove from cache.</param>
        /// <returns>If the cache entry was found and removed, then returns true. Otherwise returns false.</returns>
        bool Remove(CacheEntryKey key); 
    }
}
