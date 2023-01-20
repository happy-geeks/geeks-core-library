using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
    public class TemplatesService : ITemplatesService
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
        private readonly IDatabaseHelpersService databaseHelpersService;

        /// <summary>
        /// Initializes a new instance of <see cref="LegacyTemplatesService"/>.
        /// </summary>
        public TemplatesService(ILogger<LegacyTemplatesService> logger,
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
            IAccountsService accountsService,
            IDatabaseHelpersService databaseHelpersService)
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
            this.databaseHelpersService = databaseHelpersService;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "", bool includeContent = true)
        {
            if (id <= 0 && String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
            }

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

            var useTypeFilter = false;

            if (id > 0)
            {
                databaseConnection.AddParameter("id", id);
                whereClause.Add("template.template_id = ?id");
            }
            else
            {
                databaseConnection.AddParameter("name", name);
                whereClause.Add("template.template_name = ?name");
                useTypeFilter = type.HasValue;
            }

            if (parentId > 0)
            {
                databaseConnection.AddParameter("parentId", parentId);
                whereClause.Add("template.parent_id = ?parentId");
            }
            else if (!String.IsNullOrWhiteSpace(parentName))
            {
                databaseConnection.AddParameter("parentName", parentName);
                whereClause.Add("parent1.template_name = ?parentName");
            }

            if (useTypeFilter)
            {
                databaseConnection.AddParameter("templateType", (int)type.Value);
                whereClause.Add("template.template_type = ?templateType");
            }

            whereClause.Add("template.removed = 0");

            var query = $@"SELECT
    COALESCE(parent5.template_name, parent4.template_name, parent3.template_name, parent2.template_name, parent1.template_name) AS root_name,
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
    {(includeContent ? "template.template_data_minified, template.template_data," : "")}
    template.url_regex,
    template.use_cache,
    template.cache_minutes,
    template.cache_location,
    template.cache_regex,
    0 AS use_obfuscate,
    template.insert_mode,
    template.grouping_create_object_instead_of_array,
    template.grouping_key_column_name,
    template.grouping_value_column_name,
    template.grouping_key,
    template.grouping_prefix,
    template.pre_load_query,
    template.return_not_found_when_pre_load_query_has_no_data,
    template.login_required,
    template.login_role,
    template.login_redirect_url,
    template.routine_type,
    template.routine_parameters,
    template.routine_return_type,
    template.trigger_timing,
    template.trigger_event,
    template.trigger_table_name,
    template.is_partial,
    template.version
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
                LoginRedirectUrl = result.LoginRedirectUrl,
                LoginRoles = result.LoginRoles
            };

            if (httpContextAccessor.HttpContext == null)
            {
                // No context available; return empty template without doing a login check.
                return emptyTemplate;
            }

            // Check current login, and match user's roles against required roles of the template.
            var userData = await accountsService.GetUserDataFromCookieAsync();

            if (userData is not {UserId: > 0} || (result.LoginRoles != null && result.LoginRoles.Any() && userData.Roles != null && !userData.Roles.Any(role => result.LoginRoles.Contains(role.Id))))
            {
                return emptyTemplate;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateCacheSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            if (id <= 0 && String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
            }

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

            if (id > 0)
            {
                databaseConnection.AddParameter("id", id);
                whereClause.Add("template.template_id = ?id");
            }
            else
            {
                databaseConnection.AddParameter("name", name);
                whereClause.Add("template.template_name = ?name");
            }

            if (parentId > 0)
            {
                databaseConnection.AddParameter("parentId", parentId);
                joinPart += $" JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)";
                whereClause.Add("template.parent_id = ?parentId");
            }
            else if (!String.IsNullOrWhiteSpace(parentName))
            {
                databaseConnection.AddParameter("parentName", parentName);
                joinPart += $" JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)";
                whereClause.Add("parent1.template_name = ?parentName");
            }

            whereClause.Add("template.removed = 0");

            var query = $@"SELECT
                            template.template_name,
                            template.template_id,
                            template.use_cache,
                            template.cache_minutes,
                            template.cache_location, 
                            template.cache_regex,
                            template.template_type
                        FROM {WiserTableNames.WiserTemplate} AS template
                        {joinPart}

                        WHERE {String.Join(" AND ", whereClause)}
                        GROUP BY template.template_id
                        LIMIT 1";

            var dataTable = await databaseConnection.GetAsync(query);
            var result = dataTable.Rows.Count == 0 ? new Template() : new Template
            {
                Id = dataTable.Rows[0].Field<int>("template_id"),
                Name = dataTable.Rows[0].Field<string>("template_name"),
                CachingMinutes = dataTable.Rows[0].Field<int>("cache_minutes"),
                CachingMode = dataTable.Rows[0].Field<TemplateCachingModes>("use_cache"),
                CachingLocation = dataTable.Rows[0].Field<TemplateCachingLocations>("cache_location"),
                CachingRegex = dataTable.Rows[0].Field<string>("cache_regex"),
                Type = dataTable.Rows[0].Field<TemplateTypes>("template_type")
            };

            return result;
        }

        /// <inheritdoc />
        public async Task<int> GetTemplateIdFromNameAsync(string name, TemplateTypes type)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"The parameter {nameof(name)} must contain a value");
            }

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

            databaseConnection.AddParameter("name", name);
            whereClause.Add("template.template_name = ?name");

            if (type is TemplateTypes.Css or TemplateTypes.Scss)
            {
                whereClause.Add($"template.template_type IN ({(int)TemplateTypes.Css}, {(int)TemplateTypes.Scss})");
            }
            else
            {
                databaseConnection.AddParameter("type", (int)type);
                whereClause.Add("template.template_type = ?type");
            }

            whereClause.Add("template.removed = 0");

            var query = $@"SELECT template.template_id
                        FROM {WiserTableNames.WiserTemplate} AS template
                        {joinPart}

                        WHERE {String.Join(" AND ", whereClause)}
                        LIMIT 1";

            var dataTable = await databaseConnection.GetAsync(query);
            return dataTable.Rows.Count == 0 ? 0 : dataTable.Rows[0].Field<int>("template_id");
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetGeneralTemplateLastChangedDateAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard)
        {
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
            whereClause.Add("template.load_always = 1");

            whereClause.Add(templateType is TemplateTypes.Css or TemplateTypes.Scss
                ? $"template.template_type IN ({(int)TemplateTypes.Css}, {(int)TemplateTypes.Scss})"
                : $"template.template_type = {(int)templateType}");

            whereClause.Add($"template.insert_mode = {(int)byInsertMode}");

            var query = $@"SELECT MAX(template.changed_on) AS lastChanged
                        FROM {WiserTableNames.WiserTemplate} AS template
                        {joinPart}
                        WHERE {String.Join(" AND ", whereClause)}";

            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return dataTable.Rows[0].Field<DateTime?>("lastChanged");
        }

        /// <inheritdoc />
        public async Task<List<Template>> GetTemplatesAsync(ICollection<int> templateIds, bool includeContent)
        {
            var results = new List<Template>();
            databaseConnection.AddParameter("includeContent", includeContent);

            var joinPart = "";
            var whereClause = new List<string> { $"template.template_id IN ({String.Join(",", templateIds)})", "template.removed = 0" };
            if (gclSettings.Environment == Environments.Development)
            {
                joinPart = $" JOIN (SELECT template_id, MAX(version) AS maxVersion FROM {WiserTableNames.WiserTemplate} GROUP BY template_id) AS maxVersion ON template.template_id = maxVersion.template_id AND template.version = maxVersion.maxVersion";
            }
            else
            {
                whereClause.Add($"(template.published_environment & {(int)gclSettings.Environment}) = {(int)gclSettings.Environment}");
            }

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
                            {(includeContent ? "template.template_data_minified, template.template_data," : "")}
                            template.url_regex,
                            template.use_cache,
                            template.cache_minutes,
                            template.cache_location,
                            template.cache_regex,
                            0 AS use_obfuscate,
                            template.insert_mode,
                            template.grouping_create_object_instead_of_array,
                            template.grouping_key_column_name,
                            template.grouping_value_column_name,
                            template.grouping_key,
                            template.grouping_prefix,
                            template.pre_load_query,
                            template.return_not_found_when_pre_load_query_has_no_data,
                            template.version
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
        public async Task<TemplateResponse> GetGeneralTemplateValueAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard)
        {
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
            whereClause.Add("template.load_always = 1");

            whereClause.Add(templateType is TemplateTypes.Css or TemplateTypes.Scss
                ? $"template.template_type IN ({(int)TemplateTypes.Css}, {(int)TemplateTypes.Scss})"
                : $"template.template_type = {(int)templateType}");

            whereClause.Add($"template.insert_mode = {(int)byInsertMode}");

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
                            template.cache_location,
                            template.cache_regex,
                            0 AS use_obfuscate,
                            template.insert_mode,
                            template.grouping_create_object_instead_of_array,
                            template.grouping_key_column_name,
                            template.grouping_value_column_name,
                            template.grouping_key,
                            template.grouping_prefix,
                            template.version
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

            // Start with special template replacements for the pre load query that you can set in HTML templates in the templates module in Wiser.
            if (httpContextAccessor != null && httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.Items.ContainsKey(Constants.TemplatePreLoadQueryResultKey))
            {
                input = stringReplacementsService.DoReplacements(input, (DataRow)httpContextAccessor.HttpContext.Items[Constants.TemplatePreLoadQueryResultKey], forQuery, prefix: "{template.");
            }

            // Then do the normal string replacements, because includes can contain variables in a query string, which need to be replaced first.
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
            if (String.IsNullOrWhiteSpace(resizeMode))
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
                var dataTable = await databaseConnection.GetAsync(@$"SELECT
    item_id,
    file_name,
    property_name
FROM `{WiserTableNames.WiserItemFile}`
WHERE {queryWherePart}
AND IF(?propertyName = '', 1=1, property_name = ?propertyName)
AND content_type LIKE 'image%'
ORDER BY id ASC");

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
                            content = content.Replace("<div class=\"dynamic-content", $"<div data=\"{queryString}\" class=\"/dynamic-content");
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
                            content = content.Replace("<div class=\"dynamic-content", $"<div data=\"{queryString}\" class=\"/dynamic-content");
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
            var query = gclSettings.Environment == Environments.Development
                ? @$"SELECT 
                    component.content_id,
                    component.settings,
                    component.component,
                    component.component_mode,
                    component.version,
                    component.title
                FROM {WiserTableNames.WiserDynamicContent} AS component
                LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
                WHERE component.content_id = ?contentId
                AND otherVersion.id IS NULL"
                : @$"SELECT 
                    component.content_id,
                    component.settings,
                    component.component,
                    component.component_mode,
                    component.version,
                    component.title
                FROM {WiserTableNames.WiserDynamicContent} AS component
                WHERE component.content_id = ?contentId
                AND (component.published_environment & {(int)gclSettings.Environment}) = {(int)gclSettings.Environment}
                ORDER BY component.version DESC
                LIMIT 1";

            databaseConnection.AddParameter("contentId", contentId);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return new DynamicContent
            {
                Id = contentId,
                Name = dataTable.Rows[0].Field<string>("component"),
                SettingsJson = dataTable.Rows[0].Field<string>("settings"),
                ComponentMode = dataTable.Rows[0].Field<string>("component_mode"),
                Version = dataTable.Rows[0].Field<int>("version"),
                Title = dataTable.Rows[0].Field<string>("title")
            };
        }

        /// <inheritdoc />
        public async Task<object> GenerateDynamicContentHtmlAsync(DynamicContent dynamicContent, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            if (String.IsNullOrWhiteSpace(dynamicContent?.Name) || String.IsNullOrWhiteSpace(dynamicContent?.SettingsJson))
            {
                return "";
            }

            var logRenderingOfComponent = await ComponentRenderingShouldBeLoggedAsync(dynamicContent.Id);
            var error = "";
            var startTime = DateTime.Now;
            var stopWatch = new Stopwatch();
            try
            {
                if (logRenderingOfComponent)
                {
                    stopWatch.Start();
                }

                var viewComponentName = dynamicContent.Name;

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
                var component = await viewComponentHelper.InvokeAsync(viewComponentName, new {dynamicContent, callMethod, forcedComponentMode, extraData});

                // If there is a InvokeMethodResult, it means this that a specific method on a specific component was called via /gclcomponent.gcl
                // and we only want to return the results of that method, instead of rendering the entire component.
                if (viewContext.TempData.ContainsKey("InvokeMethodResult") && viewContext.TempData["InvokeMethodResult"] != null)
                {
                    return viewContext.TempData["InvokeMethodResult"];
                }

                await using var stringWriter = new StringWriter();
                component.WriteTo(stringWriter, HtmlEncoder.Default);
                var html = stringWriter.ToString();
                return html;
            }
            catch (Exception exception)
            {
                error = exception.ToString();
                throw;
            }
            finally
            {
                if (logRenderingOfComponent)
                {
                    stopWatch.Stop();
                    var endTime = DateTime.Now;
                    await AddTemplateOrComponentRenderingLogAsync(dynamicContent.Id, 0, dynamicContent.Version, startTime, endTime, stopWatch.ElapsedMilliseconds, error);
                }
            }
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
                    logger.LogWarning($"Found dynamic content with invalid componentId of '{match.Groups["contentId"].Value}', so ignoring it.");
                    continue;
                }

                try
                {
                    var extraData = match.Groups["data"].Value?.ToDictionary("&", "=");
                    var dynamicContentData = componentOverrides?.FirstOrDefault(d => d.Id == contentId);
                    var html = dynamicContentData == null ? await templatesService.GenerateDynamicContentHtmlAsync(contentId, extraData: extraData) : await templatesService.GenerateDynamicContentHtmlAsync(dynamicContentData, extraData: extraData);
                    template = template.Replace(match.Value, $"<!-- Start component {contentId} -->{(string)html}<!-- End component {contentId} -->");
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
        public async Task<JArray> GetJsonResponseFromRoutineAsync(RoutineTemplate routineTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false)
        {
            var routineName = $"WISER_{routineTemplate.Name}";
            // First get all parameters of the routine.
            var query = @"SELECT PARAMETER_NAME
FROM information_schema.parameters 
WHERE SPECIFIC_NAME = ?name
ORDER BY ORDINAL_POSITION ASC";
            databaseConnection.AddParameter("name", routineName);
            var dataTable = await databaseConnection.GetAsync(query);
            var parameters = dataTable.Rows.Cast<DataRow>().Select(dataRow => $"'{{{dataRow.Field<string>("PARAMETER_NAME")}}}'").ToList();

            // Build the query to execute the routine and get the results.
            var queryPrefix = routineTemplate.RoutineType == RoutineTypes.Function ? "SELECT " : "CALL ";
            var querySuffix = routineTemplate.RoutineType == RoutineTypes.Function ? "AS result" : "";
            query = $"{queryPrefix} {routineName}({String.Join(", ", parameters)}) {querySuffix};";
            query = await stringReplacementsService.DoAllReplacementsAsync(query, forQuery: true);
            dataTable = await databaseConnection.GetAsync(query);
            return dataTable.Rows.Count == 0 ? new JArray() : dataTable.ToJsonArray(null, encryptionKey, skipNullValues, allowValueDecryption);
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
        public async Task<bool> ExecutePreLoadQueryAndRememberResultsAsync(Template template)
        {
            return await ExecutePreLoadQueryAndRememberResultsAsync(this, template);
        }

        /// <inheritdoc />
        public async Task<bool> ExecutePreLoadQueryAndRememberResultsAsync(ITemplatesService templatesService, Template template)
        {
            if (httpContextAccessor.HttpContext == null || String.IsNullOrWhiteSpace(template?.PreLoadQuery))
            {
                return true;
            }

            var query = await DoReplacesAsync(templatesService, template.PreLoadQuery, forQuery: true, templateType: TemplateTypes.Query);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return false;
            }

            httpContextAccessor.HttpContext.Items.Add(Constants.TemplatePreLoadQueryResultKey, dataTable.Rows[0]);
            return true;
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
                case TemplateCachingModes.ServerSideCachingBasedOnUrlRegex:
                    if (String.IsNullOrWhiteSpace(contentTemplate.CachingRegex))
                    {
                        throw new Exception($"Caching for template {contentTemplate.Id} is set to {nameof(TemplateCachingModes.ServerSideCachingBasedOnUrlRegex)}, but no regex has been entered.");
                    }

                    try
                    {
                        var regex = new Regex(contentTemplate.CachingRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
                        var match = regex.Match(originalUri.PathAndQuery);
                        if (!match.Success)
                        {
                            return "";
                        }

                        // Add all values of named groups to the cache key.
                        foreach (Group group in match.Groups)
                        {
                            if (String.IsNullOrWhiteSpace(group.Name) || Int32.TryParse(group.Name, out _))
                            {
                                // Ignore groups without a name (when you have no name given in the regex, the group name will be a number).
                                continue;
                            }

                            // Strip invalid characters that can't be in a file name.
                            var value = Path.GetInvalidFileNameChars().Aggregate(group.Value, (current, character) => current.Replace(character, '-'));
                            
                            // Add the group value to the file name.
                            cacheFileName.Append($"{Uri.EscapeDataString(value)}_");
                        }
                    }
                    catch (ArgumentException argumentException)
                    {
                        // ArgumentException will be thrown if the regex is not valid.
                        logger.LogWarning(argumentException, $"Caching for template {contentTemplate.Id} is set to {nameof(TemplateCachingModes.ServerSideCachingBasedOnUrlRegex)}, but an invalid regex has been entered.");
                        throw new Exception($"Caching for template {contentTemplate.Id} is set to {nameof(TemplateCachingModes.ServerSideCachingBasedOnUrlRegex)}, but an invalid regex has been entered. The exact error was: {argumentException.Message}");
                    }

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
        public async Task<List<Template>> GetTemplateUrlsAsync()
        {
            string query;
            if (gclSettings.Environment == Environments.Development)
            {
                query = $@"SELECT
	template.template_id,
	template.template_type,
	template.url_regex
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE otherVersion.id IS NULL
AND template.template_type IN ({(int)TemplateTypes.Html}, {(int)TemplateTypes.Query}, {(int)TemplateTypes.Routine})
AND template.url_regex IS NOT NULL
AND template.url_regex <> ''";
            }
            else
            {
                query = $@"
SELECT 
	template.template_id,
	template.template_type,
	template.url_regex
FROM {WiserTableNames.WiserTemplate} AS template
WHERE (template.published_environment & {(int)gclSettings.Environment}) = {(int)gclSettings.Environment}
AND template.template_type IN ({(int)TemplateTypes.Html}, {(int)TemplateTypes.Query}, {(int)TemplateTypes.Routine})
AND template.url_regex IS NOT NULL
AND template.url_regex <> ''";
            }

            var dataTable = await databaseConnection.GetAsync(query);

            var results = new List<Template>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new Template
                {
                    Id = dataRow.Field<int>("template_id"),
                    Type = dataRow.Field<TemplateTypes>("template_type"),
                    UrlRegex = dataRow.Field<string>("url_regex")
                });
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<List<PageWidgetModel>> GetGlobalPageWidgetsAsync()
        {
            var results = new List<PageWidgetModel>();
            var globalWidgetsQuery = $@"SELECT
	widget.id,
	widget.title,
	IF(languageSpecificHtml.id IS NULL, CONCAT_WS('', genericHtml.`value`, genericHtml.long_value), CONCAT_WS('', languageSpecificHtml.`value`, languageSpecificHtml.long_value)) AS html,
	IFNULL(location.value, '{(int) Constants.PageWidgetDefaultLocation}') AS location
FROM {WiserTableNames.WiserItem} AS widget
LEFT JOIN {WiserTableNames.WiserItemDetail} AS genericHtml ON genericHtml.item_id = widget.id AND genericHtml.`key` = '{Constants.PageWidgetHtmlPropertyName}'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS languageSpecificHtml ON languageSpecificHtml.item_id = widget.id AND languageSpecificHtml.`key` = '{Constants.PageWidgetHtmlPropertyName}' AND languageSpecificHtml.language_code = '{languagesService.CurrentLanguageCode}'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS location ON location.item_id = widget.id AND location.`key` = '{Constants.PageWidgetLocationPropertyName}'
LEFT JOIN {WiserTableNames.WiserItemLink} AS linkToParent ON linkToParent.item_id = widget.id AND linkToParent.type = {Constants.PageWidgetParentLinkType}
WHERE widget.entity_type = '{Constants.PageWidgetEntityType}'
ORDER BY IFNULL(linkToParent.destination_item_id, 0) ASC, IFNULL(linkToParent.ordering, widget.ordering) ASC";

            var dataTable = await databaseConnection.GetAsync(globalWidgetsQuery);
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var html = dataRow.Field<string>("html");
                if (String.IsNullOrWhiteSpace(html))
                {
                    // No point in adding empty widgets to the page.
                    continue;
                }

                results.Add(new PageWidgetModel
                {
                    Location = (PageWidgetLocations) Convert.ToInt32(dataRow["location"]),
                    Html = html
                });
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<List<PageWidgetModel>> GetPageWidgetsAsync(int templateId, bool includeGlobalSnippets = true)
        {
            return await GetPageWidgetsAsync(this, templateId, includeGlobalSnippets);
        }

        /// <inheritdoc />
        public async Task<List<PageWidgetModel>> GetPageWidgetsAsync(ITemplatesService templatesService, int templateId, bool includeGlobalSnippets = true)
        {
            var results = includeGlobalSnippets ? await templatesService.GetGlobalPageWidgetsAsync() : new List<PageWidgetModel>();
            
            if (templateId <= 0)
            {
                return results;
            }
            
            var joinPart = "";
            var whereClause = new List<string> { "template.template_id = ?id", "template.removed = 0" };
            if (gclSettings.Environment == Environments.Development)
            {
                joinPart = $" JOIN (SELECT template_id, MAX(version) AS maxVersion FROM {WiserTableNames.WiserTemplate} GROUP BY template_id) AS maxVersion ON template.template_id = maxVersion.template_id AND template.version = maxVersion.maxVersion";
            }
            else
            {
                whereClause.Add($"(template.published_environment & {(int)gclSettings.Environment}) = {(int)gclSettings.Environment}");
            }

            databaseConnection.AddParameter("id", templateId);
            var query = $@"SELECT
    template.widget_content,
    template.widget_location
FROM {WiserTableNames.WiserTemplate} AS template
{joinPart}

WHERE {String.Join(" AND ", whereClause)}
LIMIT 1";

            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return results;
            }

            var html = dataTable.Rows[0].Field<string>("widget_content");
            if (String.IsNullOrWhiteSpace(html))
            {
                return results;
            }

            results.Add(new PageWidgetModel
            {
                Html = html,
                Location = (PageWidgetLocations) Convert.ToInt32(dataTable.Rows[0]["widget_location"])
            });
            
            return results;
        }

        /// <inheritdoc />
        public async Task<bool> ComponentRenderingShouldBeLoggedAsync(int componentId)
        {
            var logRenderingOfComponentsSetting = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_components_{gclSettings.Environment}");
            if (String.IsNullOrWhiteSpace(logRenderingOfComponentsSetting))
            {
                logRenderingOfComponentsSetting = await objectsService.FindSystemObjectByDomainNameAsync("log_rendering_of_components");
                if (String.IsNullOrWhiteSpace(logRenderingOfComponentsSetting))
                {
                    return false;
                }
            }
            
            if (String.Equals("all", logRenderingOfComponentsSetting, StringComparison.OrdinalIgnoreCase) || String.Equals("true", logRenderingOfComponentsSetting, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var listOfComponentIdsToLog = logRenderingOfComponentsSetting.Split(",").Select(value => !Int32.TryParse(value, out var id) ? 0 : id);
            return listOfComponentIdsToLog.Contains(componentId);
        }

        /// <inheritdoc />
        public async Task<bool> TemplateRenderingShouldBeLoggedAsync(int templateId)
        {
            var logRenderingOfTemplatesSetting = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_templates_{gclSettings.Environment}");
            if (String.IsNullOrWhiteSpace(logRenderingOfTemplatesSetting))
            {
                logRenderingOfTemplatesSetting = await objectsService.FindSystemObjectByDomainNameAsync("log_rendering_of_templates");
                if (String.IsNullOrWhiteSpace(logRenderingOfTemplatesSetting))
                {
                    return false;
                }
            }
            
            if (String.Equals("all", logRenderingOfTemplatesSetting, StringComparison.OrdinalIgnoreCase) || String.Equals("true", logRenderingOfTemplatesSetting, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var listOfTemplateIdsToLog = logRenderingOfTemplatesSetting.Split(",").Select(value => !Int32.TryParse(value, out var id) ? 0 : id);
            return listOfTemplateIdsToLog.Contains(templateId);
        }

        /// <inheritdoc />
        public async Task AddTemplateOrComponentRenderingLogAsync(int componentId, int templateId, int version, DateTime startTime, DateTime endTime, long timeTaken, string error = "")
        {
            try
            {
                var userData = await accountsService.GetUserDataFromCookieAsync();
                
                var tableName = componentId > 0 ? WiserTableNames.WiserDynamicContentRenderLog : WiserTableNames.WiserTemplateRenderLog;
                await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {tableName});
                databaseConnection.AddParameter("rendering_content_id", componentId);
                databaseConnection.AddParameter("rendering_template_id", templateId);
                databaseConnection.AddParameter("rendering_version", version);
                databaseConnection.AddParameter("rendering_url", HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext));
                databaseConnection.AddParameter("rendering_environment", gclSettings.Environment.ToString());
                databaseConnection.AddParameter("rendering_start", startTime);
                databaseConnection.AddParameter("rendering_end", endTime);
                databaseConnection.AddParameter("rendering_time_taken", timeTaken);
                databaseConnection.AddParameter("rendering_user_id", userData.UserId);
                databaseConnection.AddParameter("rendering_language_code", await languagesService.GetLanguageCodeAsync() ?? "");
                databaseConnection.AddParameter("rendering_error", error);

                var idColumn = componentId > 0 ? "content_id" : "template_id";
                var idParameter = componentId > 0 ? "rendering_content_id" : "rendering_template_id";
                var query = $@"INSERT INTO {tableName} ({idColumn}, version, url, environment, start, end, time_taken, user_id, language_code, error)
VALUES (?{idParameter}, ?rendering_version, ?rendering_url, ?rendering_environment, ?rendering_start, ?rendering_end, ?rendering_time_taken, ?rendering_user_id, ?rendering_language_code, ?rendering_error)";
                await databaseConnection.ExecuteAsync(query);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, templateId > 0 
                    ? $"Error while trying to log the render time of template #{templateId}" 
                    : $"Error while trying to log the render time of component #{componentId}");
            }
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
