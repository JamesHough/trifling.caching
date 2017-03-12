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
    using Trifling.Comparison;

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
        /// The cache of sets.
        /// </summary>
        private static ConcurrentDictionary<string, SortedSet<object>> cachedSets =
            new ConcurrentDictionary<string, SortedSet<object>>();

        /// <summary>
        /// The cache of lists.
        /// </summary>
        private static ConcurrentDictionary<string, List<object>> cachedLists =
            new ConcurrentDictionary<string, List<object>>();

        /// <summary>
        /// The cache of dictionaries.
        /// </summary>
        private static ConcurrentDictionary<string, Dictionary<string, object>> cachedDictionaries =
            new ConcurrentDictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// The cache of queues.
        /// </summary>
        private static ConcurrentDictionary<string, Queue<object>> cachedQueues =
            new ConcurrentDictionary<string, Queue<object>>();

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
        public void Initialize(CacheEngineConfiguration configuration)
        {
            // this implementation doesn't use any configuration because it's ... simple.
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
            cacheExpireTimes.TryRemove(new SimpleCacheEntryMetadata(cacheEntryKey, 'V', DateTime.MinValue), out d);

            // only now we can check for other expired items
            this.CleanupExpiredCacheItems();

            // return the outcome of our initial attempt to remove the cached item.
            return removed;
        }

        /// <summary>
        /// Checks if any cached value exists with the specified <paramref name="cacheEntryKey"/>.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to seek in cache.</param>
        /// <returns>If the cache entry was found, then returns true. Otherwise returns false.</returns>
        public bool Exists(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            return cachedItems.ContainsKey(cacheEntryKey);
        }

        #region Single value caching

        /// <summary>
        /// Caches a new cache entry or overwrites an existing cache entry with the matching 
        /// <paramref name="cacheEntryKey"/>.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to write.</param>
        /// <param name="value">The value to place in cache.</param>
        /// <param name="expiry">The time span that the cache entry will be cached before becoming eligible to 
        /// be deleted by the caching engine.</param>
        /// <returns>Returns true if the value was successfully cached. Otherwise false.</returns>
        public bool Cache(string cacheEntryKey, byte[] value, TimeSpan expiry)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);

            cachedItems.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'V', expireTime);
            return true;
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

        #endregion Single value caching

        #region Set caching

        /// <summary>
        /// Caches the given enumeration of <paramref name="setItems"/> as a set in the cache.
        /// </summary>
        /// <typeparam name="T">The type of object being cached. All items of the set must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="setItems">The individual items to store as a set.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the set was successfully created with all <paramref name="setItems"/> values cached.</returns>
        public bool CacheAsSet<T>(string cacheEntryKey, IEnumerable<T> setItems, TimeSpan expiry)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);
            var value = new SortedSet<object>(setItems.Cast<object>());

            cachedSets.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'S', expireTime);
            return true;
        }

        /// <summary>
        /// Caches the given enumeration of <paramref name="setItems"/> byte arrays as a set in the cache.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="setItems">The individual items to store as a set.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the set was successfully created with all <paramref name="setItems"/> values cached.</returns>
        public bool CacheAsSet(string cacheEntryKey, IEnumerable<byte[]> setItems, TimeSpan expiry)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);
            var value = new SortedSet<object>(setItems.Cast<object>(), BoxedByteArrayComparer.Default);

            cachedSets.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'S', expireTime);
            return true;
        }

        /// <summary>
        /// Adds a single new entry into an existing cached set.
        /// </summary>
        /// <typeparam name="T">The type of object being cached. All existing items of the set must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and add to.</param>
        /// <param name="value">The new individual item to store in the existing set.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be added to the cached set. Otherwise true.</returns>
        public bool AddToSet<T>(string cacheEntryKey, T value)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return false;
            }

            SortedSet<object> set;
            if (!cachedSets.TryGetValue(cacheEntryKey, out set))
            {
                return false;
            }

            if (set.Contains(value))
            {
                // set already contins this value.
                return false;
            }

            set.Add(value);
            cachedSets.AddOrUpdate(cacheEntryKey, set, (k, v) => set);
            return true;
        }

        /// <summary>
        /// Adds a single new byte array entry into an existing cached set.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and add to.</param>
        /// <param name="value">The new individual item to store in the existing set.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be added to the cached set. Otherwise true.</returns>
        public bool AddToSet(string cacheEntryKey, byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return false;
            }

            SortedSet<object> set;
            if (!cachedSets.TryGetValue(cacheEntryKey, out set))
            {
                return false;
            }

            if (set.Any(x => ByteArraysEqual((byte[])x, value)))
            {
                // set already contins this value.
                return false;
            }

            set.Add(value);
            cachedSets.AddOrUpdate(cacheEntryKey, set, (k, v) => set);
            return true;
        }

        /// <summary>
        /// Removes any matching entries with the same value from an existing cached set.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing set and remove.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached set. Otherwise true.</returns>
        public bool RemoveFromSet<T>(string cacheEntryKey, T value)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return false;
            }

            SortedSet<object> set;
            if (!cachedSets.TryGetValue(cacheEntryKey, out set))
            {
                return false;
            }

            set.RemoveWhere(x => x.Equals(value));
            cachedSets.AddOrUpdate(cacheEntryKey, set, (k, v) => set);
            return true;
        }

        /// <summary>
        /// Removes any matching byte array entries with the same value from an existing cached set.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing set and remove.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached set. Otherwise true.</returns>
        public bool RemoveFromSet(string cacheEntryKey, byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return false;
            }

            SortedSet<object> set;
            if (!cachedSets.TryGetValue(cacheEntryKey, out set))
            {
                return false;
            }

            set.RemoveWhere(x => ByteArraysEqual((byte[])x, value));
            cachedSets.AddOrUpdate(cacheEntryKey, set, (k, v) => set);
            return true;
        }

        /// <summary>
        /// Fetches a stored set from the cache and returns it as a set. If the key was found then the set 
        /// is returned. If not found then a null is returned.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located set from the cache if the key was found. Otherwise null.</returns>
        public ISet<T> RetrieveSet<T>(string cacheEntryKey)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return null;
            }

            SortedSet<object> retrievedSet;
            if (!cachedSets.TryGetValue(cacheEntryKey, out retrievedSet))
            {
                return null;
            }

            return new SortedSet<T>(retrievedSet.Cast<T>());
        }

        /// <summary>
        /// Fetches a stored set from the cache and returns it as a set. If the key was found then the set 
        /// is returned. If not found then a null is returned.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located set from the cache if the key was found. Otherwise null.</returns>
        public ISet<byte[]> RetrieveSet(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return null;
            }

            SortedSet<object> retrievedSet;
            if (!cachedSets.TryGetValue(cacheEntryKey, out retrievedSet))
            {
                return null;
            }

            return new SortedSet<byte[]>(retrievedSet.Cast<byte[]>(), ByteArrayComparer.Default);
        }

        /// <summary>
        /// Attempts to locate the <paramref name="value"/> in a cached set.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached set to locate and within which to find the value.</param>
        /// <param name="value">The value to locate in the existing set.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value is not present in the cached set.</returns>
        public bool ExistsInSet(string cacheEntryKey, byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return false;
            }

            SortedSet<object> retrievedSet;
            if (!cachedSets.TryGetValue(cacheEntryKey, out retrievedSet))
            {
                return false;
            }

            var comparer = ByteArrayComparer.Default;

            // this is a sorted set, so we can stop searching as soon as the value we seek is greater than the current item.
            return retrievedSet
                .Cast<byte[]>()
                .TakeWhile(v => comparer.Compare(value, v) >= 0)
                .Any(v => comparer.Compare(value, v) == 0);
        }

        /// <summary>
        /// Attempts to locate the <paramref name="value"/> in a cached set.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cached set to locate and within which to find the value.</param>
        /// <param name="value">The value to locate in the existing set.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value is not present in the cached set.</returns>
        public bool ExistsInSet<T>(string cacheEntryKey, T value) 
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return false;
            }

            SortedSet<object> retrievedSet;
            if (!cachedSets.TryGetValue(cacheEntryKey, out retrievedSet))
            {
                return false;
            }

            var comparer = Comparer<T>.Default;

            // this is a sorted set, so we can stop searching as soon as the value we seek is greater than the current item.
            return retrievedSet
                .Cast<T>()
                .TakeWhile(v => comparer.Compare(value, v) >= 0)
                .Any(v => comparer.Compare(value, v) == 0);
        }

        /// <summary>
        /// Gets the length of a set stored in the cache. If the key doesn't exist or isn't a set then returns null.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached set to locate and for which the length must be read.</param>
        /// <returns>Returns the length of the set if found, or null if not found.</returns>
        public long? LengthOfSet(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedSets.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached set
                return null;
            }

            SortedSet<object> retrievedSet;
            if (!cachedSets.TryGetValue(cacheEntryKey, out retrievedSet))
            {
                return null;
            }

            return retrievedSet.Count;
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
        public bool CacheAsList<T>(string cacheEntryKey, IEnumerable<T> listItems, TimeSpan expiry)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);
            var value = new List<object>(listItems.Cast<object>());

            cachedLists.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'L', expireTime);
            return true;
        }

        /// <summary>
        /// Caches the given enumeration of <paramref name="listItems"/> byte arrays as a list in the cache.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="listItems">The individual byte array values to store as a list.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the list was successfully created with all <paramref name="listItems"/> values cached.</returns>
        public bool CacheAsList(string cacheEntryKey, IEnumerable<byte[]> listItems, TimeSpan expiry)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);
            var value = new List<object>(listItems.Cast<object>());

            cachedLists.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'L', expireTime);
            return true;
        }

        /// <summary>
        /// Fetches a stored list from the cache and returns it as a <see cref="IList{T}"/>. If the key was 
        /// found then the list is returned. If not found then a null is returned.
        /// </summary>
        /// <typeparam name="T">The type of object that was cached in a list.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located list from the cache if the key was found. Otherwise null.</returns>
        public IList<T> RetrieveList<T>(string cacheEntryKey)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return null;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return null;
            }

            return new List<T>(retrievedList.Cast<T>());
        }

        /// <summary>
        /// Fetches a stored list from the cache and returns it as a List of byte array values. If the key was 
        /// found then the list is returned. If not found then a null is returned.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located list from the cache if the key was found. Otherwise null.</returns>
        public IList<byte[]> RetrieveList(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return null;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return null;
            }

            return new List<byte[]>(retrievedList.Cast<byte[]>());
        }

        /// <summary>
        /// Appends a new value to the end of an existing cached list.
        /// </summary>
        /// <typeparam name="T">The type of object being appended to the cached list. All items of the list must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that the 
        /// <paramref name="value"/> will be appended to.</param>
        /// <param name="value">The value to append to the cached list.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be appended. Otherwise true.</returns>
        public bool AppendToList<T>(string cacheEntryKey, T value)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return false;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return false;
            }

            retrievedList.Add(value);
            return true;
        }

        /// <summary>
        /// Appends a new byte array value to the end of an existing cached list.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that the 
        /// <paramref name="value"/> will be appended to.</param>
        /// <param name="value">The value to append to the cached list.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be appended. Otherwise true.</returns>
        public bool AppendToList(string cacheEntryKey, byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return false;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return false;
            }

            retrievedList.Add(value);
            return true;
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
        public bool ShrinkList(string cacheEntryKey, long firstIndexKept, long lastIndexKept)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return false;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return false;
            }

            var length = retrievedList.Count;
            if (firstIndexKept < 0L)
            {
                // get the real index based on this end reference
                firstIndexKept = length + firstIndexKept;
            }

            if (lastIndexKept < 0L)
            {
                // get the real index based on this end reference
                lastIndexKept = length + lastIndexKept;
            }

            // if the caller entered criteria that would result in an invalid range then rather clear
            if (lastIndexKept < firstIndexKept)
            {
                retrievedList.Clear();
                return true;
            }

            for (var i = length - 1; i >= 0; i--)
            {
                if (i > lastIndexKept)
                {
                    retrievedList.RemoveAt(i);
                }
                else if (i < firstIndexKept)
                {
                    retrievedList.RemoveAt(i);
                }
            }

            return true;
        }

        /// <summary>
        /// Removes any matching entries with the same value from an existing cached list.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached list.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cached list to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing list and remove.</param>
        /// <returns>Returns -1 list doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached list. Otherwise returns the number of removed items.</returns>
        public long RemoveFromList<T>(string cacheEntryKey, T value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return -1L;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return -1L;
            }

            return retrievedList.Remove(value) ? 1L : 0L;
        }

        /// <summary>
        /// Removes any matching entries with the same byte array value from an existing cached list.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached list to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing list and remove.</param>
        /// <returns>Returns -1 list doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached list. Otherwise returns the number of removed items.</returns>
        public long RemoveFromList(string cacheEntryKey, byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return -1L;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return -1L;
            }

            return retrievedList.RemoveAll(l => BoxedByteArrayComparer.Default.Compare(l, value) == 0);
        }

        /// <summary>
        /// Removes all items from an existing cached list.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that must be cleared.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the list cannot be cleared. Otherwise true.</returns>
        public bool ClearList(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return false;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return false;
            }

            retrievedList.Clear();
            return true;
        }

        /// <summary>
        /// Gets the length of a list stored in the cache. If the key doesn't exist or isn't a list then returns null.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached list to locate and for which the length must be read.</param>
        /// <returns>Returns the length of the list if found, or null if not found.</returns>
        public long? LengthOfList(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedLists.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached list
                return null;
            }

            List<object> retrievedList;
            if (!cachedLists.TryGetValue(cacheEntryKey, out retrievedList))
            {
                return null;
            }

            return retrievedList.Count;
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
        public bool CacheAsDictionary<T>(string cacheEntryKey, IDictionary<string, T> dictionaryItems, TimeSpan expiry)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);
            var value = new Dictionary<string, object>(
                dictionaryItems
                    .ToDictionary(x => x.Key, x => (object)x.Value));

            cachedDictionaries.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'D', expireTime);
            return true;
        }

        /// <summary>
        /// Caches the given dictionary of byte array items as a new dictionary in the cache engine.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which will contain the dictionary.</param>
        /// <param name="dictionaryItems">The items to cache as a dictionary.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the dictionary was successfully created with all <paramref name="dictionaryItems"/> byte array values cached.</returns>
        public bool CacheAsDictionary(string cacheEntryKey, IDictionary<string, byte[]> dictionaryItems, TimeSpan expiry)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);
            var value = new Dictionary<string, object>(
                dictionaryItems
                    .ToDictionary(x => x.Key, x => (object)x.Value));

            cachedDictionaries.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'D', expireTime);
            return true;
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
        public bool AddToDictionary<T>(string cacheEntryKey, string dictionaryKey, T value)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return false;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return false;
            }

            if (dictionary.ContainsKey(dictionaryKey))
            {
                return false;
            }

            dictionary.Add(dictionaryKey, value);
            return true;
        }

        /// <summary>
        /// Adds a new dictionary entry for the given byte array value into an existing cached dictionary with 
        /// the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary that the 
        /// <paramref name="value"/> will be added to.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being added.</param>
        /// <param name="value">The byte array value to add into the cached dictionary.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be added. Otherwise true.</returns>
        public bool AddToDictionary(string cacheEntryKey, string dictionaryKey, byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return false;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return false;
            }

            if (dictionary.ContainsKey(dictionaryKey))
            {
                return false;
            }

            dictionary.Add(dictionaryKey, value);
            return true;
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
        public bool UpdateDictionaryEntry<T>(string cacheEntryKey, string dictionaryKey, T value)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return false;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return false;
            }

            if (!dictionary.ContainsKey(dictionaryKey))
            {
                return false;
            }

            dictionary[dictionaryKey] = value;
            return true;
        }

        /// <summary>
        /// Updates an existing dictionary entry with the given byte array value in an existing cached 
        /// dictionary for the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being updated.</param>
        /// <param name="value">The value to update in the cached dictionary.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the <paramref name="dictionaryKey"/> cannot
        /// be found or the value cannot be updated. Otherwise true.</returns>
        public bool UpdateDictionaryEntry(string cacheEntryKey, string dictionaryKey, byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return false;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return false;
            }

            if (!dictionary.ContainsKey(dictionaryKey))
            {
                return false;
            }

            dictionary[dictionaryKey] = value;
            return true;
        }

        /// <summary>
        /// Removes a dictionary entry from an existing cached dictionary for the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being removed.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the <paramref name="dictionaryKey"/> cannot
        /// be removed. Otherwise true.</returns>
        public bool RemoveFromDictionary(string cacheEntryKey, string dictionaryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return false;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return false;
            }

            return dictionary.Remove(dictionaryKey);
        }

        /// <summary>
        /// Retrieves all entries in a cached dictionary as a new <see cref="IDictionary{TKey, TValue}"/>. 
        /// </summary>
        /// <typeparam name="T">The type of object which was written in the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <returns>Returns the located dictionary from the cache if the key was found. Otherwise null.</returns>
        public IDictionary<string, T> RetrieveDictionary<T>(string cacheEntryKey)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return null;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return null;
            }

            return dictionary
                .ToDictionary(x => x.Key, x => (T)x.Value);
        }

        /// <summary>
        /// Retrieves all entries in a cached dictionary as a new <see cref="IDictionary{TKey, TValue}"/>. 
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <returns>Returns the located dictionary containing byte array values from the cache if the key was found. Otherwise null.</returns>
        public IDictionary<string, byte[]> RetrieveDictionary(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return null;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return null;
            }

            return dictionary
                .ToDictionary(x => x.Key, x => (byte[])x.Value);
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
        public bool RetrieveDictionaryEntry<T>(string cacheEntryKey, string dictionaryKey, out T value)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                value = default(T);
                return false;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                value = default(T);
                return false;
            }

            if (!dictionary.ContainsKey(dictionaryKey))
            {
                value = default(T);
                return false;
            }

            value = (T)dictionary[dictionaryKey];
            return true;
        }

        /// <summary>
        /// Retrieves a single entry (a byte array) from a cached dictionary located by the <paramref name="dictionaryKey"/>. 
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being sought.</param>
        /// <param name="value">Returns the byte array value found in the dictionary cache. If not found then null is returned.</param>
        /// <returns>Returns true if the value was located in the cached dictionary. Otherwise false.</returns>
        public bool RetrieveDictionaryEntry(string cacheEntryKey, string dictionaryKey, out byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                value = null;
                return false;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                value = null;
                return false;
            }

            if (!dictionary.ContainsKey(dictionaryKey))
            {
                value = null;
                return false;
            }

            value = (byte[])dictionary[dictionaryKey];
            return true;
        }

        /// <summary>
        /// Attempts to locate the <paramref name="dictionaryKey"/> in a cached dictionary.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being sought.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the key is not present in the cached dictionary.</returns>
        public bool ExistsInDictionary(string cacheEntryKey, string dictionaryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return false;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return false;
            }

            return dictionary.ContainsKey(dictionaryKey);
        }

        /// <summary>
        /// Gets the length of a dictionary stored in the cache. If the key doesn't exist or isn't a dictionary then returns null.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached dictionary to locate and for which the length must be read.</param>
        /// <returns>Returns the length of the dictionary if found, or null if not found.</returns>
        public long? LengthOfDictionary(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedDictionaries.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached dictionary
                return null;
            }

            Dictionary<string, object> dictionary;
            if (!cachedDictionaries.TryGetValue(cacheEntryKey, out dictionary))
            {
                return null;
            }

            return dictionary.Count;
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
        public bool CacheAsQueue<T>(string cacheEntryKey, IEnumerable<T> queuedItems, TimeSpan expiry)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);
            var value = new Queue<object>(queuedItems.Cast<object>());

            cachedQueues.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'Q', expireTime);
            return true;
        }

        /// <summary>
        /// Caches the given enumeration of <paramref name="queuedItems"/> byte arrays as a queue in the cache.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="queuedItems">The individual byte array values to store as a queue.</param>
        /// <param name="expiry">The time period that the data will be valid. May be set to never expire by setting <see cref="TimeSpan.MaxValue"/>.</param>
        /// <returns>Returns true if the queue was successfully created with all <paramref name="queuedItems"/> values cached.</returns>
        public bool CacheAsQueue(string cacheEntryKey, IEnumerable<byte[]> queuedItems, TimeSpan expiry)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (expiry <= TimeSpan.Zero)
            {
                return false;
            }

            var expireTime = DateTime.UtcNow.Add(expiry);
            var value = new Queue<object>(queuedItems.Cast<object>());

            cachedQueues.AddOrUpdate(cacheEntryKey, value, (k, v) => value);

            // update the expiry time with the new expiry
            this.SetCacheExpireTime(cacheEntryKey, 'Q', expireTime);
            return true;
        }

        /// <summary>
        /// Pushes a new value to the end of an existing cached queue.
        /// </summary>
        /// <typeparam name="T">The type of object being pushed to the cached queue. All items of the queue must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">The value to append to the cached queue.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be pushed to the queue. Otherwise true.</returns>
        public bool PushQueue<T>(string cacheEntryKey, T value)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedQueues.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached queue
                return false;
            }

            Queue<object> queue;
            if (!cachedQueues.TryGetValue(cacheEntryKey, out queue))
            {
                return false;
            }

            queue.Enqueue(value);
            return true;
        }

        /// <summary>
        /// Pushes a new byte array to the end of an existing cached queue.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">The value to append to the cached queue.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be pushed to the queue. Otherwise true.</returns>
        public bool PushQueue(string cacheEntryKey, byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedQueues.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached queue
                return false;
            }

            Queue<object> queue;
            if (!cachedQueues.TryGetValue(cacheEntryKey, out queue))
            {
                return false;
            }

            queue.Enqueue(value);
            return true;
        }

        /// <summary>
        /// Pops the next value in the cached queue and returns the value.
        /// </summary>
        /// <typeparam name="T">The type of the objects stored in the cached queue. All items of the queue must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">Returns the next value from the cached queue. If not found then a default value is returned.</param>
        /// <returns>Returns true if the next value in the cached queue was successfully returned in <paramref name="value"/>. Otherwise false.</returns>
        public bool PopQueue<T>(string cacheEntryKey, out T value)
            where T : IConvertible
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedQueues.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached queue
                value = default(T);
                return false;
            }

            Queue<object> queue;
            if (!cachedQueues.TryGetValue(cacheEntryKey, out queue))
            {
                value = default(T);
                return false;
            }

            if (queue.Count < 1)
            {
                // there are no more items in the queue. don't error, return false.
                value = default(T);
                return false;
            }

            value = (T)queue.Dequeue();
            return true;
        }

        /// <summary>
        /// Pops the next byte array in the cached queue and returns the value.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">Returns the next byte array value from the cached queue. If not found then null is returned.</param>
        /// <returns>Returns true if the next value in the cached queue was successfully returned in <paramref name="value"/>. Otherwise false.</returns>
        public bool PopQueue(string cacheEntryKey, out byte[] value)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedQueues.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached queue
                value = null;
                return false;
            }

            Queue<object> queue;
            if (!cachedQueues.TryGetValue(cacheEntryKey, out queue))
            {
                value = null;
                return false;
            }

            if (queue.Count < 1)
            {
                // there are no more items in the queue. don't error, return false.
                value = null;
                return false;
            }

            value = (byte[])queue.Dequeue();
            return true;
        }

        /// <summary>
        /// Removes all items from an existing cached queue.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue that must be cleared.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the queue cannot be cleared. Otherwise true.</returns>
        public bool ClearQueue(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedQueues.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached queue
                return false;
            }

            Queue<object> queue;
            if (!cachedQueues.TryGetValue(cacheEntryKey, out queue))
            {
                return false;
            }

            queue.Clear();
            return true;
        }

        /// <summary>
        /// Gets the length of a queue stored in the cache. If the key doesn't exist or isn't a queue then returns null.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached queue to locate and for which the length must be read.</param>
        /// <returns>Returns the length of the queue if found, or null if not found.</returns>
        public long? LengthOfQueue(string cacheEntryKey)
        {
            // first remove anything that has already expired.
            this.CleanupExpiredCacheItems();

            if (!cachedQueues.ContainsKey(cacheEntryKey))
            {
                // key doesn't exist as a cached queue
                return null;
            }

            Queue<object> queue;
            if (!cachedQueues.TryGetValue(cacheEntryKey, out queue))
            {
                return null;
            }

            return queue.Count;
        }

        #endregion Queue caching

        #region Private methods

        /// <summary>
        /// Compares two byte arrays to check if they are equal in value.
        /// </summary>
        /// <param name="a">The first byte array to compare.</param>
        /// <param name="b">The second byte array to compare.</param>
        /// <returns>Returns true only if both arrays are the same length and each member is equal in value.</returns>
        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
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
                switch (oldItem.Type)
                {
                    case 'S':
                        SortedSet<object> v;
                        cachedSets.TryRemove(oldItem.Key, out v);
                        break;

                    case 'L':
                        List<object> l;
                        cachedLists.TryRemove(oldItem.Key, out l);
                        break;

                    case 'D':
                        Dictionary<string, object> x;
                        cachedDictionaries.TryRemove(oldItem.Key, out x);
                        break;

                    case 'Q':
                        Queue<object> q;
                        cachedQueues.TryRemove(oldItem.Key, out q);
                        break;

                    case 'V':
                    default:
                        byte[] b;
                        cachedItems.TryRemove(oldItem.Key, out b);
                        break;
                }
                
                DateTime d;
                cacheExpireTimes.TryRemove(oldItem, out d);
            }
        }

        /// <summary>
        /// Sets (or resets) the expiry time for an item in the cache.
        /// </summary>
        /// <param name="cacheKey">The key of the cache entry.</param>
        /// <param name="type">The type of cached entry - one of 'S', 'L', 'D', 'Q' or 'V'.</param>
        /// <param name="expireTime">The new expiry time for the cache item in Universal time.</param>
        private void SetCacheExpireTime(string cacheKey, char type, DateTime expireTime)
        {
            var old = cacheExpireTimes.Keys.FirstOrDefault(x => string.Equals(x.Key, cacheKey, StringComparison.Ordinal));
            if ((old != null) && !string.IsNullOrEmpty(old.Key))
            {
                DateTime d;
                cacheExpireTimes.TryRemove(old, out d);
            }

            cacheExpireTimes.AddOrUpdate(
                new SimpleCacheEntryMetadata(cacheKey, type, expireTime),
                expireTime,
                (c, e) => expireTime);
        }

        #endregion Private methods
    }
}
