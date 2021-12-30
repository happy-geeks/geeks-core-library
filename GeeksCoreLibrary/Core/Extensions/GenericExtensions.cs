using System;
using System.Collections.Generic;
using System.Linq;

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class GenericExtensions
    {
        /// <summary>
        /// Determines if an element is contained in a specified sequence by using the default equality comparer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="values">The values to compare this against.</param>
        /// <returns></returns>
        public static bool InList<T>(this T input, params T[] values) where T : IComparable
        {
            return values.Contains(input);
        }

        /// <summary>
        /// Determines if an element is contained in a specified sequence by using a specified <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="comparer"></param>
        /// <param name="values">The values to compare this against.</param>
        /// <returns></returns>
        public static bool InList<T>(this T input, IEqualityComparer<T> comparer, params T[] values) where T : IComparable
        {
            return values.Contains(input, comparer);
        }
    }
}
