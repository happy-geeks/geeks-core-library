namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

public class BarcodeResponse
{
    /// <summary>
    /// Gets or sets the id of the given rule
    /// </summary>
    public int RuleId { get; set; }
    
    /// <summary>
    /// Gets or sets the colli number
    /// </summary>
    public int ColiNumber { get; set; }
    
    /// <summary>
    /// Gets or sets string containing the barcode of the label
    /// </summary>
    public string Barcode { get; set; }
    
    /// <summary>
    /// Gets or sets the PDF file containing the label formatted as Base64 string
    /// </summary>
    public string Attachment { get; set; }
}