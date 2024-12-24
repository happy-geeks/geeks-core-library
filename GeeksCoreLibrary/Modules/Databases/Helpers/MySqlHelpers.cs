using System;
using System.Collections.Generic;
using System.Data;
using GeeksCoreLibrary.Core.Exceptions;
using GeeksCoreLibrary.Modules.Databases.Services;
using MySqlConnector;

namespace GeeksCoreLibrary.Modules.Databases.Helpers;

public static class MySqlHelpers
{
    /// <summary>
    /// Gets the list of column mappings for a bulk copy operation for MySQL, from a <see cref="DataTable"/>.
    /// </summary>
    /// <param name="dataTable">The <see cref="DataTable"/> to get the columns of.</param>
    /// <returns>A list of <see cref="MySqlBulkCopyColumnMapping"/>.</returns>
    public static List<MySqlBulkCopyColumnMapping> GetMySqlColumnMappingForBulkCopy(DataTable dataTable)
    {
        var colMappings = new List<MySqlBulkCopyColumnMapping>();
        for (var index = 0; index < dataTable.Columns.Count; index++)
        {
            var column = dataTable.Columns[index];
            colMappings.Add(new MySqlBulkCopyColumnMapping(index, column.ColumnName));
        }

        return colMappings;
    }

    /// <summary>
    /// Check whether the given <see cref="MySqlException"/> is an error that can be fixed by retrying the same operation again.
    /// </summary>
    /// <param name="mySqlException">The exception to check.</param>
    /// <returns>Whether the operation that caused this exception is something that can be fixed by retrying the same operation again.</returns>
    public static bool IsErrorToRetry(MySqlException mySqlException)
    {
        return MySqlDatabaseConnection.MySqlErrorCodesToRetry.Contains(mySqlException.ErrorCode);
    }

    /// <summary>
    /// Check whether the given <see cref="InvalidOperationException"/> is an error that can be fixed by retrying the same operation again.
    /// </summary>
    /// <param name="invalidOperationException">The exception to check.</param>
    /// <returns>Whether the operation that caused this exception is something that can be fixed by retrying the same operation again.</returns>
    public static bool IsErrorToRetry(InvalidOperationException invalidOperationException)
    {
        return invalidOperationException.Message.Contains("This MySqlConnection is already in use", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check whether the given <see cref="GclQueryException"/> is an error that can be fixed by retrying the same operation again.
    /// </summary>
    /// <param name="gclQueryException">The exception to check.</param>
    /// <returns>Whether the operation that caused this exception is something that can be fixed by retrying the same operation again.</returns>
    public static bool IsErrorToRetry(GclQueryException gclQueryException)
    {
        return gclQueryException.InnerException switch
        {
            MySqlException mySqlException => IsErrorToRetry(mySqlException),
            InvalidOperationException invalidOperationException => IsErrorToRetry(invalidOperationException),
            _ => false
        };
    }
}