namespace GeeksCoreLibrary.Modules.Payments.Models.PayNL;

/// <summary>
/// Model for prices in PayNL requests
/// </summary>
public class Amount
{
    /// <summary>
    /// Gets or sets the price value in cents
    /// </summary>
    public int Value { get; set; }
    
    /// <summary>
    /// Gets or Sets the currency in ISO 4217 three letter codes
    /// For example EUR for euro
    /// </summary>
    public string Currency { get; set; }
}