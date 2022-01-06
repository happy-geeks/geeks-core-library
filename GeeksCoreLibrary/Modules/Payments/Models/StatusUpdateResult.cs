namespace GeeksCoreLibrary.Modules.Payments.Models
{
    public class StatusUpdateResult
    {
        /// <summary>
        /// Gets or sets whether the payment was successful.
        /// </summary>
        public bool Successful { get; set; }

        /// <summary>
        /// Gets or sets the status text or number that the PSP gave us.
        /// </summary>
        public string Status { get; set; }
    }
}