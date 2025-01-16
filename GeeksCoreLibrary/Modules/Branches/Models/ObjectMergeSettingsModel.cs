namespace GeeksCoreLibrary.Modules.Branches.Models;

/// <summary>
/// A model with settings for what changes to merge of an object.
/// </summary>
public class ObjectMergeSettingsModel
{
    /// <summary>
    /// Gets or sets whether to merge the creation of objects of this type.
    /// </summary>
    public bool Create { get; set; }

    /// <summary>
    /// Gets or sets whether to merge updates of objects of this type.
    /// </summary>
    public bool Update { get; set; }

    /// <summary>
    /// Gets or sets whether to merge deletions of objects of this type.
    /// </summary>
    public bool Delete { get; set; }
}