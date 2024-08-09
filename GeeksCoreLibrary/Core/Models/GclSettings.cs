using System;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;
using Microsoft.AspNetCore.Http;

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
        /// If the database connection requires an SSH tunnel, you can set the SSH settings here.
        /// </summary>
        public SshSettings DatabaseSshSettings { get; set; }

        /// <summary>
        /// The default connection string for the current website. This will be used for queries that write data.
        /// </summary>
        public string ConnectionStringForWriting { get; set; }

        /// <summary>
        /// If the database connection requires an SSH tunnel, you can set the SSH settings here. This will be used for queries that write data.
        /// </summary>
        public SshSettings DatabaseSshSettingsForWriting { get; set; }

        /// <summary>
        /// This will be used to set the correct timezone for the database before executing any query, so that all times will be shown in the requested timezone.
        /// </summary>
        public string DatabaseTimeZone { get; set; } = "Europe/Amsterdam";

        /// <summary>
        /// This will be used to set the correct character set for the database before executing any query. The MySqlConnector package normally
        /// forces the character set to utf8mb4, but that might not be the correct character set for your database. This setting allows you to
        /// set the character set to the correct value.
        /// </summary>
        public string DatabaseCharacterSet { get; set; } = "utf8mb4";

        /// <summary>
        /// This will be used to set the correct collation for the database before executing any query. The MySqlConnector package normally
        /// forces the collation to the database standard, which is usually utf8mb4_0900_ai_ci for MySQL 8.x installations, but that might not be the
        /// correct collation for your database. This setting allows you to set the collation to the correct value.
        /// </summary>
        public string DatabaseCollation { get; set; } = "utf8mb4_general_ci";

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
        ///     <item>
        ///         <term>1412</term>
        ///         <description>Table definition changed.</description>
        ///     </item>
        /// </list>
        /// </summary>
        public int MaximumRetryCountForQueries { get; set; } = 5;

        /// <summary>
        /// Gets or sets the amount of time to wait before retrying a query when a deadlock or similar exception occurs.
        /// </summary>
        public int TimeToWaitBeforeRetryingQueryInMilliseconds { get; set; } = 200;

        /// <summary>
        /// The current environment.
        /// </summary>
        public Environments Environment { get; set; } = Environments.Live;

        /// <summary>
        /// The encryption key key used for AES encryption.
        /// </summary>
        public string DefaultEncryptionKey { get; set; }

        /// <summary>
        /// The salt string that will be used in the AES encryption. This value should represent at least 8 bytes when converted into bytes using UTF-8.
        /// Note that the functions EncryptWithAesWithSalt and DecryptWithAesWithSalt do NOT use this salt! Those functions use a random salt. Only the functions
        /// EncryptWithAes and DecryptWithAes use this salt.
        /// </summary>
        public string DefaultEncryptionSalt { get; set; }

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
        /// The encryption key that will be used for encrypting values with an expiry date.
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
        /// The index of the language part in the URL. For example: https://example.com/content/fr/. The index of the "fr" part is 1.
        /// </summary>
        public int IndexOfLanguagePartInUrl { get; set; }

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
        /// The amount of time to cache various aspects of the ShoppingBasket.
        /// </summary>
        public TimeSpan DefaultShoppingBasketsCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache data selector responses for data selector parsers.
        /// </summary>
        public TimeSpan DefaultDataSelectorParsersCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The amount of time to cache objects.
        /// </summary>
        public TimeSpan DefaultOrderProcessCacheDuration { get; set; } = new(1, 0, 0);

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
        /// Specifies whether to suppress the generation of X-Frame-Options header which is used to prevent ClickJacking.
        /// By default, the X-Frame-Options header is generated with the value SAMEORIGIN. If this setting is 'true', the X-Frame-Options header will not be generated for the response.
        /// </summary>
        public bool SuppressXFrameOptionHeader { get; set; }

        /// <summary>
        /// In Wiser 3 we created a new templates module from scratch, which will be used by default.
        /// If you have a project that still needs to run on the old wiser 1 module (that uses the tables easy_templates and easy_dynamiccontent), set this to <see langword="true"/>.
        /// </summary>
        public bool UseLegacyWiser1TemplateModule { get; set; }

        /// <summary>
        /// A list of domain names that are considered to be test domains. E.g.: my-test-domain.com
        /// </summary>
        public string[] TestDomains { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Whether to log whenever a database connection gets opened and closed.
        /// These logs will be saved in the table "gcl_database_connection_log".
        /// That table will be automatically created if it doesn't exist yet.
        /// </summary>
        public bool LogOpeningAndClosingOfConnections { get; set; }

        /// <summary>
        /// The SameSite mode to use for cookies.
        /// </summary>
        public SameSiteMode CookieSameSiteMode { get; set; } = SameSiteMode.Lax;

        /// <summary>
        /// Settings for request logging.
        /// </summary>
        public RequestLoggingOptions RequestLoggingOptions { get; set; } = new();
    }
}