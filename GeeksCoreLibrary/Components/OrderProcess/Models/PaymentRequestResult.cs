using GeeksCoreLibrary.Modules.Payments.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models;

public class PaymentRequestResult
{
    /// <summary>
    /// Gets or sets whether the payment request was successful. Note that this means whether the request to perform a payment was successful,
    /// not that the payment itself was successful.
    /// </summary>
    public bool Successful { get; set; }

    /// <summary>
    /// Gets or sets the required action after the payment request has been handled.
    /// </summary>
    public PaymentRequestActions Action { get; set; }

    /// <summary>
    /// Gets or sets the data that accompanies the <see cref="Action"/>, like a URL in the case of a redirect.
    /// </summary>
    public string ActionData { get; set; }

    /// <summary>
    /// Gets or sets the error message, if the request was not successful.
    /// </summary>
    public string ErrorMessage { get; set; }
}