using System;
using GeeksCoreLibrary.Modules.Databases.Enums;

namespace GeeksCoreLibrary.Modules.Databases.Extensions
{
    public static class IndexTypesExtensions
    {
        /// <summary>
        /// Converts an <see cref="IndexTypes"/> to a string that can be used in MySQL for creating an index of this type.
        /// </summary>
        /// <param name="value">The value to get the MySQL string of.</param>
        /// <returns>The index type as MySQL string.</returns>
        public static string ToMySqlString(this IndexTypes value)
        {
            return value switch
            {
                IndexTypes.Normal => "",
                IndexTypes.Unique => "UNIQUE",
                IndexTypes.FullText => "FULLTEXT",
                _ => throw new ArgumentOutOfRangeException(nameof(IndexTypes), value, null)
            };
        }
    }
}
