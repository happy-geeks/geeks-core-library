namespace GeeksCoreLibrary.Core.Enums;

/// <summary>
/// The different aggregation methods we support for aggregating values of Wiser items.
/// </summary>
public enum WiserItemPropertyAggregateMethods
{
    /// <summary>
    /// Save the data as is.
    /// </summary>
    None,
        
    /// <summary>
    /// Calculate the sum of all items with the same parent and link type.
    /// </summary>
    Sum,
        
    /// <summary>
    /// Calculate the lowest value of all items with the same parent and link type.
    /// </summary>
    Min,
        
    /// <summary>
    /// Calculate the highest value of all items with the same parent and link type.
    /// </summary>
    Max,
        
    /// <summary>
    /// Calculate the average value of all items with the same parent and link type.
    /// </summary>
    Average
}