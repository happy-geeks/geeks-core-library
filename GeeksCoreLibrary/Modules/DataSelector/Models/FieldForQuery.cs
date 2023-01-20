namespace GeeksCoreLibrary.Modules.DataSelector.Models;

public class FieldForQuery
{
    /// <summary>
    /// Gets or sets a reference to the Field object this item was created for.
    /// </summary>
    public Field Field { get; set; }

    public string JoinQueryPart { get; set; }

    public string SelectQueryPart { get; set; }

    public string ScopesQueryPart { get; set; }
}