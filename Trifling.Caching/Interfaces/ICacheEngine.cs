﻿// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Interfaces
{
    using System;
    using System.Collections.Generic;

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

        #region Single value caching

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
        /// <returns>Returns true if the value was successfully cached. Otherwise false.</returns>
        bool Cache(string cacheEntryKey, byte[] value, TimeSpan expiry);

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
        bool CacheAsSet<T>(string cacheEntryKey, IEnumerable<T> setItems, TimeSpan expiry)
            where T : IConvertible;

        /// <summary>
        /// Caches the given enumeration of <paramref name="setItems"/> byte arrays as a set in the cache.
        /// </summary>
        /// <remarks>
        /// Items of a set are not guaranteed to retain ordering when retrieved from cache. The
        /// implementation of <see cref="RetrieveSet"/> returns a sorted set even if the input
        /// was not sorted.
        /// </remarks>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="setItems">The individual items to store as a set.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the set was successfully created with all <paramref name="setItems"/> values cached.</returns>
        bool CacheAsSet(string cacheEntryKey, IEnumerable<byte[]> setItems, TimeSpan expiry);

        /// <summary>
        /// Adds a single new entry into an existing cached set.
        /// </summary>
        /// <typeparam name="T">The type of object being cached. All existing items of the set must be of this type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and add to.</param>
        /// <param name="value">The new individual item to store in the existing set.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be added to the cached set. Otherwise true.</returns>
        bool AddToSet<T>(string cacheEntryKey, T value)
            where T : IConvertible;

        /// <summary>
        /// Adds a single new byte array entry into an existing cached set.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and add to.</param>
        /// <param name="value">The new individual item to store in the existing set.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be added to the cached set. Otherwise true.</returns>
        bool AddToSet(string cacheEntryKey, byte[] value);

        /// <summary>
        /// Removes any matching entries with the same value from an existing cached set.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing set and remove.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached set. Otherwise true.</returns>
        bool RemoveFromSet<T>(string cacheEntryKey, T value)
            where T : IConvertible;

        /// <summary>
        /// Removes any matching byte array entries with the same value from an existing cached set.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing set and remove.</param>
        /// <returns>Returns false if the set doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached set. Otherwise true.</returns>
        bool RemoveFromSet(string cacheEntryKey, byte[] value);

        /// <summary>
        /// Fetches a stored set from the cache and returns it as a set. If the key was found then the set 
        /// is returned. If not found then a null is returned.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached set.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located set from the cache if the key was found. Otherwise null.</returns>
        ISet<T> RetrieveSet<T>(string cacheEntryKey)
            where T : IConvertible;

        /// <summary>
        /// Fetches a stored set from the cache and returns it as a set. If the key was found then the set 
        /// is returned. If not found then a null is returned.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located set from the cache if the key was found. Otherwise null.</returns>
        ISet<byte[]> RetrieveSet(string cacheEntryKey);

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
        bool CacheAsList<T>(string cacheEntryKey, IEnumerable<T> listItems, TimeSpan expiry)
            where T : IConvertible;

        /// <summary>
        /// Caches the given enumeration of <paramref name="listItems"/> byte arrays as a list in the cache.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="listItems">The individual byte array values to store as a list.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the list was successfully created with all <paramref name="listItems"/> values cached.</returns>
        bool CacheAsList(string cacheEntryKey, IEnumerable<byte[]> listItems, TimeSpan expiry);

        /// <summary>
        /// Fetches a stored list from the cache and returns it as a <see cref="IList{T}"/>. If the key was 
        /// found then the list is returned. If not found then a null is returned.
        /// </summary>
        /// <typeparam name="T">The type of object that was cached in a list.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located list from the cache if the key was found. Otherwise null.</returns>
        IList<T> RetrieveList<T>(string cacheEntryKey)
            where T : IConvertible;

        /// <summary>
        /// Fetches a stored list from the cache and returns it as a List of byte array values. If the key was 
        /// found then the list is returned. If not found then a null is returned.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to attempt to retrieve.</param>
        /// <returns>Returns the located list from the cache if the key was found. Otherwise null.</returns>
        IList<byte[]> RetrieveList(string cacheEntryKey);

        /// <summary>
        /// Appends a new value to the end of an existing cached list.
        /// </summary>
        /// <typeparam name="T">The type of object being appended to the cached list. All items of the list must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that the 
        /// <paramref name="value"/> will be appended to.</param>
        /// <param name="value">The value to append to the cached list.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be appended. Otherwise true.</returns>
        bool AppendToList<T>(string cacheEntryKey, T value)
            where T : IComparable;

        /// <summary>
        /// Appends a new byte array value to the end of an existing cached list.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that the 
        /// <paramref name="value"/> will be appended to.</param>
        /// <param name="value">The value to append to the cached list.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be appended. Otherwise true.</returns>
        bool AppendToList(string cacheEntryKey, byte[] value);

        /// <summary>
        /// Injects a new value into an existing cached list at the position specified.
        /// </summary>
        /// <typeparam name="T">The type of object being injected into the cached list. All items of the list must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that the 
        /// <paramref name="value"/> will be appended to.</param>
        /// <param name="index">The zero-based position at which the value must be inserted in the list.</param>
        /// <param name="value">The value to inject into the cached list.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be injected. Otherwise true.</returns>
        bool InjectInList<T>(string cacheEntryKey, long index, T value)
            where T : IConvertible;

        /// <summary>
        /// Injects a new byte array value into an existing cached list at the position specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that the 
        /// <paramref name="value"/> will be appended to.</param>
        /// <param name="index">The zero-based position at which the value must be inserted in the list.</param>
        /// <param name="value">The byte array value to inject into the cached list.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be injected. Otherwise true.</returns>
        bool InjectInList(string cacheEntryKey, long index, byte[] value);

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
        bool ShrinkList(string cacheEntryKey, long firstIndexKept, long lastIndexKept);

        /// <summary>
        /// Removes any matching entries with the same value from an existing cached list.
        /// </summary>
        /// <typeparam name="T">The type of objects that are contained in the cached list.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cached list to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing list and remove.</param>
        /// <returns>Returns -1 list doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached list. Otherwise returns the number of removed items.</returns>
        long RemoveFromList<T>(string cacheEntryKey, T value);

        /// <summary>
        /// Removes any matching entries with the same byte array value from an existing cached list.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cached list to locate and remove the value from.</param>
        /// <param name="value">The value to locate in the existing list and remove.</param>
        /// <returns>Returns -1 list doesn't exist as a cache entry or if the <paramref name="value"/> could not be found in the cached list. Otherwise returns the number of removed items.</returns>
        long RemoveFromList(string cacheEntryKey, byte[] value);

        /// <summary>
        /// Removes all items from an existing cached list.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the list that must be cleared.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the list cannot be cleared. Otherwise true.</returns>
        bool ClearList(string cacheEntryKey);

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
        bool CacheAsDictionary<T>(string cacheEntryKey, IDictionary<string, T> dictionaryItems, TimeSpan expiry)
            where T : IConvertible;

        /// <summary>
        /// Caches the given dictionary of byte array items as a new dictionary in the cache engine.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which will contain the dictionary.</param>
        /// <param name="dictionaryItems">The items to cache as a dictionary.</param>
        /// <param name="expiry">The time period that the data will be valid.</param>
        /// <returns>Returns true if the dictionary was successfully created with all <paramref name="dictionaryItems"/> byte array values cached.</returns>
        bool CacheAsDictionary(string cacheEntryKey, IDictionary<string, byte[]> dictionaryItems, TimeSpan expiry);

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
        bool AddToDictionary<T>(string cacheEntryKey, string dictionaryKey, T value)
            where T : IConvertible;

        /// <summary>
        /// Adds a new dictionary entry for the given byte array value into an existing cached dictionary with 
        /// the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary that the 
        /// <paramref name="value"/> will be added to.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being added.</param>
        /// <param name="value">The byte array value to add into the cached dictionary.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be added. Otherwise true.</returns>
        bool AddToDictionary(string cacheEntryKey, string dictionaryKey, byte[] value);

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
        bool UpdateDictionaryEntry<T>(string cacheEntryKey, string dictionaryKey, T value)
            where T : IConvertible;

        /// <summary>
        /// Updates an existing dictionary entry with the given byte array value in an existing cached 
        /// dictionary for the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being updated.</param>
        /// <param name="value">The value to update in the cached dictionary.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the <paramref name="dictionaryKey"/> cannot
        /// be found or the value cannot be updated. Otherwise true.</returns>
        bool UpdateDictionaryEntry(string cacheEntryKey, string dictionaryKey, byte[] value);

        /// <summary>
        /// Removes a dictionary entry from an existing cached dictionary for the <paramref name="dictionaryKey"/> specified.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being removed.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the <paramref name="dictionaryKey"/> cannot
        /// be removed. Otherwise true.</returns>
        bool RemoveFromDictionary(string cacheEntryKey, string dictionaryKey);

        /// <summary>
        /// Retrieves all entries in a cached dictionary as a new <see cref="IDictionary{TKey, TValue}"/>. 
        /// </summary>
        /// <typeparam name="T">The type of object which was written in the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <returns>Returns the located dictionary from the cache if the key was found. Otherwise null.</returns>
        IDictionary<string, T> RetrieveDictionary<T>(string cacheEntryKey)
            where T : IConvertible;

        /// <summary>
        /// Retrieves all entries in a cached dictionary as a new <see cref="IDictionary{TKey, TValue}"/>. 
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <returns>Returns the located dictionary containing byte array values from the cache if the key was found. Otherwise null.</returns>
        IDictionary<string, byte[]> RetrieveDictionary(string cacheEntryKey);

        /// <summary>
        /// Retrieves a single entry from a cached dictionary located by the <paramref name="dictionaryKey"/>. 
        /// </summary>
        /// <typeparam name="T">The type of object which was written in the cached dictionary. All values of the 
        /// dictionary values must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being sought.</param>
        /// <param name="value">Returns the value found in the dictionary cache. If not found the default value is returned.</param>
        /// <returns>Returns true if the value was located in the cached dictionary. Otherwise false.</returns>
        bool RetrieveDictionaryEntry<T>(string cacheEntryKey, string dictionaryKey, out T value)
            where T : IConvertible;

        /// <summary>
        /// Retrieves a single entry (a byte array) from a cached dictionary located by the <paramref name="dictionaryKey"/>. 
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the dictionary.</param>
        /// <param name="dictionaryKey">The unique name within the dictionary for the value being sought.</param>
        /// <param name="value">Returns the byte array value found in the dictionary cache. If not found then null is returned.</param>
        /// <returns>Returns true if the value was located in the cached dictionary. Otherwise false.</returns>
        bool RetrieveDictionaryEntry(string cacheEntryKey, string dictionaryKey, out byte[] value);

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
        bool CacheAsQueue<T>(string cacheEntryKey, IEnumerable<T> queuedItems, TimeSpan expiry)
            where T : IConvertible;

        /// <summary>
        /// Caches the given enumeration of <paramref name="queuedItems"/> byte arrays as a queue in the cache.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry to create.</param>
        /// <param name="queuedItems">The individual byte array values to store as a queue.</param>
        /// <param name="expiry">The time period that the data will be valid. May be set to never expire by setting <see cref="TimeSpan.MaxValue"/>.</param>
        /// <returns>Returns true if the queue was successfully created with all <paramref name="queuedItems"/> values cached.</returns>
        bool CacheAsQueue(string cacheEntryKey, IEnumerable<byte[]> queuedItems, TimeSpan expiry);

        /// <summary>
        /// Pushes a new value to the end of an existing cached queue.
        /// </summary>
        /// <typeparam name="T">The type of object being pushed to the cached queue. All items of the queue must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">The value to append to the cached queue.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be pushed to the queue. Otherwise true.</returns>
        bool PushQueue<T>(string cacheEntryKey, T value)
            where T : IComparable;

        /// <summary>
        /// Pushes a new byte array to the end of an existing cached queue.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">The value to append to the cached queue.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the value cannot be pushed to the queue. Otherwise true.</returns>
        bool PushQueue(string cacheEntryKey, byte[] value);

        /// <summary>
        /// Pops the next value in the cached queue and returns the value.
        /// </summary>
        /// <typeparam name="T">The type of the objects stored in the cached queue. All items of the queue must be of the same type.</typeparam>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">Returns the next value from the cached queue. If not found then a default value is returned.</param>
        /// <returns>Returns true if the next value in the cached queue was successfully returned in <paramref name="value"/>. Otherwise false.</returns>
        bool PopQueue<T>(string cacheEntryKey, out T value)
            where T : IConvertible;

        /// <summary>
        /// Pops the next byte array in the cached queue and returns the value.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue.</param>
        /// <param name="value">Returns the next byte array value from the cached queue. If not found then null is returned.</param>
        /// <returns>Returns true if the next value in the cached queue was successfully returned in <paramref name="value"/>. Otherwise false.</returns>
        bool PopQueue(string cacheEntryKey, out byte[] value);

        /// <summary>
        /// Removes all items from an existing cached queue.
        /// </summary>
        /// <param name="cacheEntryKey">The unique key of the cache entry which contains the queue that must be cleared.</param>
        /// <returns>Returns false if the cache entry doesn't exist or if the queue cannot be cleared. Otherwise true.</returns>
        bool ClearQueue(string cacheEntryKey);

        #endregion Queue caching
    }
}
