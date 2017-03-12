// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Interfaces
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides access to a cache engine.
    /// </summary>
    public interface ICacheManager
    {
        #region Common caching operations

        /// <summary>
        /// Removes a cache entry from the cache engine.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to remove from cache.</param>
        /// <returns>If the cache entry was found and removed, then returns true. Otherwise returns false.</returns>
        bool Remove(CacheEntryKey cacheEntryKey);

        /// <summary>
        /// Checks if any cached value exists with the specified <paramref name="cacheEntryKey"/>.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to seek in cache.</param>
        /// <returns>If the cache entry was found, then returns true. Otherwise returns false.</returns>
        bool Exists(CacheEntryKey cacheEntryKey);

        #endregion Common caching operations

        #region Single value caching

        /// <summary>
        /// Caches a new cache entry or overwrites an existing cache entry with the matching <paramref name="cacheEntryKey"/>.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to write.</param>
        /// <param name="value">The value to place in cache.</param>
        /// <param name="expiry">The time span that the cache entry will be cached before becoming eligible to 
        /// be deleted by the caching engine.</param>
        /// <returns>Returns the value that was written to cache.</returns>
        /// <typeparam name="T">The type of the value being cached.</typeparam>
        T Cache<T>(CacheEntryKey cacheEntryKey, T value, TimeSpan expiry);

        /// <summary>
        /// Attempts to retrieve the cached value. If not found or if expired, then uses the provided
        /// <paramref name="valueIfNotFound"/>, caches that value and returns that value.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to retrieve or re-cache.</param>
        /// <param name="valueIfNotFound">The default value to use only if the specified cache entry key is 
        /// not found in cache or has expired.</param>
        /// <returns>Returns the value from the cache with the matching <paramref name="cacheEntryKey"/>.</returns>
        /// <typeparam name="T">The type of the value being retrieved from cache. If the type doesn't match then an exception is thrown.</typeparam>
        T Retrieve<T>(CacheEntryKey cacheEntryKey, T valueIfNotFound = default(T));

        /// <summary>
        /// Attempts to retrieve the cached value. If not found or if expired, then executes the provided
        /// <paramref name="valueFunction"/> to get a value and caches that value and returns that value.
        /// </summary>
        /// <param name="cacheEntryKey">The unique identifier of the cache entry to retrieve or re-cache.</param>
        /// <param name="valueFunction">The function to execute to retrieve the new value for re-caching 
        /// only if the specified cache entry key is not found in cache or has expired.</param>
        /// <param name="expiry">The time span that the re-cached entry will be cached before becoming eligible to 
        /// be deleted by the caching engine. This is only used if re-caching is needed.</param>
        /// <returns>Returns the value from the cache with the matching <paramref name="cacheEntryKey"/>.</returns>
        /// <typeparam name="T">The type of the value being retrieved from cache. If the type doesn't match then an exception is thrown.</typeparam>
        T RetrieveOrRecache<T>(CacheEntryKey cacheEntryKey, Func<T> valueFunction, TimeSpan expiry);

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
        bool CacheAsSet<T>(CacheEntryKey cacheEntryKey, IEnumerable<T> setItems, TimeSpan expiry);

        /// <summary>
        /// Adds a single new entry into an existing cached set.
        /// </summary>
        /// <typeparam name="T">The type of object being cached. All existing items of the set must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and add to.</param>
        /// <param name="value">The new individual item to store in the existing set.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be added to the cached set. Otherwise true.</returns>
        bool AddToSet<T>(CacheEntryKey cacheEntryKey, T value);

        /// <summary>
        /// Removes any matching entries with the same value from an existing cached set.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing set and remove.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached set. Otherwise true.</returns>
        bool RemoveFromSet<T>(CacheEntryKey cacheEntryKey, T value);

        /// <summary>
        /// Fetches a stored set from the cache and returns it as a set. If the key was found then the set 
        /// is returned. If not found then a null is returned.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located set from the cache if the key was found. Otherwise null.</returns>
        ISet<T> RetrieveSet<T>(CacheEntryKey cacheEntryKey);

        /// <summary>
        /// Attempts to locate the <paramref name="value"/> in a cached set.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cached set to locate and within which to find the value.</param>
        /// <param name="value">The value to locate in the existing set.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value is not present in the cached set.</returns>
        bool ExistsInSet<T>(CacheEntryKey cacheEntryKey, T value);

        /// <summary>
        /// Gets the length of a set stored in the cache. If the key doesn't exist or isn't a set then returns null.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached set to locate and for which the length must be read.</param>
        /// <returns>Returns the length of the set if found, or null if not found.</returns>
        long? LengthOfSet(CacheEntryKey cacheEntryKey);

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
        bool CacheAsList<T>(CacheEntryKey cacheEntryKey, IEnumerable<T> listItems, TimeSpan expiry);

        /// <summary>
        /// Fetches a stored list from the cache and returns it as a <see cref="IList{T}"/>. If the key was 
        /// found then the list is returned. If not found then a null is returned.
        /// </summary>
        /// <typeparam name="T">The type of object that was cached in a list.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located list from the cache if the key was found. Otherwise null.</returns>
        IList<T> RetrieveList<T>(CacheEntryKey cacheEntryKey);

        /// <summary>
        /// Appends a new value to the end of an existing cached list.
        /// </summary>
        /// <typeparam name="T">The type of object being appended to the cached list. All items of the list must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that the 
        /// <paramref name="value"/> will be appended to.</param>
        /// <param name="value">The value to append to the cached list.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be appended. Otherwise true.</returns>
        bool AppendToList<T>(CacheEntryKey cacheEntryKey, T value);

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
        bool ShrinkList(CacheEntryKey cacheEntryKey, long firstIndexKept, long lastIndexKept);

        /// <summary>
        /// Removes any matching entries with the same value from an existing cached list.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached list.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cached list to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing list and remove.</param>
        /// <returns>Returns -1 list doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached list. Otherwise returns the number of removed items.</returns>
        long RemoveFromList<T>(CacheEntryKey cacheEntryKey, T value);

        /// <summary>
        /// Removes all items from an existing cached list.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that must be cleared.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the list cannot be cleared. Otherwise true.</returns>
        bool ClearList(CacheEntryKey cacheEntryKey);

        /// <summary>
        /// Gets the length of a list stored in the cache. If the key doesn't exist or isn't a list then returns null.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached list to locate and for which the length must be read.</param>
        /// <returns>Returns the length of the list if found, or null if not found.</returns>
        long? LengthOfList(CacheEntryKey cacheEntryKey);

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
        bool CacheAsDictionary<T>(CacheEntryKey cacheEntryKey, IDictionary<string, T> dictionaryItems, TimeSpan expiry);

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
        bool AddToDictionary<T>(CacheEntryKey cacheEntryKey, string dictionaryKey, T value);

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
        bool UpdateDictionaryEntry<T>(CacheEntryKey cacheEntryKey, string dictionaryKey, T value);

        /// <summary>
        /// Removes a dictionary entry from an existing cached dictionary for the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being removed.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the <paramref name="dictionaryKey"/> cannot
        /// be removed. Otherwise true.</returns>
        bool RemoveFromDictionary(CacheEntryKey cacheEntryKey, string dictionaryKey);

        /// <summary>
        /// Retrieves all entries in a cached dictionary as a new <see cref="IDictionary{TKey, TValue}"/>. 
        /// </summary>
        /// <typeparam name="T">The type of object which was written in the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <returns>Returns the located dictionary from the cache if the key was found. Otherwise null.</returns>
        IDictionary<string, T> RetrieveDictionary<T>(CacheEntryKey cacheEntryKey);

        /// <summary>
        /// Retrieves a single entry from a cached dictionary located by the <paramref name="dictionaryKey"/>. 
        /// </summary>
        /// <typeparam name="T">The type of object which was written in the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being sought.</param>
        /// <param name="value">Returns the value found in the dictionary cache. If not found the default value is returned.</param>
        /// <returns>Returns true if the value was located in the cached dictionary. Otherwise false.</returns>
        bool RetrieveDictionaryEntry<T>(CacheEntryKey cacheEntryKey, string dictionaryKey, out T value);

        /// <summary>
        /// Attempts to locate the <paramref name="dictionaryKey"/> in a cached dictionary.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being sought.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the key is not present in the cached dictionary.</returns>
        bool ExistsInDictionary(CacheEntryKey cacheEntryKey, string dictionaryKey);

        /// <summary>
        /// Gets the length of a dictionary stored in the cache. If the key doesn't exist or isn't a dictionary then returns null.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached dictionary to locate and for which the length must be read.</param>
        /// <returns>Returns the length of the dictionary if found, or null if not found.</returns>
        long? LengthOfDictionary(CacheEntryKey cacheEntryKey);

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
        bool CacheAsQueue<T>(CacheEntryKey cacheEntryKey, IEnumerable<T> queuedItems, TimeSpan expiry);

        /// <summary>
        /// Pushes a new value to the end of an existing cached queue.
        /// </summary>
        /// <typeparam name="T">The type of object being pushed to the cached queue. All items of the queue must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">The value to append to the cached queue.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be pushed to the queue. Otherwise true.</returns>
        bool PushQueue<T>(CacheEntryKey cacheEntryKey, T value);

        /// <summary>
        /// Pops the next value in the cached queue and returns the value.
        /// </summary>
        /// <typeparam name="T">The type of the objects stored in the cached queue. All items of the queue must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">Returns the next value from the cached queue. If not found then a default value is returned.</param>
        /// <returns>Returns true if the next value in the cached queue was successfully returned in <paramref name="value"/>. Otherwise false.</returns>
        bool PopQueue<T>(CacheEntryKey cacheEntryKey, out T value);

        /// <summary>
        /// Removes all items from an existing cached queue.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue that must be cleared.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the queue cannot be cleared. Otherwise true.</returns>
        bool ClearQueue(CacheEntryKey cacheEntryKey);

        /// <summary>
        /// Gets the length of a queue stored in the cache. If the key doesn't exist or isn't a queue then returns null.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached queue to locate and for which the length must be read.</param>
        /// <returns>Returns the length of the queue if found, or null if not found.</returns>
        long? LengthOfQueue(CacheEntryKey cacheEntryKey);

        #endregion Queue caching
    }
}
