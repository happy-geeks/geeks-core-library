using GeeksCoreLibrary.Modules.Payments.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models;

public class PaymentReturnResult
{
    /// <summary>
    /// Gets or sets the required action after the payment request has been handled.
    /// </summary>
    public PaymentResultActions Action { get; set; }

    /// <summary>
    /// Gets or sets the data that accompanies the <see cref="Action"/>, like a URL in the case of a redirect.
    /// </summary>
    public string ActionData { get; set; }
}