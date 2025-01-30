namespace GeeksCoreLibrary.Modules.ItemFiles.Enums;

/// <summary>
/// The types of file lookups that can be performed.
/// </summary>
public enum FileLookupTypes
{
    /// <summary>
    /// Get a file linked to a Wiser Item, by the ID of that item.
    /// </summary>
    ItemId,

    /// <summary>
    /// Get a file linked to a Wiser Item, by the ID of the file itself.
    /// </summary>
    ItemFileId,

    /// <summary>
    /// Get a file linked to a Wiser Item, by the name of the file itself.
    /// </summary>
    ItemFileName,

    /// <summary>
    /// Get a file linked to a Wiser item link, by the ID of that item link.
    /// </summary>
    ItemLinkId,

    /// <summary>
    /// Get a file linked to a Wiser item link, by the ID of the file itself.
    /// </summary>
    ItemLinkFileId,

    /// <summary>
    /// Get a file linked to a Wiser item link, by the name of the file itself.
    /// </summary>
    ItemLinkFileName
}