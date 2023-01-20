using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Redirect.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using GeeksCoreLibrary.Modules.Templates.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Templates.Services
{
    public class PagesService : IPagesService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly ILogger<PagesService> logger;
        private readonly ITemplatesService templatesService;
        private readonly ISeoService seoService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IRedirectService redirectService;
        private readonly IObjectsService objectsService;
        private readonly IDatabaseConnection databaseConnection;

        public PagesService(IOptions<GclSettings> gclSettings, ILogger<PagesService> logger, IObjectsService objectsService, ITemplatesService templatesService, ISeoService seoService, IHttpContextAccessor httpContextAccessor, IRedirectService redirectService, IDatabaseConnection databaseConnection)
        {
            this.gclSettings = gclSettings.Value;
            this.logger = logger;
            this.templatesService = templatesService;
            this.seoService = seoService;
            this.httpContextAccessor = httpContextAccessor;
            this.redirectService = redirectService;
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
        }

        /// <inheritdoc />
        public async Task<string> GetGlobalHeader(string url, List<int> javascriptTemplates, List<int> cssTemplates)
        {
            int headerTemplateId;
            string headerRegexCheck;
            Template template;
            
            if (!gclSettings.UseLegacyWiser1TemplateModule) 
            {
                var joinPart = "";
                var whereClause = new List<string>();
                if (gclSettings.Environment == Environments.Development)
                {
                    joinPart = $" JOIN (SELECT template_id, MAX(version) AS maxVersion FROM {WiserTableNames.WiserTemplate} GROUP BY template_id) AS maxVersion ON template.template_id = maxVersion.template_id AND template.version = maxVersion.maxVersion";
                }
                else
                {
                    whereClause.Add($"(template.published_environment & {(int) gclSettings.Environment}) = {(int) gclSettings.Environment}");
                }

                whereClause.Add("template.template_type = 1");
                whereClause.Add("template.removed = 0");
                whereClause.Add("template.is_default_header = 1");

                var query = $@"
                SELECT template.template_id, template.default_header_footer_regex
                FROM `{WiserTableNames.WiserTemplate}` AS template
                {joinPart}
                WHERE {String.Join(" AND ", whereClause)}
                GROUP BY template.template_id";

                var globalHeaders = await databaseConnection.GetAsync(query);
                foreach (DataRow globalHeaderDataRow in globalHeaders.Rows)
                {
                    headerRegexCheck = globalHeaderDataRow.Field<string>("default_header_footer_regex");
                    if (!String.IsNullOrWhiteSpace(url) && !String.IsNullOrWhiteSpace(headerRegexCheck) && !Regex.IsMatch(url, headerRegexCheck))
                    {
                        continue;
                    }

                    headerTemplateId = globalHeaderDataRow.IsNull("template_id") ? 0 : globalHeaderDataRow.Field<int>("template_id");
                    template = await templatesService.GetTemplateAsync(headerTemplateId);
                    javascriptTemplates.AddRange(template.JavascriptTemplates);
                    cssTemplates.AddRange(template.CssTemplates);
                    logger.LogDebug($"Default header template loaded: '{headerTemplateId}'");
                    return template.Content;
                }
            }

            // Try system objects method.
            if (!Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("defaultheadertemplateid"), out headerTemplateId) || headerTemplateId <= 0)
            {
                return "";
            }

            headerRegexCheck = await objectsService.FindSystemObjectByDomainNameAsync("headerregexcheck");
            if (!String.IsNullOrWhiteSpace(url) && !String.IsNullOrWhiteSpace(headerRegexCheck) && !Regex.IsMatch(url, headerRegexCheck))
            {
                return "";
            }

            template = await templatesService.GetTemplateAsync(headerTemplateId);
            javascriptTemplates.AddRange(template.JavascriptTemplates);
            cssTemplates.AddRange(template.CssTemplates);
            logger.LogDebug($"Default header template loaded: '{headerTemplateId}'");
            return template.Content;
        }

        /// <inheritdoc />
        public async Task<string> GetGlobalFooter(string url, List<int> javascriptTemplates, List<int> cssTemplates)
        {
            int footerTemplateId;
            string headerRegexCheck;
            Template template;

            if (!gclSettings.UseLegacyWiser1TemplateModule)
            {
                var joinPart = "";
                var whereClause = new List<string>();
                if (gclSettings.Environment == Environments.Development)
                {
                    joinPart = $" JOIN (SELECT template_id, MAX(version) AS maxVersion FROM {WiserTableNames.WiserTemplate} GROUP BY template_id) AS maxVersion ON template.template_id = maxVersion.template_id AND template.version = maxVersion.maxVersion";
                }
                else
                {
                    whereClause.Add($"(template.published_environment & {(int) gclSettings.Environment}) = {(int) gclSettings.Environment}");
                }

                whereClause.Add("template.template_type = 1");
                whereClause.Add("template.removed = 0");
                whereClause.Add("template.is_default_footer = 1");

                var query = $@"
                SELECT template.template_id, template.default_header_footer_regex
                FROM `{WiserTableNames.WiserTemplate}` AS template
                {joinPart}
                WHERE {String.Join(" AND ", whereClause)}
                GROUP BY template.template_id";

                var globalFooters = await databaseConnection.GetAsync(query);
                foreach (DataRow globalFooterDataRow in globalFooters.Rows)
                {
                    headerRegexCheck = globalFooterDataRow.Field<string>("default_header_footer_regex");
                    if (!String.IsNullOrWhiteSpace(url) && !String.IsNullOrWhiteSpace(headerRegexCheck) && !Regex.IsMatch(url, headerRegexCheck))
                    {
                        continue;
                    }

                    footerTemplateId = globalFooterDataRow.IsNull("template_id") ? 0 : globalFooterDataRow.Field<int>("template_id");
                    template = await templatesService.GetTemplateAsync(footerTemplateId);
                    javascriptTemplates.AddRange(template.JavascriptTemplates);
                    cssTemplates.AddRange(template.CssTemplates);
                    logger.LogDebug($"Default footer template loaded: '{footerTemplateId}'");
                    return template.Content;
                }
            }

            // Try system objects method.
            if (!Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("defaultfootertemplateid"), out footerTemplateId) || footerTemplateId <= 0)
            {
                return "";
            }

            headerRegexCheck = await objectsService.FindSystemObjectByDomainNameAsync("footerregexcheck");
            if (!String.IsNullOrWhiteSpace(url) && !String.IsNullOrWhiteSpace(headerRegexCheck) && !Regex.IsMatch(url, headerRegexCheck))
            {
                return "";
            }

            template = await templatesService.GetTemplateAsync(footerTemplateId);
            javascriptTemplates.AddRange(template.JavascriptTemplates);
            cssTemplates.AddRange(template.CssTemplates);
            logger.LogDebug($"Default footer template loaded: '{footerTemplateId}'");
            return template.Content;
        }

        /// <inheritdoc />
        public async Task<PageViewModel> CreatePageViewModelAsync(List<string> externalCss, List<int> cssTemplates, List<string> externalJavascript, List<int> javascriptTemplates, string bodyHtml, int templateId = 0)
        {
            var viewModel = new PageViewModel();

            // Add Google reCAPTCHAv3 if setup.
            await AddGoogleReCaptchaToViewModelAsync(viewModel);

            viewModel.Widgets = await templatesService.GetPageWidgetsAsync(templateId);

            // Add CSS for all pages.
            var generalStandardCss = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Css);
            externalCss.AddRange(generalStandardCss.ExternalFiles);
            if (!String.IsNullOrWhiteSpace(generalStandardCss.Content))
            {
                viewModel.Css.GeneralStandardCssFileName = $"/css/gcl_general.css?mode=Standard&c={generalStandardCss.LastChangeDate:yyyyMMddHHmmss}";
            }

            var generalInlineHeadCss = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Css, ResourceInsertModes.InlineHead);
            if (!String.IsNullOrWhiteSpace(generalInlineHeadCss.Content))
            {
                viewModel.Css.GeneralInlineHeadCss = generalInlineHeadCss.Content;
            }

            var generalSyncFooterCss = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Css, ResourceInsertModes.SyncFooter);
            if (!String.IsNullOrWhiteSpace(generalSyncFooterCss.Content))
            {
                viewModel.Css.GeneralSyncFooterCssFileName = $"/css/gcl_general.css?mode=SyncFooter&c={generalSyncFooterCss.LastChangeDate:yyyyMMddHHmmss}";
            }

            var generalAsyncFooterCss = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Css, ResourceInsertModes.AsyncFooter);
            if (!String.IsNullOrWhiteSpace(generalAsyncFooterCss.Content))
            {
                viewModel.Css.GeneralAsyncFooterCssFileName = $"/css/gcl_general.css?mode=AsyncFooter&c={generalAsyncFooterCss.LastChangeDate:yyyyMMddHHmmss}";
            }

            // Add css for this specific page.
            if (cssTemplates.Count > 0)
            {
                var standardCssTemplates = new List<int>();
                var inlineHeadCssTemplates = new List<string>();
                var syncFooterCssTemplates = new List<int>();
                var asyncFooterCssTemplates = new List<int>();

                var templates = (await templatesService.GetTemplatesAsync(cssTemplates, true)).Where(t => t.Type == TemplateTypes.Css).ToList();
                foreach (var template in templates)
                {
                    externalCss.AddRange(template.ExternalFiles);

                    if (String.IsNullOrWhiteSpace(template.Content))
                    {
                        continue;
                    }

                    switch (template.InsertMode)
                    {
                        case ResourceInsertModes.Standard:
                            standardCssTemplates.Add(template.Id);
                            break;
                        case ResourceInsertModes.InlineHead:
                            inlineHeadCssTemplates.Add(template.Content);
                            break;
                        case ResourceInsertModes.AsyncFooter:
                            asyncFooterCssTemplates.Add(template.Id);
                            break;
                        case ResourceInsertModes.SyncFooter:
                            syncFooterCssTemplates.Add(template.Id);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                var lastChanged = !templates.Any() ? DateTime.Now : templates.Max(t => t.LastChanged);
                var standardSuffix = $"c={lastChanged:yyyyMMddHHmmss}";

                if (standardCssTemplates.Any())
                {
                    viewModel.Css.PageStandardCssFileName = $"/css/gclcss_{String.Join("_", standardCssTemplates)}.css?mode=Standard&{standardSuffix}";
                }

                if (inlineHeadCssTemplates.Any())
                {
                    viewModel.Css.PageInlineHeadCss = String.Join(Environment.NewLine, inlineHeadCssTemplates);
                }

                if (asyncFooterCssTemplates.Any())
                {
                    viewModel.Css.PageAsyncFooterCssFileName = $"/css/gclcss_{String.Join("_", asyncFooterCssTemplates)}.css?mode=AsyncFooter&{standardSuffix}";
                }

                if (syncFooterCssTemplates.Any())
                {
                    viewModel.Css.PageSyncFooterCssFileName = $"/css/gclcss_{String.Join("_", asyncFooterCssTemplates)}.css?mode=SyncFooter&{standardSuffix}";
                }
            }

            // Add JavaScript for all pages.
            var moveAllJavaScriptToBottom = (await objectsService.FindSystemObjectByDomainNameAsync("javascriptmovetobottom", "false")).Equals("true", StringComparison.OrdinalIgnoreCase);
            var generalStandardJavaScript = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Js);
            externalJavascript.AddRange(generalStandardJavaScript.ExternalFiles);
            if (!String.IsNullOrWhiteSpace(generalStandardJavaScript.Content))
            {
                if (moveAllJavaScriptToBottom)
                {
                    viewModel.Javascript.GeneralSyncFooterJavaScriptFileName ??= new List<string>();
                    viewModel.Javascript.GeneralSyncFooterJavaScriptFileName.Add($"/scripts/gcl_general.js?mode=Standard&c={generalStandardJavaScript.LastChangeDate:yyyyMMddHHmmss}");
                }
                else
                {
                    viewModel.Javascript.GeneralStandardJavaScriptFileName = $"/scripts/gcl_general.js?mode=Standard&c={generalStandardJavaScript.LastChangeDate:yyyyMMddHHmmss}";
                }
            }

            var generalInlineHeadJavaScript = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Js, ResourceInsertModes.InlineHead);
            if (!String.IsNullOrWhiteSpace(generalInlineHeadJavaScript.Content))
            {
                viewModel.Javascript.GeneralInlineHeadJavaScript = generalInlineHeadJavaScript.Content;
            }

            var generalSyncFooterJavaScript = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Js, ResourceInsertModes.SyncFooter);
            if (!String.IsNullOrWhiteSpace(generalSyncFooterJavaScript.Content))
            {
                viewModel.Javascript.GeneralSyncFooterJavaScriptFileName ??= new List<string>();
                viewModel.Javascript.GeneralSyncFooterJavaScriptFileName.Add($"/scripts/gcl_general.js?mode=SyncFooter&c={generalSyncFooterJavaScript.LastChangeDate:yyyyMMddHHmmss}");
            }

            var generalAsyncFooterJavaScript = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Js, ResourceInsertModes.AsyncFooter);
            if (!String.IsNullOrWhiteSpace(generalAsyncFooterJavaScript.Content))
            {
                viewModel.Javascript.GeneralAsyncFooterJavaScriptFileName = $"/scripts/gcl_general.js?mode=AsyncFooter&c={generalAsyncFooterJavaScript.LastChangeDate:yyyyMMddHHmmss}";
            }

            // Add Javascript for this specific page.
            if (javascriptTemplates.Count > 0)
            {
                var standardJavascriptTemplates = new List<int>();
                var inlineHeadJavascriptTemplates = new List<string>();
                var syncFooterJavascriptTemplates = new List<int>();
                var asyncFooterJavascriptTemplates = new List<int>();

                var templates = (await templatesService.GetTemplatesAsync(javascriptTemplates, true)).Where(t => t.Type == TemplateTypes.Js).ToList();
                foreach (var template in templates)
                {
                    externalJavascript.AddRange(template.ExternalFiles);

                    if (String.IsNullOrWhiteSpace(template.Content))
                    {
                        continue;
                    }

                    switch (template.InsertMode)
                    {
                        case ResourceInsertModes.Standard:
                            if (moveAllJavaScriptToBottom)
                            {
                                syncFooterJavascriptTemplates.Add(template.Id);
                            }
                            else
                            {
                                standardJavascriptTemplates.Add(template.Id);
                            }

                            break;
                        case ResourceInsertModes.InlineHead:
                            inlineHeadJavascriptTemplates.Add(template.Content);
                            break;
                        case ResourceInsertModes.AsyncFooter:
                            asyncFooterJavascriptTemplates.Add(template.Id);
                            break;
                        case ResourceInsertModes.SyncFooter:
                            syncFooterJavascriptTemplates.Add(template.Id);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                var lastChanged = !templates.Any() ? DateTime.Now : templates.Max(t => t.LastChanged);
                var standardSuffix = $"c={lastChanged:yyyyMMddHHmmss}";

                if (standardJavascriptTemplates.Any())
                {
                    viewModel.Javascript.PageStandardJavascriptFileName = $"/scripts/gcljs_{String.Join("_", standardJavascriptTemplates)}.js?mode=Standard&{standardSuffix}";
                }

                if (inlineHeadJavascriptTemplates.Any())
                {
                    viewModel.Javascript.PageInlineHeadJavascript ??= new List<string>();
                    viewModel.Javascript.PageInlineHeadJavascript.AddRange(inlineHeadJavascriptTemplates);
                }

                if (asyncFooterJavascriptTemplates.Any())
                {
                    viewModel.Javascript.PageAsyncFooterJavascriptFileName = $"/scripts/gcljs_{String.Join("_", asyncFooterJavascriptTemplates)}.js?mode=AsyncFooter&{standardSuffix}";
                }

                if (syncFooterJavascriptTemplates.Any())
                {
                    viewModel.Javascript.PageSyncFooterJavascriptFileName = $"/scripts/gcljs_{String.Join("_", syncFooterJavascriptTemplates)}.js?mode=SyncFooter&{standardSuffix}";
                }
            }

            // Get SEO data and replace the body with data from seo module if applicable.
            if (await seoService.SeoModuleIsEnabledAsync())
            {
                viewModel.MetaData = await seoService.GetSeoDataForPageAsync(HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext));

                if (bodyHtml.Contains("[{seomodule_", StringComparison.OrdinalIgnoreCase))
                {
                    if (String.IsNullOrWhiteSpace(viewModel.MetaData?.SeoText))
                    {
                        bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_content}\|(.*?)\]", "$1");
                    }
                    else
                    {
                        bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_content}\|(.*?)\]", viewModel.MetaData.SeoText);
                        bodyHtml = bodyHtml.ReplaceCaseInsensitive("[{seomodule_content}]", viewModel.MetaData.SeoText);
                    }

                    if (String.IsNullOrWhiteSpace(viewModel.MetaData?.H1Text))
                    {
                        bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_h1header}\|(.*?)\]", "$1");
                    }
                    else
                    {
                        bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_h1header}\|(.*?)\]", viewModel.MetaData.H1Text);
                        bodyHtml = bodyHtml.ReplaceCaseInsensitive("[{seomodule_h1header}]", viewModel.MetaData.H1Text);
                    }

                    if (String.IsNullOrWhiteSpace(viewModel.MetaData?.H2Text))
                    {
                        bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_h2header}\|(.*?)\]", "$1");
                    }
                    else
                    {
                        bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_h2header}\|(.*?)\]", viewModel.MetaData.H2Text);
                        bodyHtml = bodyHtml.ReplaceCaseInsensitive("[{seomodule_h2header}]", viewModel.MetaData.H2Text);
                    }

                    if (String.IsNullOrWhiteSpace(viewModel.MetaData?.H3Text))
                    {
                        bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_h3header}\|(.*?)\]", "$1");
                    }
                    else
                    {
                        bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_h3header}\|(.*?)\]", viewModel.MetaData.H3Text);
                        bodyHtml = bodyHtml.ReplaceCaseInsensitive("[{seomodule_h3header}]", viewModel.MetaData.H3Text);
                    }
                }
            }

            // Handle any left over seo module things.
            if (bodyHtml.Contains("[{seomodule_"))
            {
                bodyHtml = bodyHtml.ReplaceCaseInsensitive("[{seomodule_content}]", "");
                bodyHtml = Regex.Replace(bodyHtml, @"\[{seomodule_.*?}\|(.*?)\]", "$1");
            }

            // Check if some component is adding external JavaScript libraries to the page.
            var externalScripts = externalJavascript.Select(ej => new JavaScriptResource { Uri = new Uri(ej) }).ToList();
            if (httpContextAccessor.HttpContext?.Items[CmsSettings.ExternalJavaScriptLibrariesFromComponentKey] is List<JavaScriptResource> componentExternalJavaScriptLibraries)
            {
                foreach (var externalLibrary in componentExternalJavaScriptLibraries.Where(externalLibrary => !externalScripts.Any(l => l.Uri.AbsoluteUri.Equals(externalLibrary.Uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))))
                {
                    externalScripts.Add(externalLibrary);
                }
            }

            viewModel.Css.ExternalCss.AddRange(externalCss);
            viewModel.Javascript.ExternalJavascript.AddRange(externalScripts);
            viewModel.Body = bodyHtml;

            // Add viewport.
            var viewportSystemObjectValue = await objectsService.FindSystemObjectByDomainNameAsync("metatag_viewport", "false");
            var viewportValueIsBoolean = Boolean.TryParse(viewportSystemObjectValue, out var viewportBooleanValue);
            var viewportValueIsInt = Int32.TryParse(viewportSystemObjectValue, out var viewportIntValue);

            if (viewportValueIsBoolean || viewportValueIsInt)
            {
                // Value is either a boolean value or integer value.
                if (viewportBooleanValue || viewportIntValue > 0)
                {
                    viewModel.MetaData.MetaTags["viewport"] = "height=device-height,width=device-width,initial-scale=1.0,maximum-scale=5.0";
                }
            }
            else if (!String.IsNullOrWhiteSpace(viewportSystemObjectValue))
            {
                // Viewport is a custom string; use the value of the system object.
                viewModel.MetaData.MetaTags["viewport"] = viewportSystemObjectValue;
            }

            // Check if some component is adding SEO data to the page.
            if (httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] is PageMetaDataModel componentSeoData)
            {
                if (componentSeoData.MetaTags != null && componentSeoData.MetaTags.Any())
                {
                    foreach (var (key, value) in componentSeoData.MetaTags.Where(metaTag => !viewModel.MetaData.MetaTags.ContainsKey(metaTag.Key) || String.IsNullOrWhiteSpace(viewModel.MetaData.MetaTags[metaTag.Key])))
                    {
                        viewModel.MetaData.MetaTags[key] = value;
                    }
                }

                if (componentSeoData.OpenGraphMetaTags != null && componentSeoData.OpenGraphMetaTags.Any())
                {
                    foreach (var (key, value) in componentSeoData.OpenGraphMetaTags.Where(metaTag => !viewModel.MetaData.OpenGraphMetaTags.ContainsKey(metaTag.Key) || String.IsNullOrWhiteSpace(viewModel.MetaData.MetaTags[metaTag.Key])))
                    {
                        viewModel.MetaData.OpenGraphMetaTags[key] = value;
                    }
                }

                if (!String.IsNullOrWhiteSpace(componentSeoData.PageTitle) && String.IsNullOrWhiteSpace(viewModel.MetaData.PageTitle))
                {
                    viewModel.MetaData.PageTitle = componentSeoData.PageTitle;
                }

                if (!String.IsNullOrWhiteSpace(componentSeoData.Canonical) && String.IsNullOrWhiteSpace(viewModel.MetaData.Canonical))
                {
                    viewModel.MetaData.Canonical = componentSeoData.Canonical;
                }

                if (!String.IsNullOrWhiteSpace(componentSeoData.H1Text) && String.IsNullOrWhiteSpace(viewModel.MetaData.H1Text))
                {
                    viewModel.MetaData.H1Text = componentSeoData.H1Text;
                }

                if (!String.IsNullOrWhiteSpace(componentSeoData.H2Text) && String.IsNullOrWhiteSpace(viewModel.MetaData.H2Text))
                {
                    viewModel.MetaData.H2Text = componentSeoData.H2Text;
                }

                if (!String.IsNullOrWhiteSpace(componentSeoData.H3Text) && String.IsNullOrWhiteSpace(viewModel.MetaData.H3Text))
                {
                    viewModel.MetaData.H3Text = componentSeoData.H3Text;
                }

                if (!String.IsNullOrWhiteSpace(componentSeoData.SeoText) && String.IsNullOrWhiteSpace(viewModel.MetaData.SeoText))
                {
                    viewModel.MetaData.SeoText = componentSeoData.SeoText;
                }

                if (!String.IsNullOrWhiteSpace(componentSeoData.PreviousPageLink) && String.IsNullOrWhiteSpace(viewModel.MetaData.PreviousPageLink))
                {
                    viewModel.MetaData.PreviousPageLink = componentSeoData.PreviousPageLink;
                }

                if (!String.IsNullOrWhiteSpace(componentSeoData.NextPageLink) && String.IsNullOrWhiteSpace(viewModel.MetaData.NextPageLink))
                {
                    viewModel.MetaData.NextPageLink = componentSeoData.NextPageLink;
                }
            }

            // See if we need to add canonical to self, but only if no other canonical has been added yet.
            if (String.IsNullOrWhiteSpace(viewModel.MetaData.Canonical))
            {
                var canonicalSetting = await objectsService.FindSystemObjectByDomainNameAsync("always_add_canonical_to_self");
                if (canonicalSetting.Equals("true", StringComparison.OrdinalIgnoreCase) || canonicalSetting.Equals("1", StringComparison.Ordinal))
                {
                    var canonicalUrl = HttpContextHelpers.GetOriginalRequestUriBuilder(httpContextAccessor.HttpContext);
                    var parametersToIncludeForCanonical = (await objectsService.FindSystemObjectByDomainNameAsync("include_parameters_canonical")).Split(",", StringSplitOptions.RemoveEmptyEntries);

                    if (!parametersToIncludeForCanonical.Any())
                    {
                        canonicalUrl.Query = "";
                    }
                    else
                    {
                        // Remove the query string from the canonical, except for keys that have been set in the settings.
                        var queryString = HttpUtility.ParseQueryString(canonicalUrl.Query);
                        var queryStringsToRemove = queryString.AllKeys.Where(k => !parametersToIncludeForCanonical.Any(p => p.Equals(k)));
                        foreach (var key in queryStringsToRemove)
                        {
                            queryString.Remove(key);
                        }

                        canonicalUrl.Query = queryString.ToString();

                        // If the current URL does not exist in the SEO module, check if we need to strip a part of it before adding it as a canonical.
                        var canonicalPathEnd = (await objectsService.FindSystemObjectByDomainNameAsync("canonical_path_end")).Split(",", StringSplitOptions.RemoveEmptyEntries);
                        foreach (var urlValue in canonicalPathEnd)
                        {
                            var index = canonicalUrl.Path.IndexOf(urlValue, StringComparison.OrdinalIgnoreCase);
                            if (index == -1)
                            {
                                continue;
                            }

                            // Strip the value of canonicalPathEnd and everything after that from the current URL, that will be the new canonical URL.
                            canonicalUrl.Path = canonicalUrl.Path.Substring(0, index);

                            if (!canonicalUrl.Path.EndsWith("/") && await redirectService.ShouldRedirectToUrlWithTrailingSlashAsync())
                            {
                                canonicalUrl.Path += "/";
                            }

                            // Only do this for the first occurrence.
                            break;
                        }
                    }

                    // Call the Uri's ToString method instead of the UriBuilder's ToString, otherwise default ports will
                    // be added (like 443 for https and 80 for http).
                    viewModel.MetaData.Canonical = canonicalUrl.Uri.ToString();
                }
            }

            // Check if there is a global meta title suffix set and add it to the final page title.
            var globalPageTitleSuffix = await objectsService.FindSystemObjectByDomainNameAsync("global_meta_title_suffix");
            if (String.IsNullOrWhiteSpace(viewModel.MetaData.PageTitle) || (!String.IsNullOrWhiteSpace(globalPageTitleSuffix) && !viewModel.MetaData.PageTitle.EndsWith(globalPageTitleSuffix, StringComparison.OrdinalIgnoreCase)))
            {
                viewModel.MetaData.GlobalPageTitleSuffix = globalPageTitleSuffix;
            }

            // Load all Google Analytics related stuff.
            await AddGoogleAnalyticsToViewModelAsync(viewModel);

            // Check for additional plugins to load (like Wiser Search, Zopim, etc.).
            await AddPluginScriptsAsync(viewModel);

            return viewModel;
        }

        /// <inheritdoc />
        public void SetPageSeoData(string seoTitle = null, string seoDescription = null, string seoKeyWords = null, string seoCanonical = null, bool noIndex = false, bool noFollow = false, IEnumerable<string> robots = null, string previousPageLink = null, string nextPageLink = null)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(seoTitle) && String.IsNullOrWhiteSpace(seoDescription) && String.IsNullOrWhiteSpace(seoKeyWords) && String.IsNullOrWhiteSpace(seoCanonical) && !noIndex && !noFollow && robots == null && String.IsNullOrWhiteSpace(previousPageLink) && String.IsNullOrWhiteSpace(nextPageLink))
            {
                return;
            }

            var componentSeoData = httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] as PageMetaDataModel ?? new PageMetaDataModel();
            if (!String.IsNullOrWhiteSpace(seoTitle) && !componentSeoData.MetaTags.ContainsKey("title"))
            {
                componentSeoData.PageTitle = seoTitle;
                componentSeoData.MetaTags.Add("title", seoTitle);
            }

            if (!String.IsNullOrWhiteSpace(seoDescription) && !componentSeoData.MetaTags.ContainsKey("description"))
            {
                componentSeoData.MetaTags.Add("description", seoDescription);
            }

            if (!String.IsNullOrWhiteSpace(seoKeyWords) && !componentSeoData.MetaTags.ContainsKey("keywords"))
            {
                componentSeoData.MetaTags.Add("keywords", seoKeyWords);
            }

            if (!String.IsNullOrWhiteSpace(seoCanonical) && String.IsNullOrWhiteSpace(componentSeoData.Canonical))
            {
                componentSeoData.Canonical = seoCanonical;
            }

            if (!componentSeoData.MetaTags.ContainsKey("robots"))
            {
                var allRobots = new List<string>();
                if (robots != null)
                {
                    allRobots.AddRange(robots);
                }

                if (noIndex && !allRobots.Any(s => s.Equals("noindex", StringComparison.OrdinalIgnoreCase)))
                {
                    allRobots.Add("noindex");
                }

                if (noFollow && !allRobots.Any(s => s.Equals("nofollow", StringComparison.OrdinalIgnoreCase)))
                {
                    allRobots.Add("nofollow");
                }

                if (allRobots.Any())
                {
                    componentSeoData.MetaTags.Add("robots", String.Join(",", allRobots));
                }
            }

            if (!String.IsNullOrWhiteSpace(previousPageLink) && String.IsNullOrWhiteSpace(componentSeoData.PreviousPageLink))
            {
                componentSeoData.PreviousPageLink = previousPageLink;
            }

            if (!String.IsNullOrWhiteSpace(nextPageLink) && String.IsNullOrWhiteSpace(componentSeoData.NextPageLink))
            {
                componentSeoData.NextPageLink = nextPageLink;
            }

            httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] = componentSeoData;
        }

        /// <inheritdoc />
        public void SetOpenGraphData(IDictionary<string, string> openGraphValues)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return;
            }

            if (openGraphValues == null || openGraphValues.Count == 0)
            {
                return;
            }

            // Some keys are preserved (in other words, the underscores in these keys shouldn't be replaced with colons).
            var preservedKeys = new[] { "site_name", "secure_url", "release_date", "published_time", "modified_time", "expiration_time", "first_name", "last_name" };
            var componentSeoData = httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] as PageMetaDataModel ?? new PageMetaDataModel();
            foreach (var openGraphItem in openGraphValues)
            {
                if (!openGraphItem.Key.StartsWith("opengraph_", StringComparison.OrdinalIgnoreCase)) continue;

                // Strip the "opengraph_" part.
                var key = openGraphItem.Key[10..];

                // Make sure the values from the preservedNames
                key = preservedKeys.Aggregate(key, (current, preservedName) => current.Replace(preservedName, preservedName.Replace("_", "~~SEP~~")));
                // Replace underscores with colons, and then replace the "~~SEP~~" instances back to underscores.
                key = key.Replace("_", ":").Replace("~~SEP~~", "_");

                componentSeoData.OpenGraphMetaTags.Add(key, openGraphItem.Value);
            }

            httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] = componentSeoData;
        }

        /// <summary>
        /// Sets various Google reCAPTCHAv3 scripts based on the customer's settings.
        /// </summary>
        /// <param name="viewModel">The <see cref="PageViewModel"/> that will be updated.</param>
        private async Task AddGoogleReCaptchaToViewModelAsync(PageViewModel viewModel)
        {
            var reCaptchaSiteKey = await objectsService.FindSystemObjectByDomainNameAsync("google_recaptcha_v3_sitekey");
            var reCaptchaSecretKey = await objectsService.FindSystemObjectByDomainNameAsync("google_recaptcha_v3_secretkey");
            if (String.IsNullOrWhiteSpace(reCaptchaSiteKey) || String.IsNullOrWhiteSpace(reCaptchaSecretKey))
            {
                return;
            }

            viewModel.Javascript.ExternalJavascript.Add(new JavaScriptResource
            {
                Uri = new Uri($"https://www.google.com/recaptcha/api.js?render={reCaptchaSiteKey}"),
                Async = true,
                Defer = true
            });

            viewModel.Javascript.PageInlineHeadJavascript ??= new List<string>();
            viewModel.Javascript.PageInlineHeadJavascript.Add($@"function gclExecuteReCaptcha(action) {{
	return new Promise((resolve, reject) => {{
		if (!grecaptcha || !grecaptcha.ready) {{
			reject(""grecaptcha not defined!"");
			return;
		}}

		grecaptcha.ready(() => {{
			try {{
				grecaptcha.execute(""{reCaptchaSiteKey}"", {{action: action}}).then((token) => {{
					resolve(token);
				}}).catch((error) => {{
					reject(error);
				}});
			}} catch (exception) {{
				reject(exception);
			}}
		}});
	}});
}}

gclExecuteReCaptcha(""Page_load"")");
        }

        /// <summary>
        /// Sets various Google Analytics tracking scripts based on the customer's settings.
        /// </summary>
        /// <param name="viewModel">The <see cref="PageViewModel"/> that will be updated.</param>
        private async Task AddGoogleAnalyticsToViewModelAsync(PageViewModel viewModel)
        {
            var inlineHeadJavaScript = new StringBuilder();
            var inlineBodyNoScript = new StringBuilder();

            // Universal Analytics (Google Analytics 3).
            var universalAnalyticsCode = await objectsService.FindSystemObjectByDomainNameAsync("GoAnCode");
            var universalAnalyticsEnabled = !String.IsNullOrWhiteSpace(universalAnalyticsCode);
            if (universalAnalyticsEnabled)
            {
                inlineHeadJavaScript.AppendLine("(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){");
                inlineHeadJavaScript.AppendLine("(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),");
                inlineHeadJavaScript.AppendLine("m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)");
                inlineHeadJavaScript.AppendLine("})(window,document,'script','https://www.google-analytics.com/analytics.js','ga');");
                inlineHeadJavaScript.AppendLine();
                inlineHeadJavaScript.AppendLine($"ga('create', '{universalAnalyticsCode}', 'auto');");
            }

            // Google Analytics 4.
            var googleAnalytics4Code = await objectsService.FindSystemObjectByDomainNameAsync("GoAn4Code");
            var googleAnalytics4Enabled = !String.IsNullOrWhiteSpace(googleAnalytics4Code);
            if (googleAnalytics4Enabled)
            {
                viewModel.GoogleAnalytics.HeadJavaScriptResources.Add(new JavaScriptResource
                {
                    Uri = new Uri($"https://www.googletagmanager.com/gtag/js?id={googleAnalytics4Code}"),
                    Async = true
                });

                inlineHeadJavaScript.AppendLine("window.dataLayer = window.dataLayer || [];");
                inlineHeadJavaScript.AppendLine("function gtag(){dataLayer.push(arguments);}");
                inlineHeadJavaScript.AppendLine("gtag('js', new Date());");
                inlineHeadJavaScript.AppendLine();
                inlineHeadJavaScript.AppendLine($"gtag('config', '{googleAnalytics4Code}');");
            }

            // Google Tag Manager.
            var googleTagManagerCode = await objectsService.FindSystemObjectByDomainNameAsync("GoAnTagManagerId");
            var googleTagManagerEnabled = !String.IsNullOrWhiteSpace(googleTagManagerCode);
            if (googleTagManagerEnabled)
            {
                inlineHeadJavaScript.AppendLine("(function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':");
                inlineHeadJavaScript.AppendLine("new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],");
                inlineHeadJavaScript.AppendLine("j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=");
                inlineHeadJavaScript.AppendLine("'https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);");
                inlineHeadJavaScript.AppendLine($"}})(window,document,'script','dataLayer','{googleTagManagerCode}');");

                inlineBodyNoScript.AppendLine($"<iframe src=\"https://www.googletagmanager.com/ns.html?id={googleTagManagerCode}\" height=\"0\" width=\"0\" style=\"display:none;visibility:hidden\"></iframe>");
            }

            // Additional settings and plugins for Universal Analytics and send page view.
            if (universalAnalyticsEnabled)
            {
                var useDisplayFeatures = (await objectsService.FindSystemObjectByDomainNameAsync("GoAnDisplayFeatures")).InList(StringComparer.OrdinalIgnoreCase, "true", "1");
                if (useDisplayFeatures)
                {
                    inlineHeadJavaScript.AppendLine("ga('require', 'displayfeatures');");
                }

                var useEnhancedLinkAttribution = (await objectsService.FindSystemObjectByDomainNameAsync("GoAnUseLinkID")).InList(StringComparer.OrdinalIgnoreCase, "true", "1");
                if (useEnhancedLinkAttribution)
                {
                    inlineHeadJavaScript.AppendLine("ga('require', 'linkid');");
                }

                var anonymizeIp = (await objectsService.FindSystemObjectByDomainNameAsync("GoAnAnonymizeIp")).InList(StringComparer.OrdinalIgnoreCase, "true", "1");
                if (anonymizeIp)
                {
                    inlineHeadJavaScript.AppendLine("ga('set', 'anonymizeIp', true);");
                }

                // Page view is sent last because the plugins and settings can influence how the page view works.
                inlineHeadJavaScript.AppendLine("ga('send', 'pageview');");
            }

            // Set the scripts to their respective properties.
            if (inlineHeadJavaScript.Length > 0)
            {
                viewModel.GoogleAnalytics.InlineHeadJavaScript = inlineHeadJavaScript.ToString();
            }
            if (inlineBodyNoScript.Length > 0)
            {
                viewModel.GoogleAnalytics.InlineBodyNoScript = inlineBodyNoScript.ToString();
            }
        }

        /// <summary>
        /// Adds plugin scripts to the view model's Javascript settings.
        /// </summary>
        /// <param name="viewModel">The <see cref="PageViewModel"/> that will be updated.</param>
        private async Task AddPluginScriptsAsync(PageViewModel viewModel)
        {
            // TrackJS must be added to the head, otherwise it might start tracking errors too late.
            var trackJsEnabled = (await objectsService.FindSystemObjectByDomainNameAsync("TrackJSEnable", "false")).Equals("true", StringComparison.OrdinalIgnoreCase);
            if (trackJsEnabled)
            {
                var trackJsToken = await objectsService.FindSystemObjectByDomainNameAsync("TrackJSToken");
                if (!String.IsNullOrWhiteSpace(trackJsToken))
                {
                    viewModel.Javascript.ExternalJavascript.Add(new JavaScriptResource
                    {
                        Uri = new Uri("https://cdn.trackjs.com/agent/v3/latest/t.js")
                    });

                    if (!Decimal.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("TrackJSRequestsPercentage"), out var requestsPercentage) || requestsPercentage <= 0)
                    {
                        requestsPercentage = 0;
                    }

                    var enabledPart = requestsPercentage <= 0 ? "true" : $"Math.random() <= {(requestsPercentage / 100).ToString("F2", CultureInfo.InvariantCulture)}";

                    viewModel.Javascript.PageInlineHeadJavascript ??= new List<string>();
                    viewModel.Javascript.PageInlineHeadJavascript.Add($"window.TrackJS && TrackJS.install({{ token: \"{trackJsToken}\", enabled: {enabledPart} }});");
                }
            }

            var wiserSearchScript = await objectsService.FindSystemObjectByDomainNameAsync("WiserSearchScript");
            if (!String.IsNullOrWhiteSpace(wiserSearchScript))
            {
                if (wiserSearchScript.StartsWith("<script", StringComparison.OrdinalIgnoreCase))
                {
                    wiserSearchScript = Regex.Replace(wiserSearchScript, "^<script.*?>(?<script>.*?)</script>", "${script}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }

                viewModel.Javascript.PagePluginInlineJavascriptSnippets.Add(wiserSearchScript);
            }
        }
    }
}
