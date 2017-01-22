// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Impl
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Trifling.Caching.Interfaces;

    /// <summary>
    /// A very simple in-memory cache with basic expiry support and no memory limits nor any optimised key removal.
    /// Entries are only removed at the time they expire or if they are explicitly removed.
    /// </summary>
    /// <remarks>This should only be used for simple testing or local debugging. A production implementation should
    /// use a robust caching engine such as Redis.</remarks>
    public class SimpleCacheEngine : ICacheEngine
    {
        /// <summary>
        /// The cache of items.
        /// </summary>
        private static ConcurrentDictionary<string, byte[]> cachedItems = 
            new ConcurrentDictionary<string, byte[]>();

        /// <summary>
        /// The expiry times of the items in the <see cref="cachedItems"/>.
        /// </summary>
        private static ConcurrentDictionary<SimpleCacheEntryMetadata, DateTime> cacheExpireTimes = 
            new ConcurrentDictionary<SimpleCacheEntryMetadata, DateTime>();

        /// <summary>
        /// A lock root for parallel threaded operations.
        /// </summary>
        private static object expireLockRoot = new object();

        /// <summary>
        /// The previous time that the expiry check was done.
        /// </summary>
        private static DateTime previousExpiryCheck = DateTime.MinValue;

        /// <summary>
        /// Initialises a new instance of the <see cref="SimpleCacheEngine"/> class. 
        /// </summary>
        public SimpleCacheEngine()
        {
        }

        /// <summary>
        /// Initialises this cache engine with the given configuration.
        /// </summary>
        /// <param name="configuration">This implementation doesn't use the configuration given.</param>
        public void Initialise(CacheEngineConfiguration configuration)
        {
            // this implementation doesn't use any configuration because it's ... simple.
        }

        /// <summary>
        /// Caches a new cache entry or overwrites an existing cache entry with the matching 
        /// <paramref name="cacheEntryKey"/>.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to write.</param>
        /// <param name="value">The value to place in cache.</param>
        /// <param name="expiry">The time span that the cache entry will be cached before becoming eligible to 
        /// be deleted by the caching engine.</param>
        public void Cache(string cacheEntryKey, byte[] value, TimeSpan expiry)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);

            cachedItems.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, expireTime);
        }

        /// <summary>
        /// Removes a cache entry from the cache engine.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to remove from cache.</param>
        /// <returns>If the cache entry was found and removed, then returns true. Otherwise returns false.</returns>
        public bool Remove(string cacheEntryKey)
        {
            byte[] b;
            var removed = cachedItems.TryRemove(cacheEntryKey, out b);
            DateTime d;
            cacheExpireTimes.TryRemove(new SimpleCacheEntryMetadata(cacheEntryKey, DateTime.MinValue), out d);

            // only now we can check for other expired items
            this.CleanupExpiredCacheItems();

            // return the outcome of our initial attempt to remove the cached item.
            return removed;
        }

        /// <summary>
        /// Retrieves the cache entry with the matching <paramref name="cacheEntryKey"/>. If no cache entry is
        /// found or if the cache entry has expired, then a null value is returned.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to retrieve.</param>
        /// <returns>Returns the located cache entry or null if not found.</returns>
        public byte[] Retrieve(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            byte[] value;
            return (!cachedItems.TryGetValue(cacheEntryKey, out value))
                ? null
                : value;
        }

        /// <summary>
        /// Checks the last time that the cache was cleaned and if it was more than 500 ms ago then it
        /// will clean up the expired items from the cache.
        /// </summary>
        private void CleanupExpiredCacheItems()
        {
            // lock access to the expiry keys for the duration of this operation.
            lock (expireLockRoot)
            {
                if (previousExpiryCheck.AddMilliseconds(500) > DateTime.UtcNow)
                {
                    // ran less than 500 milliseconds ago, skip this.
                    return;
                }

                previousExpiryCheck = DateTime.UtcNow;
            }

            var killList = new List<SimpleCacheEntryMetadata>();
            foreach (var expireTime in cacheExpireTimes.Keys)
            {
                if (expireTime.ExpireTimeUtc <= DateTime.UtcNow)
                {
                    killList.Add(expireTime);
                }
            }

            // now kill these expired entries.
            foreach (var oldItem in killList)
            {
                byte[] b;
                cachedItems.TryRemove(oldItem.Key, out b);
                DateTime d;
                cacheExpireTimes.TryRemove(oldItem, out d);
            }
        }

        /// <summary>
        /// Sets (or resets) the expiry time for an item in the cache.
        /// </summary>
        /// <param name="cacheKey">The key of the cache entry.</param>
        /// <param name="expireTime">The new expiry time for the cache item in Universal time.</param>
        private void SetCacheExpireTime(string cacheKey, DateTime expireTime)
        {
            var old = cacheExpireTimes.Keys.FirstOrDefault(x => string.Equals(x.Key, cacheKey, StringComparison.Ordinal));
            if ((old != null) && !string.IsNullOrEmpty(old.Key))
            {
                DateTime d;
                cacheExpireTimes.TryRemove(old, out d);
            }

            cacheExpireTimes.AddOrUpdate(
                new SimpleCacheEntryMetadata(cacheKey, expireTime),
                expireTime,
                (c, e) => expireTime);
        }
    }
}
