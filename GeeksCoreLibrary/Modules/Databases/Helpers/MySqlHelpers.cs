using System.Collections.Generic;
using System.Data;
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
}