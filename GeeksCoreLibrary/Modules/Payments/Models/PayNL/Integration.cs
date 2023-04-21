namespace GeeksCoreLibrary.Modules.Payments.Models.PayNL;

/// <summary>
/// PayNL model to set values used during integration of the PayNL payment provider
/// </summary>
public class Integration
{
    /// <summary>
    /// Gets or Sets whether the request is in testmode.
    /// If true payments will be send to the pay nl sandbox where payments can be simulated
    /// </summary>
    public bool TestMode { get; set; }
}