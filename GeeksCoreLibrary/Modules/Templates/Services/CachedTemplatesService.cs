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
using GeeksCoreLibrary.Core.Extensions;

namespace GeeksCoreLibrary.Modules.Templates.Services
{
    public class CachedTemplatesService : ITemplatesService
    {
        private readonly ILogger<LegacyCachedTemplatesService> logger;
        private readonly ITemplatesService templatesService;
        private readonly IAppCache cache;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICacheService cacheService;

        public CachedTemplatesService(ILogger<LegacyCachedTemplatesService> logger, ITemplatesService templatesService, IAppCache cache, IOptions<GclSettings> gclSettings, IDatabaseConnection databaseConnection, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, ICacheService cacheService)
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
            var joinPart = "";
            var whereClause = new List<string>();
            if (gclSettings.Environment == Environments.Development)
            {
                joinPart = $" JOIN (SELECT template_id, MAX(version) AS maxVersion FROM {WiserTableNames.WiserTemplate} GROUP BY template_id) AS maxVersion ON template.template_id = maxVersion.template_id AND template.version = maxVersion.maxVersion";
            }
            else
            {
                whereClause.Add($"(template.published_environment & {(int)gclSettings.Environment}) = {(int)gclSettings.Environment}");
            }

            whereClause.Add("template.removed = 0");
            
            var query = $@"SELECT
                            IFNULL(parent5.template_name, IFNULL(parent4.template_name, IFNULL(parent3.template_name, IFNULL(parent2.template_name, parent1.template_name)))) as root_name, 
                            parent1.template_name AS parent_name, 
                            template.parent_id,
                            template.template_name,
                            template.template_type,
                            template.ordering,
                            parent1.ordering AS parent_ordering,
                            template.template_id,
                            GROUP_CONCAT(DISTINCT linkedCssTemplate.template_id) AS css_templates, 
                            GROUP_CONCAT(DISTINCT linkedJavascriptTemplate.template_id) AS javascript_templates,
                            template.load_always,
                            template.changed_on,
                            template.external_files,
                            template.template_data_minified,
                            template.template_data,
                            template.url_regex,
                            template.use_cache,
                            template.cache_minutes,
                            0 AS use_obfuscate,
                            template.insert_mode,
                            template.grouping_create_object_instead_of_array,
                            template.grouping_key_column_name,
                            template.grouping_value_column_name,
                            template.grouping_key,
                            template.grouping_prefix
                        FROM {WiserTableNames.WiserTemplate} AS template
                        {joinPart}
                        LEFT JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)
                        LEFT JOIN {WiserTableNames.WiserTemplate} AS parent2 ON parent2.template_id = parent1.parent_id AND parent2.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent1.parent_id)
                        LEFT JOIN {WiserTableNames.WiserTemplate} AS parent3 ON parent3.template_id = parent2.parent_id AND parent3.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent2.parent_id)
                        LEFT JOIN {WiserTableNames.WiserTemplate} AS parent4 ON parent4.template_id = parent3.parent_id AND parent4.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent3.parent_id)
                        LEFT JOIN {WiserTableNames.WiserTemplate} AS parent5 ON parent5.template_id = parent4.parent_id AND parent5.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent4.parent_id)

                        LEFT JOIN {WiserTableNames.WiserTemplate} AS linkedCssTemplate ON FIND_IN_SET(linkedCssTemplate.template_id, template.linked_templates) AND linkedCssTemplate.template_type IN (2, 3) AND linkedCssTemplate.removed = 0
                        LEFT JOIN {WiserTableNames.WiserTemplate} AS linkedJavascriptTemplate ON FIND_IN_SET(linkedJavascriptTemplate.template_id, template.linked_templates) AND linkedJavascriptTemplate.template_type = 4 AND linkedJavascriptTemplate.removed = 0

                        WHERE {String.Join(" AND ", whereClause)}
                        GROUP BY template.template_id
                        ORDER BY parent5.ordering ASC, parent4.ordering ASC, parent3.ordering ASC, parent2.ordering ASC, parent1.ordering ASC, template.ordering ASC";

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
            var query = gclSettings.Environment == Environments.Development 
                ? @$"SELECT 
                    component.content_id,
                    component.settings, 
                    component.component,
                    component.version
                FROM {WiserTableNames.WiserDynamicContent} AS component
                LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
                WHERE otherVersion.id IS NULL" 
                : @$"SELECT 
                    component.content_id,
                    component.settings, 
                    component.component,
                    component.version
                FROM {WiserTableNames.WiserDynamicContent} AS component
                WHERE (component.published_environment & {(int)gclSettings.Environment}) = {(int)gclSettings.Environment}";

            databaseConnection.ClearParameters();
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return dynamicContent;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var contentId = dataRow.Field<int>("content_id");
                dynamicContent.Add(contentId, new DynamicContent
                {
                    Id = contentId,
                    Name = dataRow.Field<string>("component"),
                    SettingsJson = dataRow.Field<string>("settings"),
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
        public async Task<string> ReplaceAllDynamicContentAsync(string template)
        {
            // TODO: This code is exactly the same as in the normal TemplatesService, but that is needed because we need to call "GenerateDynamicContentHtmlAsync" inside the CachedTemplatesService instead of the TemplatesService.
            // TODO: Figure out if there is a better way to do this.
            if (String.IsNullOrWhiteSpace(template))
            {
                return template;
            }
            
            // Timeout on the regular expression to prevent denial of service attacks.
            var regEx = new Regex(@"<div[^<>]*?(?:class=['""]dynamic-content['""][^<>]*?)?(?:data=['""](?<data>.*?)['""][^>]*?)?(component-id|content-id)=['""](?<contentId>\d+)['""][^>]*?>[^<>]*?<h2>[^<>]*?(?<title>[^<>]*?)<\/h2>[^<>]*?<\/div>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMinutes(3));

            var matches = regEx.Matches(template);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                if (!Int32.TryParse(match.Groups["contentId"].Value, out var contentId) || contentId <= 0)
                {
                    logger.LogWarning($"Found dynamic content with invalid contentId of '{match.Groups["contentId"].Value}', so ignoring it.");
                    continue;
                }

                try
                {
                    var extraData = match.Groups["data"].Value?.ToDictionary("&", "=");
                    var (html, _) = await GenerateDynamicContentHtmlAsync(contentId, extraData: extraData);
                    template = template.Replace(match.Value, (string)html);
                }
                catch (Exception exception)
                {
                    logger.LogError($"An error while generating component with id '{contentId}': {exception}");
                    var errorOnPage = $"An error occurred while generating component with id '{contentId}'";
                    if (gclSettings.Environment is Environments.Development or Environments.Test)
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
            return await templatesService.GetJsonResponseFromQueryAsync(queryTemplate, encryptionKey, skipNullValues, allowValueDecryption, recursive);
        }

        /// <inheritdoc />
        public async Task<TemplateDataModel> GetTemplateDataAsync(int id = 0, string name = "", TemplateTypes type = TemplateTypes.Html, int parentId = 0, string parentName = "")
        {
            return await this.templatesService.GetTemplateDataAsync(id, name, type, parentId, parentName);
        }
    }
}
