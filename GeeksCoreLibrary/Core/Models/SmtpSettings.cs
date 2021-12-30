namespace GeeksCoreLibrary.Core.Models
{
    /// <summary>
    /// The SMTP settings are used for emails that arent using the CommunicationsService.
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// Gets or sets the host. Can be a DNS-resolvable hostname or a valid IP-address.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the username that will be used to authenticate with the SMTP server.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password that will be used to authenticate with the SMTP server.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets whether SSL should be used.
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port { get; set; }
    }
}
