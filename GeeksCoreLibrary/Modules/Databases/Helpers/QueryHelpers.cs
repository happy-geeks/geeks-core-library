using System;

namespace GeeksCoreLibrary.Modules.Databases.Helpers;

public static class QueryHelpers
{
    /// <summary>
    /// Determines if the given query is a write query.
    /// </summary>
    /// <param name="query">The query to check.</param>
    /// <returns>True if the query is a write query, otherwise false.</returns>
    public static bool IsWriteQuery(string query)
    {
        if (String.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        return query.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
               query.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) ||
               query.Contains("DELETE", StringComparison.OrdinalIgnoreCase) ||
               query.Contains("ALTER", StringComparison.OrdinalIgnoreCase) ||
               query.Contains("CREATE", StringComparison.OrdinalIgnoreCase) ||
               query.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
               query.Contains("TRUNCATE", StringComparison.OrdinalIgnoreCase) ||
               query.Contains("OPTIMIZE", StringComparison.OrdinalIgnoreCase) ||
               query.Contains("REPLACE INTO", StringComparison.OrdinalIgnoreCase);
    }
}