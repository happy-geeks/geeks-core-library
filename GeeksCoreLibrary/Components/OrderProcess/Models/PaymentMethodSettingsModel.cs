using GeeksCoreLibrary.Components.OrderProcess.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models;

/// <summary>
/// A model for settings for a payment method.
/// </summary>
public class PaymentMethodSettingsModel : OrderProcessBaseModel
{
    /// <summary>
    /// Gets or sets the external name for the payment method. This is the name that will be sent to the PSP.
    /// </summary>
    public string ExternalName { get; set; }

    /// <summary>
    /// Gets or sets the PSP that should be used for this payment method.
    /// </summary>
    public PaymentServiceProviderSettingsModel PaymentServiceProvider { get; set; }

    /// <summary>
    /// Gets or sets the fee that the user needs to pay to use this payment method.
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// Gets or sets when the field should be visible.
    /// </summary>
    public OrderProcessFieldVisibilityTypes Visibility { get; set; }

    /// <summary>
    /// Gets or sets the check for seeing if we need a minimal amount before this payment method is available.
    /// </summary>
    public bool UseMinimalAmountCheck { get; set; }

    /// <summary>
    /// Gets or sets the check for making this payment method unavailable if we go over a certain amount 
    /// </summary>
    public bool UseMaximumAmountCheck { get; set; }

    /// <summary>
    /// Gets or sets the amount for the minimal check.
    /// </summary>
    public decimal MinimalAmountCheck { get; set; }

    /// <summary>
    /// Gets or sets the amount for the maximum check. 
    /// </summary>
    public decimal MaximumAmountCheck { get; set; }
}