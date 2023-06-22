namespace GeeksCoreLibrary.Modules.Payments.Models.PayNL;

/// <summary>
/// Data model used in requests to start a PayNL transaction
/// </summary>
public class TransactionStartBody
{
    /// <summary>
    /// The service Id connected to the marketer
    /// </summary>
    public string ServiceId { get; set; }
    
    /// <summary>
    /// The price that needs to be paid to complete the transaction
    /// </summary>
    public Amount Amount { get; set; }
    
    /// <summary>
    /// Gets or Sets the description that is shown on the back statement of the customer
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the url Pay.nl needs to return to after the transaction is started
    /// </summary>
    public string ReturnUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the url pay.nl will use to send status updates for this transaction
    /// </summary>
    public string ExchangeUrl { get; set; }
    
    /// <summary>
    /// Gets or sets model containing values used during integration of the PayNL payment provider
    /// </summary>
    public Integration Integration { get; set; }
}