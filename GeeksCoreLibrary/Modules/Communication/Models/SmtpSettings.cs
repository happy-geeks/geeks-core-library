using GeeksCoreLibrary.Modules.Communication.Enums;
using GeeksCoreLibrary.Modules.Communication.Models.SmtPeter;

namespace GeeksCoreLibrary.Modules.Communication.Models
{
    /// <summary>
    /// The SMTP settings are used for emails that arent using the CommunicationsService.
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// Gets or sets the provider to use for the email communication.
        /// </summary>
        public EmailServiceProviders Provider { get; set; } = EmailServiceProviders.Smtp;

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

        /// <summary>
        /// Gets or sets extra settings for the SmtPeter Rest API.
        /// </summary>
        public SmtPeterSettings SmtPeterSettings { get; set; }
    }
}
