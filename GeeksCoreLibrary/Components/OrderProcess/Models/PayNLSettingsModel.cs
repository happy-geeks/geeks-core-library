using System;
using System.Text;

namespace GeeksCoreLibrary.Components.OrderProcess.Models;

public class PayNLSettingsModel : PaymentServiceProviderSettingsModel
{
    /// <summary>
    /// Gets or sets the API account code
    /// </summary>
    public string ApiCode { get; set; }
    
    
    /// <summary>
    /// Gets or sets the API key for the current environment.
    /// </summary>
    public string ApiKey { get; set; }
}