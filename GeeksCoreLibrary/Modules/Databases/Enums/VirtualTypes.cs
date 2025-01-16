namespace GeeksCoreLibrary.Modules.Databases.Enums;

/// <summary>
/// The types of virtual columns for columns that are not stored in the database.
/// </summary>
public enum VirtualTypes
{
    /// <summary>
    /// Column values are not stored, but are evaluated when rows are read, immediately after any BEFORE triggers. A virtual column takes no storage.
    /// </summary>
    Virtual,

    /// <summary>
    /// Column values are evaluated and stored when rows are inserted or updated. A stored column does require storage space and can be indexed.
    /// </summary>
    Stored
}