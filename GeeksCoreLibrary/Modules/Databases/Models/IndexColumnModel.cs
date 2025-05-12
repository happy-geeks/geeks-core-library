namespace GeeksCoreLibrary.Modules.Databases.Models;

/// <summary>
/// A model for a database index column.
/// </summary>
public class IndexColumnModel
{
    /// <summary>
    /// The name of the column in the database table.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The length of the column, if applicable. This is used for string columns to specify the maximum length of the index.
    /// </summary>
    public long? Length { get; set; }

    /// <summary>
    /// The sequence order of the column in the index. This is used to determine the order of the columns in the index.
    /// </summary>
    public uint Sequence { get; set; }
}