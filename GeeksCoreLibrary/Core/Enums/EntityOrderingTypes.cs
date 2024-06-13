namespace GeeksCoreLibrary.Core.Enums;

/// <summary>
/// Enum with all possible ways to order items in a tree view in Wiser.
/// </summary>
public enum EntityOrderingTypes
{
    /// <summary>
    /// Order items via the ordering column of wiser_itemlink or wiser_item.
    /// </summary>
    LinkOrdering,
    /// <summary>
    /// Order items via the title column of wiser_item.
    /// </summary>
    ItemTitle
}