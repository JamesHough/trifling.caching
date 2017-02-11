// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching.Impl
{
    using System.Collections.Generic;

    /// <summary>
    /// A comparer for comparing the content of two byte arrays.
    /// </summary>
    internal class ByteArrayComparer : IComparer<object>
    {
        /// <summary>
        /// Gets the default instance of the <see cref="ByteArrayComparer"/>. 
        /// </summary>
        public static ByteArrayComparer Default
        {
            get { return new ByteArrayComparer(); }
        }

        /// <summary>
        /// Compares two objects as byte arrays and returns their relative position.
        /// </summary>
        /// <param name="x">The first byte array to compare.</param>
        /// <param name="y">The second byte array to compare.</param>
        /// <returns>Returns -1 if <paramref name="x"/> is before <paramref name="y"/>. Returns 0 if they are equal. Otherwise returns 1.</returns>
        public int Compare(object x, object y)
        {
            if (!(x is byte[]) && !(y is byte[]))
            {
                return 0;
            }

            if (!(x is byte[]))
            {
                return -1;
            }

            if (!(y is byte[]))
            {
                return 1;
            }

            var a = (byte[])x;
            var b = (byte[])y;

            if (a == null && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return -1;
            }

            if (b == null)
            {
                return 1;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (b.Length <= i)
                {
                    return -1;
                }

                var comp = b[i].CompareTo(a[i]);

                if (comp != 0)
                {
                    return comp;
                }
            }

            if (b.Length > a.Length)
            {
                return 1;
            }

            return 0;
        }
    }
}
