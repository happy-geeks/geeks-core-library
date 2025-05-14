using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Databases.Enums;

namespace GeeksCoreLibrary.Modules.Databases.Models;

/// <summary>
/// A model for an index in a database.
/// </summary>
public class IndexSettingsModel
{
    /// <summary>
    /// Creates a new instance of the <see cref="IndexSettingsModel"/> class, without setting any properties.
    /// </summary>
    public IndexSettingsModel()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IndexSettingsModel"/> class with the specified parameters.
    /// </summary>
    /// <param name="tableName">The name of the table that the index belongs to.</param>
    /// <param name="name">The name of the index itself.</param>
    /// <param name="type">The type of index (normal/unique/fulltext).</param>
    /// <param name="columns">The list of database columns that the index is for.</param>
    /// <param name="comment">Optional: A comment to describe what the index is for.</param>
    public IndexSettingsModel(string tableName, string name, IndexTypes type = IndexTypes.Normal, List<IndexColumnModel> columns = null, string comment = null)
    {
        Name = name;
        Type = type;
        TableName = tableName;
        Columns = columns ?? [];
        Comment = comment;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IndexSettingsModel"/> class with the specified parameters.
    /// </summary>
    /// <param name="tableName">The name of the table that the index belongs to.</param>
    /// <param name="name">The name of the index itself.</param>
    /// <param name="type">The type of index (normal/unique/fulltext).</param>
    /// <param name="columns">The list of database columns that the index is for.</param>
    /// <param name="comment">Optional: A comment to describe what the index is for.</param>
    public IndexSettingsModel(string tableName, string name, IndexTypes type = IndexTypes.Normal, List<string> columns = null, string comment = null)
    {
        Name = name;
        Type = type;
        TableName = tableName;
        Comment = comment;

        Columns = [];
        if (columns == null)
        {
            return;
        }

        // Iterate through the list of column names and create IndexColumnModel instances for each one.
        for (var index = 0; index < columns.Count; index++)
        {
            var indexColumn = columns[index];
            var columnName = indexColumn;
            long? length = null;

            // Check if the column name contains a length specification (e.g., "column_name(10)").
            var subPartStart = indexColumn.LastIndexOf('(');
            var subPartEnd = indexColumn.LastIndexOf(')');

            // Check if the field contains both opening and closing parentheses and if the closing parenthesis comes after the opening parenthesis.
            if (subPartStart != -1 && subPartEnd != -1 && subPartStart <= subPartEnd)
            {
                columnName = indexColumn[..subPartStart].Trim();

                // Try to parse the length from the substring between the parentheses, ignore the value if it's not a valid number.
                if (Int64.TryParse(indexColumn[(subPartStart + 1)..subPartEnd], out var parsedLength))
                {
                    length = parsedLength;
                }
            }

            Columns.Add(new IndexColumnModel
            {
                Name = columnName,
                Length = length,
                Sequence = Convert.ToUInt32(index + 1)
            });
        }
    }

    /// <summary>
    /// Gets or sets the name of the index.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of index. Default value is <see cref="IndexTypes.Normal"/>.
    /// </summary>
    public IndexTypes Type { get; set; } = IndexTypes.Normal;

    /// <summary>
    /// The name of the table that this index belongs to.
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// Gets or sets the table columns that are part of this index.
    /// </summary>
    public List<IndexColumnModel> Columns { get; set; } = [];

    /// <summary>
    /// Gets or sets the comment describing the index.
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// Gets or sets whether the index is visible in the database.
    /// See the following article for more information: https://dev.mysql.com/doc/refman/8.4/en/invisible-indexes.html
    /// </summary>
    public bool IsVisible { get; set; } = true;
}