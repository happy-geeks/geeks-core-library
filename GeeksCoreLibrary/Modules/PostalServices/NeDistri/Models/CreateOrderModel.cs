using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

/// <summary>
/// Model for creating an order with NE DistriService
/// </summary>
public class CreateOrderModel
{
    /// <summary>
    /// Gets or sets the user code.
    /// Can be used if there are multiple user / shipper codes to choose from.
    /// </summary>
    public int? UserCode { get; set; }
    
    /// <summary>
    /// Gets or sets the recipient address
    /// </summary>
    public AddressModel Address { get; set; }
    
    /// <summary>
    /// Gets or sets units timestamp for pick-up date. Optional: can be left empty for first possible date.
    /// </summary>
    public long? LoadDate { get; set; }
    
    /// <summary>
    /// Gets or sets instruction for load address
    /// </summary>
    public string LoadRemarks { get; set; }
    
    /// <summary>
    /// Gets or sets ordertype
    /// </summary>
    public OrderType OrderType { get; set; }
    
    /// <summary>
    /// Gets or sets list of one, two or three references like an invoice number or client number
    /// </summary>
    public ICollection<string> Reference { get; set; }
    
    /// <summary>
    /// Gets or sets extra remarks for the order
    /// </summary>
    public string Remarks { get; set; }
    
    /// <summary>
    /// Gets or sets collection of rule objects, listing parcels and their details
    /// </summary>
    public ICollection<RuleModel> Rules { get; set; }
    
    /// <summary>
    /// Gets or sets Units timestamp for for unload date. Optional: can be left empty for first possible date.
    /// </summary>
    public long? UnloadDate { get; set; }
    
    /// <summary>
    /// Gets or sets Instruction for unload address
    /// </summary>
    public string UnloadRemarks { get; set; }
}