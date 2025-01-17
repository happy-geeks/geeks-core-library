using System;
using System.Data;
using System.Data.Common;

namespace GeeksCoreLibrary.Core.Extensions;

public static class DbDataReaderExtensions
{
    /// <summary>
    /// Gets a string value from a <see cref="DbDataReader"/> and returns an empty string if the value is <see langword="null"/>.
    /// </summary>
    /// <param name="reader">The <see cref="DbDataReader"/> to get the value of.</param>
    /// <param name="columnIndex">The index of the column to get the value of.</param>
    /// <returns>A <see langword="string"/> with the value.</returns>
    public static string GetStringHandleNull(this DbDataReader reader, int columnIndex)
    {
        return reader.IsDBNull(columnIndex) ? String.Empty : reader.GetString(columnIndex);
    }

    /// <summary>
    /// Gets a string value from a <see cref="DbDataReader"/> and returns an empty string if the value is <see langword="null"/>.
    /// </summary>
    /// <param name="reader">The <see cref="DbDataReader"/> to get the value of.</param>
    /// <param name="columnName">The name of the column to get the value of.</param>
    /// <returns>A <see langword="string"/> with the value.</returns>
    public static string GetStringHandleNull(this DbDataReader reader, string columnName)
    {
        if (!reader.HasColumn(columnName))
        {
            return String.Empty;
        }

        return reader.IsDBNull(reader.GetOrdinal(columnName)) ? String.Empty : reader.GetString(columnName);
    }

    /// <summary>
    /// Checks if a columns exists in a data reader.
    /// </summary>
    /// <param name="reader">The <see cref="IDataRecord"/> to check.</param>
    /// <param name="columnName">The name of the column to check.</param>
    /// <returns></returns>
    public static bool HasColumn(this IDataRecord reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}