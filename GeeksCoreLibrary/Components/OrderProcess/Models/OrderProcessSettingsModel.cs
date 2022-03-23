namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// A model for an order/checkout process.
    /// </summary>
    public class OrderProcessSettingsModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the URL for this order process.
        /// </summary>
        public string FixedUrl { get; set; }
    }
}
