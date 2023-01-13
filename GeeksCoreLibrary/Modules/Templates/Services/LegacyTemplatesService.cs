using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Filter.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Extensions;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Template = GeeksCoreLibrary.Modules.Templates.Models.Template;

namespace GeeksCoreLibrary.Modules.Templates.Services
{
    /// <summary>
    /// This class provides template caching, template replacements and rendering
    /// for all types of templates, like CSS, JS, Query's and HTML templates.
    /// </summary>
    public class LegacyTemplatesService : ITemplatesService
    {
        private readonly GclSettings gclSettings;
        private readonly ILogger<LegacyTemplatesService> logger;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IViewComponentHelper viewComponentHelper;
        private readonly ITempDataProvider tempDataProvider;
        private readonly IActionContextAccessor actionContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IObjectsService objectsService;
        private readonly ILanguagesService languagesService;
        private readonly IFiltersService filtersService;
        private readonly IAccountsService accountsService;

        /// <summary>
        /// Initializes a new instance of <see cref="LegacyTemplatesService"/>.
        /// </summary>
        public LegacyTemplatesService(ILogger<LegacyTemplatesService> logger,
            IOptions<GclSettings> gclSettings,
            IDatabaseConnection databaseConnection,
            IStringReplacementsService stringReplacementsService,
            IHttpContextAccessor httpContextAccessor,
            IViewComponentHelper viewComponentHelper,
            ITempDataProvider tempDataProvider,
            IActionContextAccessor actionContextAccessor,
            IWebHostEnvironment webHostEnvironment,
            IFiltersService filtersService,
            IObjectsService objectsService,
            ILanguagesService languagesService,
            IAccountsService accountsService)
        {
            this.gclSettings = gclSettings.Value;
            this.logger = logger;
            this.databaseConnection = databaseConnection;
            this.stringReplacementsService = stringReplacementsService;
            this.httpContextAccessor = httpContextAccessor;
            this.viewComponentHelper = viewComponentHelper;
            this.tempDataProvider = tempDataProvider;
            this.actionContextAccessor = actionContextAccessor;
            this.webHostEnvironment = webHostEnvironment;
            this.filtersService = filtersService;
            this.objectsService = objectsService;
            this.languagesService = languagesService;
            this.accountsService = accountsService;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "", bool includeContent = true)
        {
            if (id <= 0 && String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
            }

            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new ArgumentOutOfRangeException(nameof(gclSettings.Environment), gclSettings.Environment.ToString())
            };

            var whereClause = new List<string>();

            var useTypeFilter = false;

            if (id > 0)
            {
                databaseConnection.AddParameter("id", id);
                whereClause.Add("i.id = ?id");
            }
            else
            {
                databaseConnection.AddParameter("name", name);
                whereClause.Add("i.name = ?name");
                useTypeFilter = type.HasValue;
            }

            if (parentId > 0)
            {
                databaseConnection.AddParameter("parentId", parentId);
                whereClause.Add(" AND ip.id = ?parentId");
            }
            else if (!String.IsNullOrWhiteSpace(parentName))
            {
                databaseConnection.AddParameter("parentName", parentName);
                whereClause.Add(" AND ip.name = ?parentName");
            }

            if (useTypeFilter && type.Value != TemplateTypes.Unknown)
            {
                switch (type.Value)
                {
                    case TemplateTypes.Query:
                        // Query templates don't have a type.
                        whereClause.Add("(t.templatetype IS NULL OR t.templatetype = '')");
                        whereClause.Add("COALESCE(ippppp.`name`, ipppp.`name`, ippp.`name`, ipp.`name`, ip.`name`) = 'QUERY'");
                        break;
                    case TemplateTypes.Routine:
                        databaseConnection.AddParameter("templateType1", "FUNCTION");
                        databaseConnection.AddParameter("templateType2", "PROCEDURE");
                        whereClause.Add("t.templatetype IN (?templateType1, ?templateType2)");
                        break;
                    default:
                        databaseConnection.AddParameter("templateType", type.Value.ToString("G").ToLowerInvariant());
                        whereClause.Add("t.templatetype = ?templateType");
                        break;
                }
            }

            var query = $@"SELECT
                            COALESCE(ippppp.name, ipppp.name, ippp.name, ipp.name, ip.name) AS root_name, 
                            ip.`name` AS parent_name, 
                            ip.id AS parent_id,
                            i.`name` AS template_name,
                            t.templatetype AS template_type,
                            i.volgnr AS ordering,
                            ip.volgnr AS parent_ordering,
                            i.id AS template_id,
                            t.csstemplates AS css_templates,
                            t.jstemplates AS javascript_templates,
                            t.loadalways AS load_always,
                            t.lastchanged AS changed_on,
                            t.externalfiles AS external_files,
                            {(includeContent ? "t.html_obfuscated, t.html_minified AS template_data_minified, t.html AS template_data, t.template," : "")}
                            t.urlregex AS url_regex,
                            t.usecache AS use_cache,
                            t.cacheminutes AS cache_minutes,
                            1 AS cache_location,
                            t.cacheregex AS cache_regex,
                            t.useobfuscate AS use_obfuscate,
                            t.defaulttemplate AS wiser_cdn_files,
                            t.pagemode AS insert_mode,
                            t.groupingCreateObjectInsteadOfArray AS grouping_create_object_instead_of_array,
                            t.groupingKeyColumnName AS grouping_key_column_name,
                            t.groupingValueColumnName AS grouping_value_column_name,
                            t.groupingkey AS grouping_key,
                            t.groupingprefix AS grouping_prefix,
                            t.issecure AS login_required,
                            t.version
                        FROM easy_items i 
                        JOIN easy_templates t ON t.itemid = i.id
                        {joinPart}
                        LEFT JOIN easy_items ip ON ip.id = i.parent_id
                        LEFT JOIN easy_items ipp ON ipp.id = ip.parent_id
                        LEFT JOIN easy_items ippp ON ippp.id = ipp.parent_id
                        LEFT JOIN easy_items ipppp ON ipppp.id = ippp.parent_id
                        LEFT JOIN easy_items ippppp ON ippppp.id = ipppp.parent_id
                        WHERE i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND {String.Join(" AND ", whereClause)}
                        ORDER BY ippppp.volgnr, ipppp.volgnr, ippp.volgnr, ipp.volgnr, ip.volgnr, i.volgnr";

            Template result;
            var reader = await databaseConnection.GetReaderAsync(query);
            try
            {
                result = await reader.ReadAsync() ? await reader.ToTemplateModelAsync(type) : new Template();
            }
            finally
            {
                await reader.CloseAsync();
                await reader.DisposeAsync();
            }

            // Check login requirement.
            if (!result.Type.InList(TemplateTypes.Html, TemplateTypes.Query) || !result.LoginRequired)
            {
                // No login required; return template.
                return result;
            }

            var emptyTemplate = new Template
            {
                Type = result.Type,
                LoginRequired = true,
                LoginRedirectUrl = await objectsService.FindSystemObjectByDomainNameAsync("defaultloginurl", "/")
            };

            if (httpContextAccessor.HttpContext == null)
            {
                // No context available; return empty template without doing a login check.
                return emptyTemplate;
            }

            // Check current login.
            var userData = await accountsService.GetUserDataFromCookieAsync();
            return userData is { UserId: > 0 } ? result : emptyTemplate;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateCacheSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            if (id <= 0 && String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
            }

            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new ArgumentOutOfRangeException(nameof(gclSettings.Environment), gclSettings.Environment.ToString())
            };

            string whereClause;
            if (id > 0)
            {
                databaseConnection.AddParameter("id", id);
                whereClause = "i.id = ?id";
            }
            else
            {
                databaseConnection.AddParameter("name", name);
                whereClause = "i.name = ?name";
            }

            if (parentId > 0)
            {
                databaseConnection.AddParameter("parentId", parentId);
                whereClause += " AND ip.id = ?parentId";
            }
            else if (!String.IsNullOrWhiteSpace(parentName))
            {
                databaseConnection.AddParameter("parentName", parentName);
                whereClause = " AND ip.name = ?parentName";
            }

            var query = $@"SELECT
                            i.`name` AS template_name,
                            i.id AS template_id,
                            t.usecache AS use_cache,
                            t.cacheminutes AS cache_minutes,
                            t.cacheregex AS cache_regex,
                            CASE t.templatetype
                                WHEN 'html' THEN 1
                                WHEN 'css' THEN 2
                                WHEN 'scss' THEN 3
                                WHEN 'js' THEN 4
                                ELSE 0
                            END AS template_type
                        FROM easy_items i 
                        JOIN easy_templates t ON i.id=t.itemid
                        {joinPart}
                        WHERE i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND {whereClause}
                        LIMIT 1";

            var dataTable = await databaseConnection.GetAsync(query);
            var result = dataTable.Rows.Count == 0 ? new Template() : new Template
            {
                Id = dataTable.Rows[0].Field<int>("template_id"),
                Name = dataTable.Rows[0].Field<string>("template_name"),
                CachingMinutes = dataTable.Rows[0].Field<int>("cache_minutes"),
                CachingMode = dataTable.Rows[0].Field<TemplateCachingModes>("use_cache"),
                CachingLocation = TemplateCachingLocations.OnDisk,
                CachingRegex = dataTable.Rows[0].Field<string>("cache_regex"),
                Type = (TemplateTypes)Convert.ToInt32(dataTable.Rows[0]["template_type"])
            };

            return result;
        }

        /// <inheritdoc />
        public Task<int> GetTemplateIdFromNameAsync(string name, TemplateTypes type)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetGeneralTemplateLastChangedDateAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard)
        {
            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new ArgumentOutOfRangeException(nameof(gclSettings.Environment), gclSettings.Environment.ToString())
            };

            var query = $@"SELECT MAX(t.lastchanged) AS lastChanged
                        FROM easy_items i 
                        JOIN easy_templates t ON i.id = t.itemid
                        {joinPart}
                        WHERE i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND t.loadalways > 0
                        AND t.templatetype = ?templateType";

            databaseConnection.AddParameter("templateType", templateType.ToString());
            DateTime? result;
            var reader = await databaseConnection.GetReaderAsync(query);
            try
            {
                if (!await reader.ReadAsync())
                {
                    return null;
                }

                var ordinal = reader.GetOrdinal("lastChanged");
                result = await reader.IsDBNullAsync(ordinal) ? null : reader.GetDateTime(ordinal);
                return result;
            }
            finally
            {
                await reader.CloseAsync();
                await reader.DisposeAsync();
            }
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetGeneralTemplateValueAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard)
        {
            var templateTypeQueryPart = templateType is TemplateTypes.Css or TemplateTypes.Scss
                ? $"t.templatetype IN ('{TemplateTypes.Css.ToString().ToMySqlSafeValue(false)}', '{TemplateTypes.Scss.ToString().ToMySqlSafeValue(false)}')"
                : $"t.templatetype = '{templateType.ToString().ToMySqlSafeValue(false)}'";

            var pageModeQueryPart = $"t.pagemode = {(int)byInsertMode}";

            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new ArgumentOutOfRangeException(nameof(gclSettings.Environment), gclSettings.Environment.ToString())
            };

            var query = $@"SELECT
                            IFNULL(ippppp.name, IFNULL(ipppp.name, IFNULL(ippp.name, IFNULL(ipp.name, ip.name)))) as root_name, 
                            ip.`name` AS parent_name, 
                            ip.id AS parent_id,
                            i.`name` AS template_name,
                            t.templatetype AS template_type,
                            i.volgnr AS ordering,
                            ip.volgnr AS parent_ordering,
                            i.id AS template_id,
                            t.csstemplates AS css_templates,
                            t.jstemplates AS javascript_templates,
                            t.loadalways AS load_always,
                            t.lastchanged AS changed_on,
                            t.externalfiles AS external_files,
                            t.html_obfuscated,
                            t.html_minified AS template_data_minified,
                            t.html AS template_data,
                            t.template,
                            t.urlregex AS url_regex,
                            t.usecache AS use_cache,
                            t.cacheminutes AS cache_minutes,
                            1 AS cache_location,
                            t.cacheregex AS cache_regex,
                            t.useobfuscate AS use_obfuscate,
                            t.defaulttemplate AS wiser_cdn_files,
                            t.pagemode AS insert_mode,
                            t.groupingCreateObjectInsteadOfArray AS grouping_create_object_instead_of_array,
                            t.groupingKeyColumnName AS grouping_key_column_name,
                            t.groupingValueColumnName AS grouping_value_column_name,
                            t.groupingkey AS grouping_key,
                            t.groupingprefix AS grouping_prefix,
                            t.version
                        FROM easy_items i 
                        JOIN easy_templates t ON i.id = t.itemid
                        {joinPart}
                        LEFT JOIN easy_items ip ON i.parent_id = ip.id
                        LEFT JOIN easy_items ipp ON ip.parent_id = ipp.id
                        LEFT JOIN easy_items ippp ON ipp.parent_id = ippp.id
                        LEFT JOIN easy_items ipppp ON ippp.parent_id = ipppp.id
                        LEFT JOIN easy_items ippppp ON ipppp.parent_id = ippppp.id
                        WHERE i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND t.loadalways > 0
                        AND {templateTypeQueryPart}
                        AND {pageModeQueryPart}
                        ORDER BY ippppp.volgnr, ipppp.volgnr, ippp.volgnr, ipp.volgnr, ip.volgnr, i.volgnr";

            var result = new TemplateResponse();
            var resultBuilder = new StringBuilder();
            var idsLoaded = new List<int>();
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).ToString();

            var reader = await databaseConnection.GetReaderAsync(query);
            try
            {
                while (await reader.ReadAsync())
                {
                    var template = await reader.ToTemplateModelAsync();
                    await AddTemplateToResponseAsync(idsLoaded, template, currentUrl, resultBuilder, result);
                }
            }
            finally
            {
                await reader.CloseAsync();
                await reader.DisposeAsync();
            }

            result.Content = resultBuilder.ToString();

            if (result.LastChangeDate == DateTime.MinValue)
            {
                result.LastChangeDate = DateTime.Now;
            }

            if (templateType is TemplateTypes.Css or TemplateTypes.Scss)
            {
                result.Content = CssHelpers.MoveImportStatementsToTop(result.Content);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<Template>> GetTemplatesAsync(ICollection<int> templateIds, bool includeContent)
        {
            var results = new List<Template>();
            databaseConnection.AddParameter("includeContent", includeContent);

            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new ArgumentOutOfRangeException(nameof(gclSettings.Environment), gclSettings.Environment.ToString())
            };

            var query = $@"SELECT
                            IFNULL(ippppp.name, IFNULL(ipppp.name, IFNULL(ippp.name, IFNULL(ipp.name, ip.name)))) as root_name, 
                            ip.`name` AS parent_name, 
                            ip.id AS parent_id,
                            i.`name` AS template_name,
                            t.templatetype AS template_type,
                            i.volgnr AS ordering,
                            ip.volgnr AS parent_ordering,
                            i.id AS template_id,
                            t.csstemplates AS css_templates,
                            t.jstemplates AS javascript_templates,
                            t.loadalways AS load_always,
                            t.lastchanged AS changed_on,
                            t.externalfiles AS external_files,
                            IF(?includeContent, t.html_obfuscated, '') AS html_obfuscated,
                            IF(?includeContent, t.html_minified, '') AS template_data_minified,
                            IF(?includeContent, t.html, '') AS template_data,
                            IF(?includeContent, t.template, '') AS template,
                            t.urlregex AS url_regex,
                            t.usecache AS use_cache,
                            t.cacheminutes AS cache_minutes,
                            1 AS cache_location,
                            t.cacheregex AS cache_regex,
                            t.useobfuscate AS use_obfuscate,
                            t.defaulttemplate AS wiser_cdn_files,
                            t.pagemode AS insert_mode,
                            t.groupingCreateObjectInsteadOfArray AS grouping_create_object_instead_of_array,
                            t.groupingKeyColumnName AS grouping_key_column_name,
                            t.groupingValueColumnName AS grouping_value_column_name,
                            t.groupingkey AS grouping_key,
                            t.groupingprefix AS grouping_prefix,
                            t.version
                        FROM easy_items i 
                        JOIN easy_templates t ON i.id = t.itemid
                        {joinPart}
                        LEFT JOIN easy_items ip ON i.parent_id = ip.id
                        LEFT JOIN easy_items ipp ON ip.parent_id = ipp.id
                        LEFT JOIN easy_items ippp ON ipp.parent_id = ippp.id
                        LEFT JOIN easy_items ipppp ON ippp.parent_id = ipppp.id
                        LEFT JOIN easy_items ippppp ON ipppp.parent_id = ippppp.id
                        WHERE i.id IN ({String.Join(",", templateIds)})
                        AND i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND t.loadalways > 0
                        ORDER BY ippppp.volgnr, ipppp.volgnr, ippp.volgnr, ipp.volgnr, ip.volgnr, i.volgnr";

            var reader = await databaseConnection.GetReaderAsync(query);
            try
            {
                while (await reader.ReadAsync())
                {
                    var template = await reader.ToTemplateModelAsync();
                    results.Add(template);
                }
            }
            finally
            {
                await reader.CloseAsync();
                await reader.DisposeAsync();
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetCombinedTemplateValueAsync(ICollection<int> templateIds, TemplateTypes templateType)
        {
            return await GetCombinedTemplateValueAsync(this, templateIds, templateType);
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetCombinedTemplateValueAsync(ITemplatesService templatesService, ICollection<int> templateIds, TemplateTypes templateType)
        {
            var result = new TemplateResponse();
            var resultBuilder = new StringBuilder();
            var idsLoaded = new List<int>();
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).ToString();
            var templates = await templatesService.GetTemplatesAsync(templateIds, true);

            foreach (var template in templates.Where(t => t.Type == templateType))
            {
                await templatesService.AddTemplateToResponseAsync(idsLoaded, template, currentUrl, resultBuilder, result);
            }

            result.Content = resultBuilder.ToString();

            if (result.LastChangeDate == DateTime.MinValue)
            {
                result.LastChangeDate = DateTime.Now;
            }

            if (templateType == TemplateTypes.Css)
            {
                result.Content = CssHelpers.MoveImportStatementsToTop(result.Content);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task AddTemplateToResponseAsync(ICollection<int> idsLoaded, Template template, string currentUrl, StringBuilder resultBuilder, TemplateResponse templateResponse)
        {
            if (idsLoaded.Contains(template.Id))
            {
                // Make sure that we don't add the same template twice.
                return;
            }

            if (!String.IsNullOrWhiteSpace(template.UrlRegex) && !Regex.IsMatch(currentUrl, template.UrlRegex))
            {
                // Skip this template if it has an URL regex and that regex does not match the current URL.
                return;
            }

            idsLoaded.Add(template.Id);

            // Get files from Wiser CDN.
            if (template.WiserCdnFiles.Any())
            {
                resultBuilder.AppendLine(await GetWiserCdnFilesAsync(template.WiserCdnFiles));
            }

            // Get the template contents.
            resultBuilder.AppendLine(template.Content);

            // Get the change date.
            if (template.LastChanged > templateResponse.LastChangeDate)
            {
                templateResponse.LastChangeDate = template.LastChanged;
            }

            // Get any external files that we need to load.
            templateResponse.ExternalFiles.AddRange(template.ExternalFiles);
        }

        /// <inheritdoc />
        public async Task<string> GetWiserCdnFilesAsync(ICollection<string> fileNames)
        {
            if (fileNames == null)
            {
                throw new ArgumentNullException(nameof(fileNames));
            }

            var enumerable = fileNames.ToList();
            if (!enumerable.Any())
            {
                return "";
            }

            var resultBuilder = new StringBuilder();
            using var webClient = new WebClient();
            foreach (var fileName in enumerable.Where(fileName => !String.IsNullOrWhiteSpace(fileName)))
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var directory = extension switch
                {
                    ".js" => "scripts",
                    _ => extension.Substring(1)
                };

                var localDirectory = Path.Combine(webHostEnvironment.WebRootPath, directory);
                if (!Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                var fileLocation = Path.Combine(localDirectory, fileName);
                if (!File.Exists(fileLocation))
                {
                    await webClient.DownloadFileTaskAsync(new Uri($"https://app.wiser.nl/{directory}/cdn/{fileName}"), fileLocation);
                }

                resultBuilder.AppendLine(await File.ReadAllTextAsync(fileLocation));
            }

            return resultBuilder.ToString();
        }

        /// <inheritdoc />
        public async Task<string> DoReplacesAsync(string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false, TemplateTypes? templateType = null)
        {
            return await DoReplacesAsync(this, input, handleStringReplacements, handleDynamicContent, evaluateLogicSnippets, dataRow, handleRequest, removeUnknownVariables, forQuery, templateType);
        }

        /// <inheritdoc />
        public async Task<string> DoReplacesAsync(ITemplatesService templatesService, string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false, TemplateTypes? templateType = null)
        {
            // Input cannot be empty.
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            // Start with normal string replacements, because includes can contain variables in a query string, which need to be replaced first.
            if (handleStringReplacements)
            {
                input = await stringReplacementsService.DoAllReplacementsAsync(input, dataRow, handleRequest, false, removeUnknownVariables, forQuery);
            }

            // HTML and mail templates.
            // Note: The string replacements service cannot handle the replacing of templates, because that would cause the StringReplacementsService to need
            // the TemplatesService, which in turn needs the StringReplacementsService, creating a circular dependency.
            input = await templatesService.HandleIncludesAsync(input, forQuery: forQuery, templateType: templateType);
            input = await templatesService.HandleImageTemplating(input);

            // Replace dynamic content.
            if (handleDynamicContent && !forQuery)
            {
                input = await templatesService.ReplaceAllDynamicContentAsync(input);
            }

            if (evaluateLogicSnippets)
            {
                input = stringReplacementsService.EvaluateTemplate(input);
            }

            return input;
        }

        /// <inheritdoc />
        public async Task<string> GenerateImageUrl(string itemId, string type, int number, string filename = "", string width = "0", string height = "0", string resizeMode = "")
        {
            var imageUrlTemplate = await objectsService.FindSystemObjectByDomainNameAsync("image_url_template", "/image/wiser2/<item_id>/<type>/<resizemode>/<width>/<height>/<number>/<filename>");

            imageUrlTemplate = imageUrlTemplate.Replace("<item_id>", itemId);
            imageUrlTemplate = imageUrlTemplate.Replace("<filename>", filename);
            imageUrlTemplate = imageUrlTemplate.Replace("<type>", type);
            imageUrlTemplate = imageUrlTemplate.Replace("<width>", width);
            imageUrlTemplate = imageUrlTemplate.Replace("<height>", height);

            // Remove if not specified
            if (number == 0)
            {
                imageUrlTemplate = imageUrlTemplate.Replace("<number>/", "");
            }

            // Remove if not specified
            if (string.IsNullOrWhiteSpace(resizeMode))
            {
                imageUrlTemplate = imageUrlTemplate.Replace("<resizemode>/", "");
            }

            imageUrlTemplate = imageUrlTemplate.Replace("<number>", number.ToString());
            imageUrlTemplate = imageUrlTemplate.Replace("<resizemode>", resizeMode);

            return imageUrlTemplate;
        }

        /// <inheritdoc />
        public async Task<string> HandleImageTemplating(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var imageTemplatingRegex = new Regex(@"\[image\[(.*?)\]\]");
            foreach (Match m in imageTemplatingRegex.Matches(input))
            {
                var replacementParameters = m.Groups[1].Value.Split(":");
                var outputBuilder = new StringBuilder();
                var imageIndex = 0;
                var resizeMode = "";
                var propertyName = "";
                var imageAltTag = "";
                var fallbackImageExtension = "jpg";
                var parameters = replacementParameters[0].Split(",");
                var imageItemIdOrFilename = parameters[0];
                var output = "";

                // Only get the parameter if specified in the templating variable
                if (parameters.Length > 1)
                {
                    propertyName = parameters[1].Trim();
                }

                if (parameters.Length > 2)
                {
                    fallbackImageExtension = parameters[2].Trim();
                }

                if (parameters.Length > 3)
                {
                    imageIndex = Int32.Parse(parameters[3].Trim());
                }

                if (parameters.Length > 4)
                {
                    resizeMode = parameters[4].Trim();
                }

                if (parameters.Length > 5)
                {
                    imageAltTag = parameters[5].Trim();
                }

                imageIndex = imageIndex == 0 ? 1 : imageIndex;

                // Get the image from the database
                databaseConnection.AddParameter("itemId", imageItemIdOrFilename);
                databaseConnection.AddParameter("filename", imageItemIdOrFilename);
                databaseConnection.AddParameter("propertyName", propertyName);

                var queryWherePart = Int64.TryParse(imageItemIdOrFilename, out _) ? "item_id = ?itemId" : "file_name = ?filename";
                var dataTable = await databaseConnection.GetAsync(@$"SELECT * FROM `{WiserTableNames.WiserItemFile}` WHERE {queryWherePart} AND IF(?propertyName = '', 1=1, property_name = ?propertyName) AND content_type LIKE 'image%' ORDER BY id ASC");

                if (dataTable.Rows.Count == 0)
                {
                    input = input.ReplaceCaseInsensitive(m.Value, $"<img src=\"/img/noimg.png\" />");
                    continue;
                }

                if (imageIndex > dataTable.Rows.Count)
                {
                    input = input.ReplaceCaseInsensitive(m.Value, "specified image index out of bound");
                    continue;
                }

                // Get various values from the table
                var imageItemId = Convert.ToString(dataTable.Rows[imageIndex - 1]["item_id"]);
                var imageFilename = dataTable.Rows[imageIndex - 1].Field<string>("file_name");
                var imagePropertyType = dataTable.Rows[imageIndex - 1].Field<string>("property_name");
                var imageFilenameWithoutExt = Path.GetFileNameWithoutExtension(imageFilename);
                var imageTemplatingSetsRegex = new Regex(@"\:(.*?)\)");
                var items = imageTemplatingSetsRegex.Matches(m.Groups[1].Value);
                var totalItems = items.Count;
                var index = 1;

                if (items.Count == 0)
                {
                    input = input.ReplaceCaseInsensitive(m.Value, "no image set(s) specified, you must at least specify one set");
                    continue;
                }

                foreach (Match s in items)
                {
                    var imageTemplate = await objectsService.FindSystemObjectByDomainNameAsync("image_template", "<figure><picture>{images}</picture></figure>");

                    // Get the specified parameters from the regex match
                    parameters = s.Value.Split(":")[1].Split("(");
                    var imageParameters = parameters[1].Replace(")", "").Split("x");
                    var imageViewportParameter = parameters[0];

                    if (String.IsNullOrWhiteSpace(imageViewportParameter))
                    {
                        input = input.ReplaceCaseInsensitive(m.Value, "no viewport parameter specified");
                        continue;
                    }

                    var imageWidth = Convert.ToInt32(imageParameters[0]);
                    var imageHeight = Convert.ToInt32(imageParameters[1]);
                    var imageWidth2X = (imageWidth * 2).ToString();
                    var imageHeight2X = (imageHeight * 2).ToString();

                    outputBuilder.Append(@"<source media=""(min-width: {min-width}px)"" srcset=""{image-url-webp-2x} 2x, {image-url-webp}"" type=""image/webp"" />");
                    outputBuilder.Append(@"<source media=""(min-width: {min-width}px)"" srcset=""{image-url-alt-2x} 2x, {image-url-alt}"" type=""{image-type-alt}"" />");

                    outputBuilder.Replace("{image-url-webp}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, $"{imageFilenameWithoutExt}.webp", imageWidth.ToString(), imageHeight.ToString(), resizeMode));
                    outputBuilder.Replace("{image-url-alt}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, $"{imageFilenameWithoutExt}.{fallbackImageExtension}", imageWidth.ToString(), imageHeight.ToString(), resizeMode));
                    outputBuilder.Replace("{image-url-webp-2x}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, $"{imageFilenameWithoutExt}.webp", imageWidth2X, imageHeight2X, resizeMode));
                    outputBuilder.Replace("{image-url-alt-2x}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, $"{imageFilenameWithoutExt}.{fallbackImageExtension}", imageWidth2X, imageHeight2X, resizeMode));
                    outputBuilder.Replace("{image-type-alt}", FileSystemHelpers.GetMediaTypeByExtension(fallbackImageExtension));
                    outputBuilder.Replace("{min-width}", imageViewportParameter);

                    // If last item, than add the default image
                    if (index == totalItems)
                    {
                        outputBuilder.Append("<img width=\"{image_width}\" height=\"{image_height}\" loading=\"lazy\" src=\"{default_image_link}\" alt=\"{image_alt}\">");
                        outputBuilder.Replace("{default_image_link}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, $"{imageFilenameWithoutExt}.webp", imageWidth.ToString(), imageHeight.ToString(), resizeMode));
                        outputBuilder.Replace("{image_width}", imageWidth.ToString());
                        outputBuilder.Replace("{image_height}", imageHeight.ToString());
                    }

                    imageTemplate = imageTemplate.Replace("{images}", outputBuilder.ToString());
                    imageTemplate = imageTemplate.Replace("{image_alt}", (String.IsNullOrWhiteSpace(imageAltTag) ? imageFilename : imageAltTag));

                    output = imageTemplate;

                    index += 1;
                }

                // Replace the image in the template
                input = input.ReplaceCaseInsensitive(m.Value, output);
            }

            return input;
        }

        /// <inheritdoc />
        public async Task<string> HandleIncludesAsync(string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false, TemplateTypes? templateType = null)
        {
            return await HandleIncludesAsync(this, input, handleStringReplacements, dataRow, handleRequest, forQuery, templateType);
        }

        /// <inheritdoc />
        public async Task<string> HandleIncludesAsync(ITemplatesService templatesService, string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false, TemplateTypes? templateType = null)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            const int max = 10;
            var counter = 0;

            // We use a while loop here because it's possible to to include a template that has another include, so we might have to replace them multiple times.
            while (counter < max && (input.Contains("<[", StringComparison.Ordinal) || input.Contains("[include", StringComparison.Ordinal)))
            {
                counter += 1;
                var inclusionsRegex = new Regex(@"<\[(.*?)\]>");
                foreach (Match m in inclusionsRegex.Matches(input))
                {
                    var templateName = m.Groups[1].Value;
                    if (templateName.Contains("{"))
                    {
                        // Make sure replaces for the template name are done
                        templateName = await stringReplacementsService.DoAllReplacementsAsync(templateName, dataRow, handleRequest, forQuery: forQuery);
                    }

                    // Replace templates (syntax is <[templateName]> or <[parentFolder\templateName]>
                    if (templateName.Contains("\\"))
                    {
                        // Contains a parent
                        var split = templateName.Split('\\');
                        var template = await templatesService.GetTemplateAsync(name: split[1], type: templateType, parentName: split[0]);
                        var templateContent = template.Content;
                        if (handleStringReplacements)
                        {
                            templateContent = await stringReplacementsService.DoAllReplacementsAsync(templateContent, dataRow, handleRequest, false, false, forQuery);
                        }

                        input = input.ReplaceCaseInsensitive(m.Groups[0].Value, templateContent);
                    }
                    else
                    {
                        var template = await templatesService.GetTemplateAsync(name: templateName, type: templateType);
                        var templateContent = template.Content;
                        if (handleStringReplacements)
                        {
                            templateContent = await stringReplacementsService.DoAllReplacementsAsync(templateContent, dataRow, handleRequest, false, false, forQuery);
                        }

                        input = input.ReplaceCaseInsensitive(m.Groups[0].Value, templateContent);
                    }
                }

                inclusionsRegex = new Regex(@"\[include\[([^{?\]]*)(\?)?([^{?\]]*?)\]\]");
                foreach (Match m in inclusionsRegex.Matches(input))
                {
                    var templateName = m.Groups[1].Value;
                    var queryString = m.Groups[3].Value.Replace("&amp;", "&");
                    if (templateName.Contains("{"))
                    {
                        // Make sure replaces for the template name are done
                        templateName = await stringReplacementsService.DoAllReplacementsAsync(templateName, dataRow, handleRequest, forQuery: forQuery);
                    }

                    // Replace templates (syntax is [include[templateName]] or [include[parentFolder\templateName]] or [include[templateName?x=y]]
                    if (templateName.Contains("\\"))
                    {
                        // Contains a parent
                        var split = templateName.Split('\\');
                        var template = await templatesService.GetTemplateAsync(name: split[1], type: templateType, parentName: split[0]);
                        var values = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries).Select(x => new KeyValuePair<string, string>(x.Split('=')[0], x.Split('=')[1]));
                        var content = stringReplacementsService.DoReplacements(template.Content, values, forQuery);
                        if (handleStringReplacements)
                        {
                            content = await stringReplacementsService.DoAllReplacementsAsync(content, dataRow, handleRequest, false, false, forQuery);
                        }

                        if (!String.IsNullOrWhiteSpace(queryString))
                        {
                            content = content.Replace("<img src=\"/preview_image.aspx", $"<img data=\"{queryString}\" src=\"/preview_image.aspx");
                        }

                        input = input.ReplaceCaseInsensitive(m.Groups[0].Value, content);
                    }
                    else
                    {
                        var template = await templatesService.GetTemplateAsync(name: templateName, type: templateType);
                        var values = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries).Select(x => new KeyValuePair<string, string>(x.Split('=')[0], x.Split('=')[1]));
                        var content = stringReplacementsService.DoReplacements(template.Content, values, forQuery);
                        if (handleStringReplacements)
                        {
                            content = await stringReplacementsService.DoAllReplacementsAsync(content, dataRow, handleRequest, false, false, forQuery);
                        }

                        if (!String.IsNullOrWhiteSpace(queryString))
                        {
                            content = content.Replace("<img src=\"/preview_image.aspx", $"<img data=\"{queryString}\" src=\"/preview_image.aspx");
                        }

                        input = input.ReplaceCaseInsensitive(m.Groups[0].Value, content);
                    }
                }
            }

            return input;
        }

        /// <inheritdoc />
        public async Task<DynamicContent> GetDynamicContentData(int contentId)
        {
            string query = null;
            var templateVersionPart = "";
            switch (gclSettings.Environment)
            {
                case Environments.Development:
                    // Always get the latest version on development.
                    query = @"SELECT 
                                d.filledvariables, 
                                d.freefield1,
                                d.type,
                                d.version
                            FROM easy_dynamiccontent d
                            JOIN (SELECT id, MAX(version) AS version FROM easy_dynamiccontent GROUP BY id) d2 ON d2.id = d.id AND d2.version = d.version
                            WHERE d.id = ?contentId";
                    break;
                case Environments.Test:
                    templateVersionPart = "AND t.istest = 1";
                    break;
                case Environments.Acceptance:
                    templateVersionPart = "AND t.isacceptance = 1";
                    break;
                case Environments.Live:
                    templateVersionPart = "AND t.islive = 1";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            query ??= $@"SELECT 
                            d.filledvariables, 
                            d.freefield1,
                            d.type,
                            d.version
                        FROM easy_dynamiccontent d
                        JOIN easy_templates t ON t.itemid = d.itemid AND t.version = d.version {templateVersionPart}
                        WHERE d.id = ?contentId

                        UNION

                        SELECT 
                            d.filledvariables, 
                            d.freefield1,
                            d.type,
                            d.version
                        FROM easy_dynamiccontent d
                        WHERE d.version = 1 
                        AND d.itemid = 0
                        AND d.id = ?contentId";

            databaseConnection.AddParameter("contentId", contentId);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return new DynamicContent
            {
                Id = contentId,
                Name = dataTable.Rows[0].Field<string>("freefield1"),
                SettingsJson = dataTable.Rows[0].Field<string>("filledvariables"),
                Version = dataTable.Rows[0].Field<int>("version")
            };
        }

        /// <inheritdoc />
        public async Task<object> GenerateDynamicContentHtmlAsync(DynamicContent dynamicContent, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            if (String.IsNullOrWhiteSpace(dynamicContent?.Name) || String.IsNullOrWhiteSpace(dynamicContent?.SettingsJson))
            {
                return "";
            }

            string viewComponentName;
            switch (dynamicContent.Name)
            {
                case "JuiceControlLibrary.MLSimpleMenu":
                case "JuiceControlLibrary.SimpleMenu":
                case "JuiceControlLibrary.ProductModule":
                {
                    viewComponentName = "Repeater";
                    break;
                }
                case "JuiceControlLibrary.AccountWiser2":
                {
                    viewComponentName = "Account";
                    break;
                }
                case "JuiceControlLibrary.ShoppingBasket":
                {
                    viewComponentName = "ShoppingBasket";
                    break;
                }
                case "JuiceControlLibrary.WebPage":
                {
                    viewComponentName = "WebPage";
                    break;
                }
                case "JuiceControlLibrary.Pagination":
                {
                    viewComponentName = "Pagination";
                    break;
                }
                case "JuiceControlLibrary.DynamicFilter":
                {
                    viewComponentName = "Filter";
                    break;
                }
                case "JuiceControlLibrary.Sendform":
                {
                    viewComponentName = "WebForm";
                    break;
                }
                case "JuiceControlLibrary.Configurator":
                {
                    viewComponentName = "Configurator";
                    break;
                }
                case "JuiceControlLibrary.DataSelectorParser":
                {
                    viewComponentName = "DataSelectorParser";
                    break;
                }
                default:
                    return $"<!-- Dynamic content type '{dynamicContent.Name}' not supported yet. Content ID: {dynamicContent.Id} -->";
            }

            // Create a fake ViewContext (but with a real ActionContext and a real HttpContext).
            var viewContext = new ViewContext(
                actionContextAccessor.ActionContext,
                NullView.Instance,
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                new TempDataDictionary(httpContextAccessor.HttpContext, tempDataProvider),
                TextWriter.Null,
                new HtmlHelperOptions());

            // Set the context in the ViewComponentHelper, so that the ViewComponents that we use actually have the proper context.
            (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);

            // Dynamically invoke the correct ViewComponent.
            var component = await viewComponentHelper.InvokeAsync(viewComponentName, new { dynamicContent, callMethod, forcedComponentMode, extraData });

            // If there is a InvokeMethodResult, it means this that a specific method on a specific component was called via /gclcomponent.gcl
            // and we only want to return the results of that method, instead of rendering the entire component.
            if (viewContext.TempData.ContainsKey("InvokeMethodResult") && viewContext.TempData["InvokeMethodResult"] != null)
            {
                return (viewContext.TempData["InvokeMethodResult"], viewContext.ViewData);
            }

            await using var stringWriter = new StringWriter();
            component.WriteTo(stringWriter, HtmlEncoder.Default);
            var html = stringWriter.ToString();
            return html;
        }

        /// <inheritdoc />
        public async Task<object> GenerateDynamicContentHtmlAsync(int componentId, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            var dynamicContent = await GetDynamicContentData(componentId);
            return await GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllDynamicContentAsync(string template, List<DynamicContent> componentOverrides = null)
        {
            return await ReplaceAllDynamicContentAsync(this, template, componentOverrides);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllDynamicContentAsync(ITemplatesService templatesService, string template, List<DynamicContent> componentOverrides = null)
        {
            if (String.IsNullOrWhiteSpace(template))
            {
                return template;
            }

            // TODO: Test the speed of this and see if it's better run a while loop on string.Contains("contentid=") instead of the regular expression.
            // Timeout on the regular expression to prevent denial of service attacks.
            var regEx = new Regex(@"<img[^>]*?(?:data=['""](?<data>.*?)['""][^>]*?)?contentid=['""](?<contentid>\d+)['""][^>]*?/?>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMinutes(3));

            var matches = regEx.Matches(template);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                if (!Int32.TryParse(match.Groups["contentid"].Value, out var contentId) || contentId <= 0)
                {
                    logger.LogWarning($"Found dynamic content with invalid contentId of '{match.Groups["contentid"].Value}', so ignoring it.");
                    continue;
                }

                try
                {
                    var extraData = match.Groups["data"].Value?.ToDictionary("&", "=");
                    var html = await templatesService.GenerateDynamicContentHtmlAsync(contentId, extraData: extraData);
                    template = template.Replace(match.Value, (string)html);
                }
                catch (Exception exception)
                {
                    logger.LogError($"An error while generating component with id '{contentId}': {exception}");
                    var errorOnPage = $"An error occurred while generating component with id '{contentId}'";
                    if (gclSettings.Environment == Environments.Development || gclSettings.Environment == Environments.Test)
                    {
                        errorOnPage += $": {exception.Message}";
                    }

                    template = template.Replace(match.Value, errorOnPage);
                }
            }

            return template;
        }

        /// <inheritdoc />
        public async Task<JArray> GetJsonResponseFromQueryAsync(QueryTemplate queryTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false, bool recursive = false)
        {
            var query = queryTemplate?.Content;
            if (String.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            queryTemplate.GroupingSettings ??= new QueryGroupingSettings();
            query = await DoReplacesAsync(query, true, false, true, null, true, false, true, TemplateTypes.Query);
            if (query.Contains("{filters}", StringComparison.OrdinalIgnoreCase))
            {
                query = query.ReplaceCaseInsensitive("{filters}", (await filtersService.GetFilterQueryPartAsync()).JoinPart);
            }

            var pusherRegex = new Regex(@"PUSHER<channel\((.*?)\),event\((.*?)\),message\(((?s:.)*?)\)>", RegexOptions.Compiled);
            var pusherMatches = pusherRegex.Matches(query);
            foreach (Match match in pusherMatches)
            {
                query = query.Replace(match.Value, "");
            }

            if (recursive)
            {
                queryTemplate.GroupingSettings.GroupingColumn = "id";
            }

            var dataTable = await databaseConnection.GetAsync(query);
            var result = dataTable.Rows.Count == 0 ? new JArray() : dataTable.ToJsonArray(queryTemplate.GroupingSettings, encryptionKey, skipNullValues, allowValueDecryption, recursive);

            if (pusherMatches.Any())
            {
                throw new NotImplementedException("Pusher messages not yet implemented");
            }

            return result;
        }

        /// <inheritdoc />
        public Task<JArray> GetJsonResponseFromRoutineAsync(RoutineTemplate routineTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false)
        {
            throw new NotImplementedException("Legacy templates don't support executing routines.");
        }

        /// <inheritdoc />
        public async Task<TemplateDataModel> GetTemplateDataAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            return await GetTemplateDataAsync(this, id, name, parentId, parentName);
        }

        /// <inheritdoc />
        public async Task<TemplateDataModel> GetTemplateDataAsync(ITemplatesService templatesService, int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            var template = await templatesService.GetTemplateAsync(id, name, TemplateTypes.Html, parentId, parentName);

            var cssStringBuilder = new StringBuilder();
            var jsStringBuilder = new StringBuilder();
            var externalCssFilesList = new List<string>();
            var externalJavaScriptFilesList = new List<string>();
            foreach (var templateId in template.CssTemplates.Concat(template.JavascriptTemplates))
            {
                var linkedTemplate = await templatesService.GetTemplateAsync(templateId);
                (linkedTemplate.Type == TemplateTypes.Css ? cssStringBuilder : jsStringBuilder).Append(linkedTemplate.Content);
                (linkedTemplate.Type == TemplateTypes.Css ? externalCssFilesList : externalJavaScriptFilesList).AddRange(linkedTemplate.ExternalFiles);
            }

            return new TemplateDataModel
            {
                Content = template.Content,
                LinkedCss = cssStringBuilder.ToString(),
                LinkedJavascript = jsStringBuilder.ToString(),
                ExternalCssFiles = externalCssFilesList,
                ExternalJavaScriptFiles = externalJavaScriptFilesList
            };
        }

        /// <inheritdoc />
        public Task<bool> ExecutePreLoadQueryAndRememberResultsAsync(Template template)
        {
            // Do nothing here, this functionality is not supported for old/legacy the templates module.
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<bool> ExecutePreLoadQueryAndRememberResultsAsync(ITemplatesService templatesService, Template template)
        {
            // Do nothing here, this functionality is not supported for old/legacy the templates module.
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<string> GetTemplateOutputCacheFileNameAsync(Template contentTemplate, string extension = ".html")
        {
            var originalUri = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext);
            var cacheFileName = new StringBuilder($"template_{contentTemplate.Id}_");
            switch (contentTemplate.CachingMode)
            {
                case TemplateCachingModes.ServerSideCaching:
                    break;
                case TemplateCachingModes.ServerSideCachingPerUrl:
                    cacheFileName.Append(Uri.EscapeDataString(originalUri.AbsolutePath.ToSha512Simple()));
                    break;
                case TemplateCachingModes.ServerSideCachingPerUrlAndQueryString:
                    cacheFileName.Append(Uri.EscapeDataString(originalUri.PathAndQuery.ToSha512Simple()));
                    break;
                case TemplateCachingModes.ServerSideCachingPerHostNameAndQueryString:
                    cacheFileName.Append(Uri.EscapeDataString(originalUri.ToString().ToSha512Simple()));
                    break;
                case TemplateCachingModes.NoCaching:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException(nameof(contentTemplate.CachingMode), contentTemplate.CachingMode.ToString());
            }

            // If the caching should deviate based on certain cookies, then the names and values of those cookies should be added to the file name.
            var cookieCacheDeviation = (await objectsService.FindSystemObjectByDomainNameAsync("contentcaching_cookie_deviation")).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (cookieCacheDeviation.Length > 0)
            {
                var requestCookies = httpContextAccessor.HttpContext?.Request.Cookies;
                foreach (var cookieName in cookieCacheDeviation)
                {
                    if (requestCookies == null || !requestCookies.TryGetValue(cookieName, out var cookieValue))
                    {
                        continue;
                    }

                    var combinedCookiePart = $"{cookieName}:{cookieValue}";
                    cacheFileName.Append($"_{Uri.EscapeDataString(combinedCookiePart.ToSha512Simple())}");
                }
            }

            // And finally add the language code to the file name.
            if (!String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
            {
                cacheFileName.Append($"_{languagesService.CurrentLanguageCode}");
            }

            if (String.IsNullOrEmpty(extension))
            {
                return cacheFileName.ToString();
            }

            if (!extension.StartsWith("."))
            {
                extension = $".{extension}";
            }

            cacheFileName.Append(extension);

            return cacheFileName.ToString();
        }

        /// <inheritdoc />
        public Task<List<Template>> GetTemplateUrlsAsync()
        {
            // Return an empty result here. This functionality is not made for legacy templates.
            return Task.FromResult(new List<Template>());
        }

        /// <inheritdoc />
        public Task<bool> ComponentRenderingShouldBeLoggedAsync(int componentId)
        {
            // Return an empty result here. This functionality is not made for legacy templates.
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> TemplateRenderingShouldBeLoggedAsync(int templateId)
        {
            // Return an empty result here. This functionality is not made for legacy templates.
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task AddTemplateOrComponentRenderingLogAsync(int componentId, int templateId, int version, DateTime startTime, DateTime endTime, long timeTaken, string error = "")
        {
            // Return an empty result here. This functionality is not made for legacy templates.
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<List<PageWidgetModel>> GetGlobalPageWidgetsAsync()
        {
            // Return an empty result here. This functionality is not made for legacy templates.
            return Task.FromResult(new List<PageWidgetModel>());
        }

        /// <inheritdoc />
        public Task<List<PageWidgetModel>> GetPageWidgetsAsync(int templateId, bool includeGlobalSnippets = true)
        {
            // Return an empty result here. This functionality is not made for legacy templates.
            return Task.FromResult(new List<PageWidgetModel>());
        }

        /// <inheritdoc />
        public Task<List<PageWidgetModel>> GetPageWidgetsAsync(ITemplatesService templatesService, int templateId, bool includeGlobalSnippets = true)
        {
            // Return an empty result here. This functionality is not made for legacy templates.
            return Task.FromResult(new List<PageWidgetModel>());
        }

        /// <summary>
        /// Do all replacement which have to do with request, session or cookie.
        /// Only use this function if you can't add ITemplatesService via dependency injection, otherwise you should use the non static functions <see cref="IStringReplacementsService.DoSessionReplacements" /> and <see cref="IStringReplacementsService.DoHttpRequestReplacements"/>.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="httpContext"></param>
        public static string DoHttpContextReplacements(string input, HttpContext httpContext)
        {
            // Querystring replaces.
            foreach (var key in httpContext.Request.Query.Keys)
            {
                input = input.ReplaceCaseInsensitive($"{{{key}}}", httpContext.Request.Query[key]);
            }

            // Form replaces.
            if (httpContext.Request.HasFormContentType)
            {
                foreach (var variable in httpContext.Request.Form.Keys)
                {
                    input = input.ReplaceCaseInsensitive($"{{{variable}}}", httpContext.Request.Form[variable]);
                }
            }

            // Session replaces.
            if (httpContext?.Features.Get<ISessionFeature>() != null && httpContext.Session.IsAvailable)
            {
                foreach (var variable in httpContext.Session.Keys)
                {
                    input = input.ReplaceCaseInsensitive($"{{{variable}}}", httpContext.Session.GetString(variable));
                }
            }

            // Cookie replaces.
            foreach (var key in httpContext.Request.Cookies.Keys)
            {
                input = input.ReplaceCaseInsensitive($"{{{key}}}", httpContext.Request.Cookies[key]);
            }

            return input;
        }
    }
}
