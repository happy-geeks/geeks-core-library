using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Extensions;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Templates.Services
{
    public class LegacyCachedTemplatesService : ITemplatesService
    {
        private readonly ILogger<LegacyCachedTemplatesService> logger;
        private readonly ITemplatesService templatesService;
        private readonly IAppCache cache;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICacheService cacheService;

        public LegacyCachedTemplatesService(ILogger<LegacyCachedTemplatesService> logger, ITemplatesService templatesService, IAppCache cache, IOptions<GclSettings> gclSettings, IDatabaseConnection databaseConnection, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, ICacheService cacheService)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.cacheService = cacheService;
        }

        /// <summary>
        /// GetAsync templates from database and write them to the MemoryCache if they are not yet there.
        /// </summary>
        private async Task<List<Template>> CacheTemplatesAsync()
        {
            return await cache.GetOrAdd("Templates", GetTemplatesForCacheAsync, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <summary>
        /// Gets all templates from database to be cached.
        /// </summary>
        /// <param name="cacheEntry"></param>
        /// <returns></returns>
        private async Task<List<Template>> GetTemplatesForCacheAsync(ICacheEntry cacheEntry)
        {
            logger.LogDebug("Caching of templates started");
            var templates = new List<Template>();

            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Test => " AND t.istest=1 ",
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
                            t.useobfuscate AS use_obfuscate,
                            t.defaulttemplate AS wiser_cdn_files,
                            t.pagemode AS insert_mode,
                            t.groupingCreateObjectInsteadOfArray AS grouping_create_object_instead_of_array,
                            t.groupingKeyColumnName AS grouping_key_column_name,
                            t.groupingValueColumnName AS grouping_value_column_name,
                            t.groupingkey AS grouping_key,
                            t.groupingprefix AS grouping_prefix
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
                        ORDER BY ippppp.volgnr, ipppp.volgnr, ippp.volgnr, ipp.volgnr, ip.volgnr, i.volgnr";

            await using (var reader = await databaseConnection.GetReaderAsync(query))
            {
                while (await reader.ReadAsync())
                {
                    var template = await reader.ToTemplateModelAsync();

                    templates.Add(template);
                }
            }

            logger.LogDebug("Templates loaded from database");
            cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;

            logger.LogDebug("Caching of templates ended");

            return templates;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateAsync(int id = 0, string name = "", TemplateTypes type = TemplateTypes.Html, int parentId = 0, string parentName = "")
        {
            if (id <= 0 && String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
            }

            var cachedTemplates = await CacheTemplatesAsync();

            if (String.IsNullOrWhiteSpace(name))
            {
                return cachedTemplates.SingleOrDefault(template => template.Id == id) ?? new Template();
            }

            return cachedTemplates.SingleOrDefault(template => template.Name.Equals(name) && template.Type == type) ?? new Template();
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetGeneralTemplateLastChangedDateAsync(TemplateTypes templateType)
        {
            var cachedTemplates = await CacheTemplatesAsync();
            var generalTemplates = cachedTemplates.Where(template => template.LoadAlways && template.Type == templateType).ToList();
            if (!generalTemplates.Any())
            {
                return null;
            }

            return generalTemplates.Max(template => template.LastChanged);
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetGeneralTemplateValueAsync(TemplateTypes templateType)
        {
            var result = new TemplateResponse();
            var resultBuilder = new StringBuilder();
            var idsLoaded = new List<int>();
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).ToString();
            var cachedTemplates = await CacheTemplatesAsync();
            var templatesToUse = cachedTemplates.Where(template => template.LoadAlways && template.Type == templateType);
            foreach (var template in templatesToUse)
            {
                await AddTemplateToResponseAsync(idsLoaded, template, currentUrl, resultBuilder, result);
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
        public async Task<List<Template>> GetTemplatesAsync(ICollection<int> templateIds, bool includeContent)
        {
            var cachedTemplates = await CacheTemplatesAsync();
            return cachedTemplates.Where(template => templateIds.Contains(template.Id)).ToList();
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetCombinedTemplateValueAsync(ICollection<int> templateIds, TemplateTypes templateType)
        {
            var result = new TemplateResponse();
            var resultBuilder = new StringBuilder();
            var idsLoaded = new List<int>();
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).ToString();
            var cachedTemplates = await CacheTemplatesAsync();
            var templatesToUse = cachedTemplates.Where(template => templateIds.Contains(template.Id) && template.Type == templateType);
            foreach (var template in templatesToUse)
            {
                await AddTemplateToResponseAsync(idsLoaded, template, currentUrl, resultBuilder, result);
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
        public Task AddTemplateToResponseAsync(ICollection<int> idsLoaded, Template template, string currentUrl, StringBuilder resultBuilder, TemplateResponse templateResponse)
        {
            return templatesService.AddTemplateToResponseAsync(idsLoaded, template, currentUrl, resultBuilder, templateResponse);
        }

        /// <inheritdoc />
        public Task<string> GetWiserCdnFilesAsync(ICollection<string> fileNames)
        {
            return templatesService.GetWiserCdnFilesAsync(fileNames);
        }

        /// <inheritdoc />
        public Task<string> DoReplacesAsync(string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false)
        {
            return templatesService.DoReplacesAsync(input, handleStringReplacements, handleDynamicContent, evaluateLogicSnippets, dataRow, handleRequest, removeUnknownVariables, forQuery);
        }

        /// <inheritdoc />
        public Task<string> HandleIncludesAsync(string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false)
        {
            return templatesService.HandleIncludesAsync(input, handleStringReplacements, dataRow, handleRequest, forQuery);
        }

        public Task<string> GenerateImageUrl(string itemId, string type, int number, string filename = "", string width = "0", string height = "0", string resizeMode = "")
        {
            return templatesService.GenerateImageUrl(itemId, type, number, filename, width, height);
        }

        /// <inheritdoc />
        public Task<string> HandleImageTemplating(string input)
        {
            return templatesService.HandleImageTemplating(input);
        }

        /// <summary>
        /// Gets all dynamic content data so that they can be cached.
        /// </summary>
        /// <param name="cacheEntry"></param>
        /// <returns></returns>
        private async Task<Dictionary<int, DynamicContent>> GetDynamicContentForCachingAsync(ICacheEntry cacheEntry)
        {
            var dynamicContent = new Dictionary<int, DynamicContent>();
            string query = null;
            var templateVersionPart = "";
            switch (gclSettings.Environment)
            {
                case Environments.Development:
                    // Always get the latest version on development.
                    query = @"SELECT
                                d.id,
                                d.filledvariables, 
                                d.freefield1,
                                d.type,
                                d.version
                            FROM easy_dynamiccontent d
                            JOIN (SELECT id, MAX(version) AS version FROM easy_dynamiccontent GROUP BY id) d2 ON d2.id = d.id AND d2.version = d.version
                            GROUP BY d.id";
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
                            d.id,
                            d.filledvariables, 
                            d.freefield1,
                            d.type,
                            d.version
                        FROM easy_dynamiccontent d
                        JOIN easy_templates t ON t.itemid = d.itemid AND t.version = d.version {templateVersionPart}

                        UNION

                        SELECT
                            d.id,
                            d.filledvariables, 
                            d.freefield1,
                            d.type,
                            d.version
                        FROM easy_dynamiccontent d
                        WHERE d.version = 1 
                        AND d.itemid = 0

                        GROUP BY id";
                        

            databaseConnection.ClearParameters();
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return dynamicContent;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var contentId = dataRow.Field<int>("id");
                dynamicContent.Add(contentId, new DynamicContent
                {
                    Id = contentId,
                    Name = dataRow.Field<string>("freefield1"),
                    SettingsJson = dataRow.Field<string>("filledvariables"),
                    Version = dataRow.Field<int>("version")
                });
            }
            
            cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;

            return dynamicContent;
        }

        /// <summary>
        /// GetAsync templates from database and write them to the MemoryCache if they are not yet there.
        /// </summary>
        private async Task<Dictionary<int, DynamicContent>> CacheDynamicContentAsync()
        {
            return await cache.GetOrAdd("DynamicContent", GetDynamicContentForCachingAsync, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<DynamicContent> GetDynamicContentData(int contentId)
        {
            if (contentId <= 0)
            {
                throw new ArgumentNullException($"The parameter {nameof(contentId)} must contain a value");
            }

            var cachedDynamicContent = await CacheDynamicContentAsync();

            if (!cachedDynamicContent.ContainsKey(contentId))
            {
                return null;
            }

            return cachedDynamicContent[contentId];
        }

        /// <inheritdoc />
        public async Task<(object result, ViewDataDictionary viewData)> GenerateDynamicContentHtmlAsync(int componentId, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            var dynamicContent = await GetDynamicContentData(componentId);
            return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        /// <inheritdoc />
        public Task<(object result, ViewDataDictionary viewData)> GenerateDynamicContentHtmlAsync(DynamicContent dynamicContent, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            return templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllDynamicContentAsync(string template, List<DynamicContent> componentOverrides = null)
        {
            return await ReplaceAllDynamicContentAsync(this, template, componentOverrides);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllDynamicContentAsync(ITemplatesService service, string template, List<DynamicContent> componentOverrides = null)
        {
            return await templatesService.ReplaceAllDynamicContentAsync(service, template, componentOverrides);
        }

        /// <inheritdoc />
        public async Task<JArray> GetJsonResponseFromQueryAsync(QueryTemplate queryTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false, bool recursive = false)
        {
            return await templatesService.GetJsonResponseFromQueryAsync(queryTemplate, encryptionKey, skipNullValues, allowValueDecryption, recursive);
        }

        /// <inheritdoc />
        public async Task<TemplateDataModel> GetTemplateDataAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            return await GetTemplateDataAsync(this, id, name, parentId, parentName);
        }

        /// <inheritdoc />
        public async Task<TemplateDataModel> GetTemplateDataAsync(ITemplatesService service, int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            return await templatesService.GetTemplateDataAsync(service, id, name, parentId, parentName);
        }
    }
}
