using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

/// <summary>
/// Rule data model for NE Distri Service
/// </summary>
public class RuleModel
{
    /// <summary>
    /// Gets or sets the amount of colli for this rule
    /// </summary>
    public int Amount { get; set; }
    
    /// <summary>
    /// Gets or sets list of barcodes. Optional.
    /// </summary>
    /// <remarks>Leave empty to let the system generate barcodes for you. If set, amount of barcodes MUST batch amount field.</remarks>
    public ICollection<BarcodeModel> Barcodes { get; set; }
    
    /// <summary>
    /// Gets or the description for this rule. Optional
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the parcel unit. Use the values as described in the <a href="https://orders.ne.nl/apidoc/orders/#units">documentation</a>
    /// </summary>
    public string Unit { get; set; }
    
    /// <summary>
    /// Gets or sets the length of the parcel in cm. Optional
    /// </summary>
    public int? Length { get; set; }
    
    /// <summary>
    /// Gets or sets the width of the parcel in cm. Optional
    /// </summary>
    public int? Width { get; set; }
    
    /// <summary>
    /// Gets or sets the height of the parcel in cm. Optional
    /// </summary>
    public int? Height { get; set; }
    
    /// <summary>
    /// Gets or sets sum of weight in kg for all colli in this rule. Optional
    /// </summary>
    /// <remarks>Weight per item is calculated like weight / amount.</remarks>>
    public int? Weight { get; set; }
}