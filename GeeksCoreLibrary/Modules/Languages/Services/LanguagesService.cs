using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Models;

namespace GeeksCoreLibrary.Modules.Languages.Services
{
    /// <inheritdoc cref="ILanguagesService" />
    public class LanguagesService : ILanguagesService, IScopedService
    {
        private readonly ILogger<LanguagesService> logger;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GclSettings gclSettings;

        public string CurrentLanguageCode { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="LanguagesService"/>.
        /// </summary>
        public LanguagesService(ILogger<LanguagesService> logger, IDatabaseConnection databaseConnection, IObjectsService objectsService, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
            this.httpContextAccessor = httpContextAccessor;
            this.gclSettings = gclSettings.Value;
        }
        
        /// <inheritdoc />
        public async Task<string> GetTranslationAsync(string original, string languageCode = null, string defaultValue = null)
        {
            try
            {
                languageCode ??= await GetLanguageCodeAsync();
                
                databaseConnection.AddParameter("languageCode", languageCode);
                databaseConnection.AddParameter("original", original);
                databaseConnection.AddParameter("groupName", Constants.TranslationsGroupName);
                databaseConnection.AddParameter("translationsItemId", await objectsService.FindSystemObjectByDomainNameAsync("W2LANGUAGES_TranslationsItemId"));
                var dataTable = await databaseConnection.GetAsync(
                    @$"SELECT
	                    `key`,
	                    CONCAT_WS('', `value`, long_value) AS `value`
                    FROM {WiserTableNames.WiserItemDetail}
                    WHERE item_id = ?translationsItemId
                        AND groupname = ?groupName
                        AND language_code = ?languageCode
                        AND (`value` = ?original OR long_value = ?original)");

                var result = dataTable.Rows.Count > 0 ? dataTable.Rows[0].Field<string>("value") ?? "" : original;

                if (String.IsNullOrEmpty(result))
                {
                    result = defaultValue ?? original;
                }

                return result;
            }
            catch (Exception exception)
            {
                logger.LogError($"Error loading translations. Errormessage: {exception}");
            }

            return original;
        }

        /// <inheritdoc />
        public async Task<string> GetLanguageCodeAsync()
        {
            // First check for a system object.
            var languageCode = await objectsService.FindSystemObjectByDomainNameAsync("W2LANGUAGES_LanguageCode");
            if (!String.IsNullOrWhiteSpace(languageCode))
            {
                CurrentLanguageCode = languageCode;
                logger.LogDebug($"LanguageCode determined through system object: {languageCode}");
                return languageCode;
            }

            if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.Request.Headers.ContainsKey(Constants.LanguageCodeHeaderKey) && !String.IsNullOrWhiteSpace(httpContextAccessor.HttpContext.Request.Headers[Constants.LanguageCodeHeaderKey]))
            {
                CurrentLanguageCode = httpContextAccessor.HttpContext.Request.Headers[Constants.LanguageCodeHeaderKey];
                logger.LogDebug($"LanguageCode determined through HTTP header: {languageCode}");
                return CurrentLanguageCode;
            }

            languageCode = await GetDefaultWiserLanguage();
            CurrentLanguageCode = languageCode;
            logger.LogDebug($"LanguageCode determined by Wiser 2 default language: {languageCode}");
            return languageCode;
        }

        /// <summary>
        /// Gets the first language code based on the ordering in the module 'Stamgegevens'.
        /// </summary>
        /// <returns>The language code of the first language found or an empty string if no languages are set.</returns>
        private async Task<string> GetDefaultWiserLanguage()
        {
            logger.LogDebug("Start GetDefaultWiserLanguage");

            var result = String.Empty;
            var userLanguages = httpContextAccessor.HttpContext?.Request.GetTypedHeaders().AcceptLanguage.OrderByDescending(v => v.Quality ?? 1).Select(v => v.Value.Value).ToList();

            var getDefaultLanguageResult = await databaseConnection.GetAsync(
                $@"SELECT c.`value`, IFNULL(d.`value`, '0') AS is_default_language
                FROM {WiserTableNames.WiserItem} AS lang
                JOIN {WiserTableNames.WiserItemDetail} AS c ON c.item_id = lang.id AND c.`key` = 'language_code' AND c.`value` IS NOT NULL AND c.`value` <> ''
                LEFT JOIN {WiserTableNames.WiserItemDetail} AS d ON d.item_id = lang.id AND d.`key` = 'is_default_language'
                LEFT JOIN {WiserTableNames.WiserItemLink} AS link ON link.item_id = lang.id AND link.type = 1
                WHERE lang.entity_type = 'language'
                AND lang.published_environment > 0
                ORDER BY IFNULL(link.ordering, lang.title)");

            if (getDefaultLanguageResult.Rows.Count == 0)
            {
                return String.Empty;
            }

            var defaultLanguage = getDefaultLanguageResult.Rows[0].Field<string>("value");
            var databaseLanguages = new List<string>();

            foreach (DataRow dataRow in getDefaultLanguageResult.Rows)
            {
                var currentLanguageCode = dataRow.Field<string>("value");
                if (dataRow.Field<string>("is_default_language") == "1")
                {
                    defaultLanguage = currentLanguageCode;
                }

                databaseLanguages.Add(currentLanguageCode);
            }

            if (userLanguages == null)
            {
                logger.LogDebug("No user languages, using default language: {defaultLanguage}", defaultLanguage);
                return defaultLanguage;
            }

            // See if there's a user language that can be used here.
            foreach (var userLanguage in userLanguages)
            {
                result = databaseLanguages.FirstOrDefault(l => l.Trim().Equals(userLanguage, StringComparison.OrdinalIgnoreCase));
                if (!String.IsNullOrWhiteSpace(result))
                {
                    break;
                }

                result = databaseLanguages.FirstOrDefault(l => l.Trim().Equals(userLanguage.Split('-').First(), StringComparison.OrdinalIgnoreCase));
            }

            if (!String.IsNullOrWhiteSpace(result))
            {
                logger.LogDebug("Using user language: {result}", result);
                return result;
            }

            logger.LogDebug("Using default language: {defaultLanguage}", defaultLanguage);
            return defaultLanguage;
        }
    }
}
