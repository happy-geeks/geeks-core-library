namespace GeeksCoreLibrary.Modules.Databases.Models;

public class DocumentStoreIndexFieldModel
{
    /// <summary>
    /// The name of the field in JSON format, e.g.: <c>$.userId</c>
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Gets or sets the type. If it's a TEXT field, a prefix length must be included, e.g.: <c>TEXT(10)</c>
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets whether this index field is required, meaning that this field must exist in the document.
    /// </summary>
    public bool Required { get; set; }
}