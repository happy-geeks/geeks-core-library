namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

/// <summary>
/// Datamodel for labels containing data about label types and the amount of colis of that type.
/// </summary>
public class LabelRule
{
    /// <summary>
    /// Gets or sets the labeltype
    /// </summary>
    public string LabelType { get; set; }
    
    /// <summary>
    /// Gets or sets the amount of colis of this labeltype
    /// </summary>
    public int ColiAmount { get; set; }
}