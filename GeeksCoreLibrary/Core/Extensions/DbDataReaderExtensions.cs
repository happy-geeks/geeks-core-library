using System;
using System.Data;
using System.Data.Common;

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class DbDataReaderExtensions
    {
        public static string GetStringHandleNull(this DbDataReader reader, int colIndex)
        {
            return reader.IsDBNull(colIndex) ? String.Empty : reader.GetString(colIndex);
        }

        public static string GetStringHandleNull(this DbDataReader reader, string columnName)
        {
            return reader.IsDBNull(reader.GetOrdinal(columnName)) ? String.Empty : reader.GetString(columnName);
        }
    }
}
