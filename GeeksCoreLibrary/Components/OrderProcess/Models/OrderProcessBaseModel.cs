namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// Base model for all other order process settings models, with properties that they all need.
    /// </summary>
    public abstract class OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the ID of the Wiser item that contains the settings for the order process.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }
    }
}
