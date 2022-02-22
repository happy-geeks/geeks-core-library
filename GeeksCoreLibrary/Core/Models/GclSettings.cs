using System;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;

namespace GeeksCoreLibrary.Core.Models
{
    public class GclSettings
    {
        public static GclSettings Current { get; private set; }

        public GclSettings()
        {
            Current = this;
        }

        /// <summary>
        /// The default connection string for the current website.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The default connection string for the current website. This will be used for queries that write data.
        /// </summary>
        public string ConnectionStringForWriting { get; set; }

        /// <summary>
        /// The maximum amount of times the GCL should retry executing a query.
        /// The GCL only does this for the following MySQL error codes, any other error will not cause another attempt:
        /// <list type="ErrorCodes">
        ///     <item>
        ///         <term>1213</term>
        ///         <description>Deadlock found when trying to get lock; try restarting transaction</description>
        ///     </item>
        ///     <item>
        ///         <term>1205</term>
        ///         <description>Lock wait timeout exceeded; try restarting transaction</description>
        ///     </item>
        ///     <item>
        ///         <term>1042</term>
        ///         <description>Given when the connection is unable to successfully connect to the host.</description>
        ///     </item>
        ///     <item>
        ///         <term>1203</term>
        ///         <description>User %s already has more than 'max_user_connections' active connections</description>
        ///     </item>
        ///     <item>
        ///         <term>1040</term>
        ///         <description>Too many connections</description>
        ///     </item>
        /// </list>
        /// </summary>
        public int MaximumRetryCountForQueries { get; set; } = 5;

        /// <summary>
        /// The current environment.
        /// </summary>
        public Environments Environment { get; set; } = Environments.Live;

        /// <summary>
        /// The encryption key key used for AES encryption.
        /// </summary>
        public string DefaultEncryptionKey { get; set; }

        /// <summary>
        /// The encryption key key used for triple DES encryption.
        /// </summary>
        public string DefaultEncryptionKeyTripleDes { get; set; }

        /// <summary>
        /// Base URL of the PostNL api
        /// </summary>
        public string PostNlShippingApiKey { get; set; }

        /// <summary>
        /// Api key to use for the PostNL api
        /// </summary>
        public string PostNlApiBaseUrl { get; set; }

        /// <summary>
        /// The encryption key the ShoppingBasketsService uses for AES encryption.
        /// </summary>
        public string ShoppingBasketEncryptionKey { get; set; }

        /// <summary>
        /// The encryption key used to encrypt and decrypt the cookie value of the Account component.
        /// </summary>
        public string AccountCookieValueEncryptionKey { get; set; }

        /// <summary>
        /// The encryption key used to encrypt and decrypt the user ID of the Account component.
        /// </summary>
        public string AccountUserIdEncryptionKey { get; set; }

        /// <summary>
        /// The amount of hours an encrypted value is valid when it was encrypted with a date and time.
        /// </summary>
        public string ExpiringEncryptionKey { get; set; }

        /// <summary>
        /// The amount of hours an encrypted value is valid when it was encrypted with a date and time.
        /// </summary>
        public int TemporaryEncryptionHoursValid { get; set; } = 24;

        /// <summary>
        /// Whether or not this site uses URL segments to indicate the selected language. For example: https://example.com/fr/.
        /// </summary>
        public bool MultiLanguageBasedOnUrlSegments { get; set; }

        /// <summary>
        /// Gets or sets how long images are cached in hours.
        /// </summary>
        public TimeSpan DefaultItemFileCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache templates.
        /// </summary>
        public TimeSpan DefaultTemplateCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache objects.
        /// </summary>
        public TimeSpan DefaultObjectsCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache languages.
        /// </summary>
        public TimeSpan DefaultLanguagesCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache queries.
        /// </summary>
        public TimeSpan DefaultQueryCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache objects.
        /// </summary>
        public TimeSpan DefaultSeoModuleCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache objects.
        /// </summary>
        public TimeSpan DefaultRedirectModuleCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache objects.
        /// </summary>
        public TimeSpan DefaultWebPageCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache results from functions of <see cref="IWiserItemsService"/>.
        /// </summary>
        public TimeSpan DefaultWiserItemsCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache Configurators.
        /// </summary>
        public TimeSpan DefaultConfiguratorsCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache various aspects of the ShoppingBasket.
        /// </summary>
        public TimeSpan DefaultShoppingBasketsCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache data selector responses for data selector parsers.
        /// </summary>
        public TimeSpan DefaultDataSelectorParsersCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The SMTP settings for sending emails.
        /// </summary>
        public SmtpSettings SmtpSettings { get; set; }

        /// <summary>
        /// The license key for the library EvoPdf.
        /// </summary>
        public string EvoPdfLicenseKey { get; set; }

        /// <summary>
        /// By default the GCL adds XSRF protection in the form of anti forgery tokens. To disable this functionality, set this option to <see langword="true"/>.
        /// </summary>
        public bool DisableXsrfProtection { get; set; }

        /// <summary>
        /// In Wiser 3 we created a new templates module from scratch, which will be used by default.
        /// If you have a project that still needs to run on the old wiser 1 module (that uses the tables easy_templates and easy_dynamiccontent), set this to <see langword="true"/>.
        /// </summary>
        public bool UseLegacyWiser1TemplateModule { get; set; }

        /// <summary>
        /// A list of domain names that are considered to be test domains. E.g.: my-test-domain.com
        /// </summary>
        public string[] TestDomains { get; set; } = Array.Empty<string>();
    }
}