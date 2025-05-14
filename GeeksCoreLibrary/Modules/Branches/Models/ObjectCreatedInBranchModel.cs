using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Databases.Models;

namespace GeeksCoreLibrary.Modules.Branches.Models;

/// <summary>
/// A simple model for keeping track of objects that have been created and deleted in the branch.
/// </summary>
public class ObjectCreatedInBranchModel
{
    /// <summary>
    /// Gets or sets the ID of the object in the branch database.
    /// </summary>
    public string ObjectId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the object in the production database.
    /// </summary>
    public string ObjectIdMapped { get; set; }

    /// <summary>
    /// Gets or sets the name of the table that contains the object.
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// Gets or sets whether or not the item was deleted.
    /// </summary>
    public bool AlsoDeleted { get; set; }

    /// <summary>
    /// Gets or sets whether the item was undeleted.
    /// </summary>
    public bool AlsoUndeleted { get; set; }

    /// <summary>
    /// The columns that we generated temporary values for when creating the object in production.
    /// We need to keep track of these columns, so that we can remove the temporary values after we're done merging.
    /// </summary>
    public List<ColumnSettingsModel> ColumnsWithTemporaryValues { get; set; } = [];
}