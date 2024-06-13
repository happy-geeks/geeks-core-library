namespace GeeksCoreLibrary.Core.Enums;

/// <summary>
/// Different ways to handle the duplication of items.
/// </summary>
public enum LinkDuplicationMethods
{
    /// <summary>
    /// Only the item itself will be duplicated, any items linked to the item will not be.
    /// </summary>
    None,
        
    /// <summary>
    /// The item itself and all links will be duplicated.
    /// For example, if item X is being duplicated, the new item will be item Z. Item Y was already linked to item X. Then item Y will also be linked to item Z. 
    /// </summary>
    CopyLink,
        
    /// <summary>
    /// The item itself and all linked items will be duplicated.
    /// For example, if item X is being duplicated, the new item wll be item Z. Item Y is linked to item X. Then item Y will also be duplicated and that duplicated item will be linked to item Z. 
    /// </summary>
    CopyItem
}