namespace GeeksCoreLibrary.Modules.Templates.Models;

public class QueryGroupingSettings
{
    /// <summary>
    /// Gets or sets the grouping column;
    /// If you want to have JSON with 2 levels, enter the name of the column here for grouping the data.
    /// </summary>
    public string GroupingColumn { get; set; }

    /// <summary>
    /// Gets or sets the grouping fields prefix;
    /// If you want to have JSON with 2 levels, enter the prefix of all fields that should show up on the second level.
    /// </summary>
    public string GroupingFieldsPrefix { get; set; }

    /// <summary>
    /// Gets or sets whether grouping the data should create a multi level object, instead of an array of objects.
    /// </summary>
    public bool ObjectInsteadOfArray { get; set; }

    /// <summary>
    /// Gets or sets the grouping key column name. Only used if <see cref="ObjectInsteadOfArray"/> is set to true.
    /// </summary>
    public string GroupingKeyColumnName { get; set; }

    /// <summary>
    /// Gets or sets the grouping value column name. Only used if <see cref="ObjectInsteadOfArray"/> is set to true.
    /// </summary>
    public string GroupingValueColumnName { get; set; }
}