// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching
{
    using System;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// A class which uniquely identifies a cache entry with flexible identification criteria.
    /// </summary>
    /// <remarks>The meaning of the Key Type and Key Elements provided is determined by the caller and uniquely
    /// identifies an object being cached. There is no defined standard. An example might be a key containing
    /// all provinces called "provinces" or "provinces/all". Another example might be a customer's account
    /// balance at a particular date in the key "customer/234121/bal/2016-12-01".</remarks>
    public class CacheEntryKey : IEquatable<CacheEntryKey>, IComparable<CacheEntryKey>, IComparable
    {
        #region private fields

        /// <summary>
        /// The type that key this cache key entry represents. Context provided by caller.
        /// </summary>
        private string _keyType;

        /// <summary>
        /// The parts of the key that uniquely identify this key instance.
        /// </summary>
        private object[] _keyElements;

        #endregion private fields

        #region constructors

        /// <summary>
        /// Initialises a new instance of the <see cref="CacheEntryKey"/> class.
        /// </summary>
        /// <param name="keyType">The type that this cache entry key represents.</param>
        /// <param name="keyElements">The parts of the key that uniquely identify this instance of cache entry key
        /// from another with the same <paramref name="keyType"/>.</param>
        public CacheEntryKey(string keyType, params object[] keyElements)
        {
            this._keyType = keyType;
            this._keyElements = keyElements;
        }

        #endregion constructors

        #region Implicit conversion

        /// <summary>
        /// Implicitly converts an instance of <see cref="CacheEntryKey"/> to a string. 
        /// </summary>
        /// <param name="key">The cache entry key that will be converted to a string.</param>
        public static implicit operator string(CacheEntryKey key)
        {
            return key.ToString();
        }

        #endregion Implicit conversion

        #region public methods

        /// <summary>
        /// Creates a string representation of the current <see cref="CacheEntryKey"/>. 
        /// </summary>
        /// <returns>A string created by concatenating the key elements with forward-slashes.</returns>
        public override string ToString()
        {
            return (this._keyElements != null && this._keyElements.Length > 0)
                ? string.Concat(this._keyType, "/", string.Join("/", this._keyElements.Select(FormatKeyElement)))
                : this._keyType;
        }

        #endregion public methods

        #region IEquatable interface

        /// <summary>
        /// Generates a hash key to describe the value of the current instance.
        /// </summary>
        /// <returns>Returns an integer value for comparison to other instances.</returns>
        public override int GetHashCode()
        {
            var result = this._keyType.GetHashCode();

            if (this._keyElements == null)
            {
                return result;
            }

            unchecked
            {
                const int Prime = 19;
                for (var i = 0; i < this._keyElements.Length; i++)
                {
                    if (this._keyElements[i] == null)
                    {
                        result = (result * Prime) - 1090;
                    }
                    else
                    {
                        result = (result * Prime) + this._keyElements[i].GetHashCode();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Evaluates if the current <see cref="CacheEntryKey"/> is equal to the given <paramref name="obj"/> and returns 
        /// true if they are. 
        /// </summary>
        /// <param name="obj">The object to which the current <see cref="CacheEntryKey"/> is compared.</param>
        /// <returns>Return true if the current object and the given object have the same value.</returns>
        public override bool Equals(object obj)
        {
            return this.CompareTo(obj) == 0;
        }

        /// <summary>
        /// Evaluates if the current <see cref="CacheEntryKey"/> is equal to the given <paramref name="other"/> instance 
        /// and returns true if they are. 
        /// </summary>
        /// <param name="other">The instance to which the current <see cref="CacheEntryKey"/> is compared.</param>
        /// <returns>Return true if the current instance and the given instance have the same value.</returns>
        public bool Equals(CacheEntryKey other)
        {
            return this.CompareTo(other) == 0;
        }

        #endregion IEquatable interface

        #region IComparable interface

        /// <summary>
        /// Compares the relative sorted position of the current <see cref="CacheEntryKey"/> instance to the given
        /// <paramref name="other"/> instance.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>If the current instance is before the given <paramref name="other"/> instance, then a negative number
        /// is returned.</item>
        /// <item>If the current instance is after the given <paramref name="other"/> instance, then a positive number 
        /// is returned.</item>
        /// <item>If both instances have the same value then zero is returned.</item>
        /// </list>
        /// </remarks>
        /// <param name="other">The instance to which the current instance must be compared.</param>
        /// <returns>Returns an integer describing the relative position. Refer to remarks.</returns>
        public int CompareTo(CacheEntryKey other)
        {
            if (other == null)
            {
                return 1;
            }

            // compare key type first. if this differs then use that difference.
            var comp = string.CompareOrdinal(this._keyType, other._keyType);

            if (comp != 0)
            {
                return comp;
            }

            // compare the key elements. if both have no elements then they are the same.
            if ((this._keyElements == null || this._keyElements.Length == 0)
                && (other._keyElements == null || other._keyElements.Length == 0))
            {
                return 0;
            }

            // if one has elements and the other does not then there is a difference.
            if ((this._keyElements == null)
                && (other._keyElements != null && other._keyElements.Length > 0))
            {
                return -1;
            }

            if ((other._keyElements == null)
                && (this._keyElements != null && this._keyElements.Length > 0))
            {
                return 1;
            }

            // both have at least some elements. compare each element to find a difference.
            for (var i = 0; i < this._keyElements.Length; i++)
            {
                // if the other doesn't have as many elements as this instance, then this instance is after other.
                if (other._keyElements.Length < i + 1)
                {
                    return 1;
                }

                comp = string.CompareOrdinal(
                    FormatKeyElement(this._keyElements[i]),
                    FormatKeyElement(other._keyElements[i]));

                if (comp != 0)
                {
                    // this arbitrary key in the array has a value that differs, use that difference.
                    return comp;
                }
            }

            // we have exhausted the list of elements on THIS instance.  If the element length is
            // shorter than the other's length, then this is before.  Otherwise it must be the same
            // after going through the loop above.
            return (this._keyElements.Length < other._keyElements.Length)
                ? -1
                : 0;
        }

        /// <summary>
        /// Compares the relative sorted position of the current <see cref="CacheEntryKey"/> instance to the given
        /// object.
        /// </summary>
        /// <exception cref="InvalidCastException">Exception occurs if the given object cannot be converted to a 
        /// <see cref="CacheEntryKey"/> instance.</exception>
        /// <remarks>
        /// <list type="bullet">
        /// <item>If the current instance is before the given <paramref name="other"/> instance, then a negative number
        /// is returned.</item>
        /// <item>If the current instance is after the given <paramref name="other"/> instance, then a positive number 
        /// is returned.</item>
        /// <item>If both instances have the same value then zero is returned.</item>
        /// </list>
        /// </remarks>
        /// <param name="obj">The object to which the current instance must be compared. If the object cannot be cast
        /// to a<see cref="CacheEntryKey"/> then an exception will occur.</param>
        /// <returns>Returns an integer describing the relative position. Refer to remarks.</returns>
        public int CompareTo(object obj)
        {
            var other = obj as CacheEntryKey;
            if (other == null)
            {
                throw new InvalidCastException();
            }

            return this.CompareTo(other);
        }

        #endregion IComparable interface

        #region Private methods

        /// <summary>
        /// Formats the given date as a string. If no time is present on the date, then the format will be "yyyy-MM-dd"
        /// but if there is a time then the universal date format is used "yyyy-MM-ddTHH:mm:ss.fffffff".
        /// </summary>
        /// <param name="dateTime">The date value to be formatted as a string.</param>
        /// <returns>Returns the given date as a string.</returns>
        private static string FormatKeyElementAsDate(DateTime dateTime)
        {
            return (dateTime.TimeOfDay == TimeSpan.Zero)
                ? string.Format(DateTimeFormatInfo.InvariantInfo, "{0:yyyy-MM-dd}", dateTime)
                : string.Format(DateTimeFormatInfo.InvariantInfo, "{0:o}", dateTime);
        }

        /// <summary>
        /// Formats the given date as a string. If no time is present on the date, then the format will be "yyyy-MM-dd (%K)"
        /// but if there is a time then the universal date format is used "yyyy-MM-ddTHH:mm:ss.fffffff%K".
        /// </summary>
        /// <param name="dateTimeOffset">The date value to be formatted as a string.</param>
        /// <returns>Returns the given date as a string.</returns>
        private static string FormatKeyElementAsDateOffset(DateTimeOffset dateTimeOffset)
        {
            return (dateTimeOffset.TimeOfDay == TimeSpan.Zero)
                ? string.Format(DateTimeFormatInfo.InvariantInfo, "{0:yyyy-MM-dd '('%K')'}", dateTimeOffset)
                : string.Format(DateTimeFormatInfo.InvariantInfo, "{0:o}", dateTimeOffset);
        }

        /// <summary>
        /// Formats a key element as a string. All objects are formatted according to Invariant Culture.
        /// For details of date formats, refer to <see cref="FormatKeyElementAsDate(DateTime)"/> and
        /// <see cref="FormatKeyElementAsDateOffset(DateTimeOffset)"/>. 
        /// </summary>
        /// <param name="element">The element to format as a string.</param>
        /// <returns>Returns a string containing the value given.</returns>
        private static string FormatKeyElement(object element)
        {
            // we handle dates (and nullable dates) differently to all other types.
            if (element is DateTimeOffset)
            {
                return FormatKeyElementAsDateOffset((DateTimeOffset)element);
            }

            if (element is DateTimeOffset?)
            {
                var date = (DateTimeOffset?)element;
                return date.HasValue
                    ? FormatKeyElementAsDateOffset(date.Value)
                    : string.Empty;
            }

            if (element is DateTime)
            {
                return FormatKeyElementAsDate((DateTime)element);
            }

            if (element is DateTime?)
            {
                var date = (DateTime?)element;
                return date.HasValue
                    ? FormatKeyElementAsDate(date.Value)
                    : string.Empty;
            }

            // all other types have standard formatting applied.
            return Convert.ToString(element, CultureInfo.InvariantCulture);
        }

        #endregion Private methods
    }
}
