using System;
using System.Text;

namespace GeeksCoreLibrary.Components.OrderProcess.Models;

public class PayNLSettingsModel : PaymentServiceProviderSettingsModel
{
    /// <summary>
    /// Gets or sets the username.
    /// This can either be with an AT code and an SL code
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// This is a token if the username is an AT code or a secret if the username is a SL code
    /// </summary>
    public string Password { get; set; }
    
    /// <summary>
    /// Gets or sets the Service ID. Required if logging in with an AT-code/token
    /// </summary>
    public string ServiceId { get; set; }
}