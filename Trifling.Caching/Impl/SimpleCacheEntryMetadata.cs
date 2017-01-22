// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Impl
{
    using System;

    /// <summary>
    /// A cache entry metadata class that indexes information about each key entry.
    /// </summary>
    internal class SimpleCacheEntryMetadata : IEquatable<SimpleCacheEntryMetadata>, IComparable<SimpleCacheEntryMetadata>, IComparable
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SimpleCacheEntryMetadata"/> class. 
        /// </summary>
        /// <param name="key">The unique key of the cache entry.</param>
        /// <param name="expireTimeUtc">The date and time that the cache entry will expire (Universal time).</param>
        public SimpleCacheEntryMetadata(string key, DateTime expireTimeUtc)
        {
            this.Key = key;
            this.ExpireTimeUtc = expireTimeUtc;
        }

        /// <summary>
        /// Gets the unique key of the cache entry.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets the date and time that the cache entry will expire (Universal time).
        /// </summary>
        public DateTime ExpireTimeUtc { get; private set; }

        #region IEquatable interface

        /// <summary>
        /// Evaluates if the value of the current object is equal to the given object.
        /// </summary>
        /// <param name="other">The instance to which this instance is compared.</param>
        /// <returns>Returns true if the objects are equal, otherwise false.</returns>
        public bool Equals(SimpleCacheEntryMetadata other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(this.Key, other.Key, StringComparison.Ordinal);
        }

        /// <summary>
        /// Evaluates if the value of the current object is equal to the given object.
        /// </summary>
        /// <param name="obj">The object to which this instance is compared.</param>
        /// <returns>Returns true if the objects are equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is string)
            {
                return string.Equals(this.Key, (string)obj, StringComparison.Ordinal);
            }

            var other = obj as SimpleCacheEntryMetadata;
            return (other == null)
                ? false
                : this.Equals(other);
        }

        /// <summary>
        /// Generates a unique hash code for the current instance's key value.
        /// </summary>
        /// <returns>Returns an integer based on the value of the current key value.</returns>
        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(this.Key)
                ? 0
                : this.Key.GetHashCode();
        }

        #endregion IEquatable interface

        #region IComparable interface

        /// <summary>
        /// Compares the current instance to the given instance to find their relative position.
        /// </summary>
        /// <param name="other">The instance to which the current instance is being compared.</param>
        /// <returns>Returns the relative position of the current item.</returns>
        public int CompareTo(SimpleCacheEntryMetadata other)
        {
            if (other == null)
            {
                return 1;
            }

            return this.Key.CompareTo(other.Key);
        }

        /// <summary>
        /// Compares the current instance to the given instance to find their relative position.
        /// </summary>
        /// <param name="obj">The instance to which the current instance is being compared.</param>
        /// <returns>Returns the relative position of the current item.</returns>
        /// <remarks>Can compare the current instance of <see cref="SimpleCacheEntryMetadata"/> to a string if <param name="obj"/> is a string.</remarks>
        public int CompareTo(object obj)
        {
            // we can compare directly to the key if the comparison is done on a string.
            if (obj is string)
            {
                return string.CompareOrdinal(this.Key, (string)obj);
            }

            var other = obj as SimpleCacheEntryMetadata;
            return (other == null)
                ? 1
                : this.CompareTo(other);
        }

        #endregion IComparable interface
    }
}
