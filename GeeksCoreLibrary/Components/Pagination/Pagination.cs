using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Filter.Interfaces;
using GeeksCoreLibrary.Components.Pagination.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Components.Pagination
{
    public class Pagination : CmsComponent<PaginationCmsSettingsModel, Pagination.ComponentModes>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IFiltersService filtersService;

        #region Enums

        public enum ComponentModes
        {
            /// <summary>
            /// For generating and rendering the pagination HTML.
            /// </summary>
            Normal,

            /// <summary>
            /// For JCL pagination components that should run with the GCL code.
            /// </summary>
            [CmsEnum(HideInCms = true)]
            Legacy
        }

        #endregion

        #region Private fields

        private uint totalItemCount;

        #endregion

        #region Constructor

        public Pagination(ILogger<Pagination> logger, IStringReplacementsService stringReplacementsService, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IAccountsService accountsService, IHttpContextAccessor httpContextAccessor, IFiltersService filtersService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.filtersService = filtersService;

            Logger = logger;
            StringReplacementsService = stringReplacementsService;
            DatabaseConnection = databaseConnection;
            TemplatesService = templatesService;
            AccountsService = accountsService;

            Settings = new PaginationCmsSettingsModel();
        }

        #endregion

        #region Handling settings

        /// <inheritdoc />
        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            if (String.IsNullOrWhiteSpace(settingsJson))
            {
                return;
            }

            if (Settings.ComponentMode == ComponentModes.Legacy)
            {
                Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<PaginationLegacySettingsModel>(settingsJson)?.ToSettingsModel();
                // Parsing the settings will set the component mode to Normal, so force it back to Legacy here.
                if (Settings != null)
                {
                    Settings.ComponentMode = ComponentModes.Legacy;
                }
            }
            else
            {
                Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<PaginationCmsSettingsModel>(settingsJson);
                if (Settings != null && forcedComponentMode.HasValue)
                {
                    Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
                }
            }
        }

        /// <inheritdoc />
        public override string GetSettingsJson()
        {
            return Settings.ComponentMode == ComponentModes.Legacy
                ? Newtonsoft.Json.JsonConvert.SerializeObject(PaginationLegacySettingsModel.FromSettingsModel(Settings))
                : Newtonsoft.Json.JsonConvert.SerializeObject(Settings);
        }

        #endregion

        #region Rendering

        /// <inheritdoc />
        public override async Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData)
        {
            ComponentId = dynamicContent.Id;
            ExtraDataForReplacements = extraData;
            if (dynamicContent.Name == "JuiceControlLibrary.Pagination")
            {
                // Force component mode to Legacy mode if it was created through the JCL.
                Settings.ComponentMode = ComponentModes.Legacy;
            }
            ParseSettingsJson(dynamicContent.SettingsJson, forcedComponentMode);
            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
            else if (!String.IsNullOrWhiteSpace(dynamicContent.ComponentMode))
            {
                Settings.ComponentMode = Enum.Parse<ComponentModes>(dynamicContent.ComponentMode);
            }

            HandleDefaultSettingsFromComponentMode();

            // Check if we should actually render this component for the current user.
            var (renderHtml, debugInformation) = await ShouldRenderHtmlAsync();
            if (!renderHtml)
            {
                ViewBag.Html = debugInformation;
                return new HtmlString(debugInformation);
            }

            // Check if we need to call a specific method and then do so. Skip everything else, because we don't want to render the entire component then.
            if (!String.IsNullOrWhiteSpace(callMethod))
            {
                TempData["InvokeMethodResult"] = await InvokeMethodAsync(callMethod);
                return new HtmlString(String.Empty);
            }

            if (!String.IsNullOrWhiteSpace(Settings.DataQuery))
            {
                var parsedQuery = Settings.DataQuery;

                // Replace the {filters} variable by the joins from the filter component
                if (parsedQuery.Contains("{filters}", StringComparison.OrdinalIgnoreCase))
                {
                    parsedQuery = parsedQuery.Replace("{filters}", (await filtersService.GetFilterQueryPartAsync()).JoinPart);
                }
                if (parsedQuery.Contains("{filters(", StringComparison.OrdinalIgnoreCase))
                {
                    parsedQuery = Regex.Replace(parsedQuery, @"{filters\((.*?),(.*?)\)}", (await filtersService.GetFilterQueryPartAsync(productJoinPart: "$1", categoryJoinPart: "$2")).JoinPart);
                }

                // Perform replacements on the query.
                parsedQuery = await TemplatesService.DoReplacesAsync(parsedQuery, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables, forQuery: true);

                var getCountResult = await DatabaseConnection.GetAsync(parsedQuery);
                if (getCountResult.Rows.Count > 0)
                {
                    // Simply try to convert the first row's first column to a UInt32.
                    totalItemCount = Convert.ToUInt32(getCountResult.Rows[0][0]);
                }
            }

            var resultHtml = new StringBuilder();
            resultHtml.Append(await CreatePaginationHtml());
            return new HtmlString(resultHtml.ToString());
        }

        private async Task<string> CreatePaginationHtml()
        {
            var paginationHtml = new StringBuilder();

            if (!UInt32.TryParse(HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, Settings.PageNumberVariableName), out var currentPage))
            {
                currentPage = 1U;
            }

            var lastPageNumber = Convert.ToUInt32(Math.Ceiling(Convert.ToDecimal(totalItemCount) / Convert.ToDecimal(Settings.ItemsPerPage)));

            if (lastPageNumber <= 1 && !Settings.RenderForSinglePage)
            {
                return String.Empty;
            }

            // Add links to the first and previous page when not on the first page.
            if (currentPage > 1)
            {
                paginationHtml.Append(ParsePageTemplate(Settings.FirstPageTemplate, 1));
                paginationHtml.Append(ParsePageTemplate(Settings.PreviousPageTemplate, currentPage - 1));
            }

            var maxBeforeCurrent = Settings.MaxPagesBeforeCurrent;
            var maxAfterCurrent = Settings.MaxPagesAfterCurrent;

            if (Settings.CombineMaxBeforeAndAfter)
            {
                if (currentPage <= maxBeforeCurrent)
                {
                    maxAfterCurrent += (maxBeforeCurrent + 1) - currentPage;
                }
                else if (lastPageNumber - currentPage < maxAfterCurrent)
                {
                    maxBeforeCurrent += maxAfterCurrent - (lastPageNumber - currentPage);
                }

                if (currentPage == 1)
                {
                    // When on first page, add extras in case first and/or previous templates are filled.
                    if (!String.IsNullOrWhiteSpace(Settings.FirstPageTemplate))
                    {
                        maxAfterCurrent += 1;
                    }

                    if (!String.IsNullOrWhiteSpace(Settings.PreviousPageTemplate))
                    {
                        maxAfterCurrent += 1;
                    }
                }
                else if (currentPage == lastPageNumber)
                {
                    // When on last page, add extras in case last and/or next templates are filled.
                    if (!String.IsNullOrWhiteSpace(Settings.LastPageTemplate))
                    {
                        maxBeforeCurrent += 1;
                    }

                    if (!String.IsNullOrWhiteSpace(Settings.NextPageTemplate))
                    {
                        maxBeforeCurrent += 1;
                    }
                }
            }

            var from = 1U;
            var to = lastPageNumber;

            if (currentPage > maxBeforeCurrent && maxBeforeCurrent > 0)
            {
                from = currentPage - maxBeforeCurrent;
            }

            if (lastPageNumber - currentPage > maxAfterCurrent && maxAfterCurrent > 0)
            {
                to = currentPage + maxAfterCurrent;
            }

            if (to - from > Settings.MaxPages && Settings.MaxPages > 0)
            {
                to = Settings.MaxPages;
            }

            // Add the "dots" template when the option is enabled.
            if (to > 1 && Settings.AddDotsToFirstAndLast)
            {
                var additionalOffset = Settings.MinPagesAtStart > 0U ? Settings.MinPagesAtStart - 1U : 0U;

                // Only if the dots replace at least a certain amount of pages should the dots template be shown.
                if (currentPage >= maxBeforeCurrent + Settings.DotsOffset + additionalOffset)
                {
                    for (var i = 1U; i <= 1U + additionalOffset; i++)
                    {
                        paginationHtml.Append(ParsePageTemplate(Settings.PageTemplate, i));
                    }

                    paginationHtml.Append(Settings.DotsTemplate);
                }

                // Show the normal pages if the dots would take up as much space, not taking "maxBeforeCurrent" into account.
                if (currentPage - maxBeforeCurrent < Settings.DotsOffset + additionalOffset)
                {
                    for (var i = 1U; i < currentPage - maxBeforeCurrent; i++)
                    {
                        paginationHtml.Append(ParsePageTemplate(Settings.PageTemplate, i));
                    }
                }
            }

            for (var i = from; i <= to; i++)
            {
                if (i > 1)
                {
                    paginationHtml.Append(Settings.InBetweenTemplate);
                }

                paginationHtml.Append(ParsePageTemplate(i == currentPage ? Settings.CurrentPageTemplate : Settings.PageTemplate, i));
            }

            // Add dots at the end if enabled.
            if (to > 1 && Settings.AddDotsToFirstAndLast)
            {
                var additionalOffset = Settings.MinPagesAtEnd > 0U ? Settings.MinPagesAtEnd - 1U : 0U;
                var dotsOffsetEnd = lastPageNumber - Settings.DotsOffset - additionalOffset + 1;

                if (currentPage <= dotsOffsetEnd - maxAfterCurrent)
                {
                    paginationHtml.Append(Settings.DotsTemplate);
                    for (var i = lastPageNumber - additionalOffset; i <= lastPageNumber; i++)
                    {
                        paginationHtml.Append(ParsePageTemplate(Settings.PageTemplate, i));
                    }
                }

                if (currentPage + maxAfterCurrent > dotsOffsetEnd)
                {
                    for (var i = currentPage + maxAfterCurrent + 1; i <= lastPageNumber; i++)
                    {
                        paginationHtml.Append(ParsePageTemplate(Settings.PageTemplate, i));
                    }
                }
            }

            // Add links to the next and last page when not on the last page.
            if (currentPage < lastPageNumber)
            {
                paginationHtml.Append(ParsePageTemplate(Settings.NextPageTemplate, currentPage + 1));
                paginationHtml.Append(ParsePageTemplate(Settings.LastPageTemplate, lastPageNumber));
            }

            var replaceData = new Dictionary<string, string>
            {
                ["firstitemnr"] = (totalItemCount < (currentPage - 1) * Settings.ItemsPerPage + 1 ? totalItemCount : (currentPage - 1) * Settings.ItemsPerPage + 1).ToString(),
                ["lastitemnr"] = (totalItemCount > currentPage * Settings.ItemsPerPage ? currentPage * Settings.ItemsPerPage : totalItemCount).ToString(),
                ["totalitems"] = totalItemCount.ToString(),
                ["totalitemnrs"] = totalItemCount.ToString() // Legacy support.
            };
            var summaryHtml = StringReplacementsService.DoReplacements(Settings.SummaryTemplate, replaceData);

            // Create the complete HTML.
            var resultHtml = Settings.FullTemplate.Replace("{summary}", summaryHtml).Replace("{pagination}", paginationHtml.ToString());
            replaceData = new Dictionary<string, string>
            {
                ["pagenr"] = currentPage.ToString(),
                ["lastpagenr"] = lastPageNumber.ToString(),
                ["totalitems"] = totalItemCount.ToString()
            };
            resultHtml = StringReplacementsService.DoReplacements(resultHtml, replaceData);

            return await TemplatesService.DoReplacesAsync(resultHtml, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);
        }

        private string ParsePageTemplate(string template, uint pageNumber)
        {
            if (String.IsNullOrWhiteSpace(template))
            {
                return String.Empty;
            }

            var linkFormat = Settings.LinkFormat.Replace("{pnr}", pageNumber.ToString()).Replace("{variablename}", Settings.PageNumberVariableName);

            var originalQueryString = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext)?.Query;
            if (Settings.AddPageQueryStringToLinkFormat)
            {
                var queryStringBuilder = HttpUtility.ParseQueryString(originalQueryString ?? "");
                if (linkFormat.Contains("?", StringComparison.Ordinal))
                {
                    // Link format contains a query string; combine it with the request's query string.
                    var qsIndex = linkFormat.IndexOf("?", StringComparison.Ordinal);
                    var linkFormatQueryString = HttpUtility.ParseQueryString(linkFormat[(qsIndex + 1)..]);

                    foreach (var key in linkFormatQueryString.AllKeys)
                    {
                        queryStringBuilder.Set(key, linkFormatQueryString.Get(key));
                    }

                    // Strip the query string from the query string (it will be added again afterwards).
                    linkFormat = linkFormat[..qsIndex];
                }

                var newLinkFormat = new StringBuilder(linkFormat);
                newLinkFormat.Append('?').Append(queryStringBuilder);
                linkFormat = newLinkFormat.ToString();
            }

            if (Settings.RemoveFirstPageFromUrl && pageNumber == 1U)
            {
                // Remove the "&<PageNumberVariableName>=1" and "?<PageNumberVariableName>=1" parts from the link URL.
                // If this results in an empty string, replace the entire URL with the "original request URI".
                linkFormat = linkFormat.Replace($"&{Settings.PageNumberVariableName}=1", String.Empty);
                linkFormat = linkFormat.Replace($"?{Settings.PageNumberVariableName}=1", String.Empty);

                if (linkFormat == String.Empty)
                {
                    var originalRequestUri = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext);
                    var uriWithoutQueryString = originalRequestUri.GetLeftPart(UriPartial.Path);

                    // Rebuild the query string to make sure it doesn't contain the page number variable name.
                    var newQueryString = HttpUtility.ParseQueryString(originalRequestUri.Query);
                    newQueryString.Remove(Settings.PageNumberVariableName);

                    linkFormat = newQueryString.Count > 0 ? $"{uriWithoutQueryString}?{newQueryString}" : uriWithoutQueryString;
                }
            }

            return template.Replace("{link}", linkFormat).Replace("{pnr}", pageNumber.ToString());
        }

        #endregion
    }
}
