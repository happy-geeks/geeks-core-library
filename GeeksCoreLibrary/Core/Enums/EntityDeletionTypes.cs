namespace GeeksCoreLibrary.Core.Enums;

/// <summary>
/// Enum with all possible ways an item can be 'deleted'.
/// </summary>
public enum EntityDeletionTypes
{
    /// <summary>
    /// Move the item to the archive tables.
    /// </summary>
    Archive,
    /// <summary>
    /// Always delete the item permanently.
    /// </summary>
    Permanent,
    /// <summary>
    /// Hide the item instead of deleting it.
    /// </summary>
    Hide,
    /// <summary>
    /// Don't allow items of this type to be deleted.
    /// </summary>
    Disallow
}