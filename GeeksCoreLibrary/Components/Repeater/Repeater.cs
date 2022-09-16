using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Filter.Interfaces;
using GeeksCoreLibrary.Components.Repeater.Interfaces;
using GeeksCoreLibrary.Components.Repeater.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Repeater
{
    [CmsObject(
        PrettyName = "Repeater",
        Description = "Read and parse repeated data",
        DeveloperRemarks = ""
    )]
    public class Repeater : CmsComponent<RepeaterCmsSettingsModel, Repeater.LegacyComponentMode>
    {
        private readonly IRepeatersService repeatersService;
        private readonly IFiltersService filtersService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IPagesService pagesService;

        #region Enums

        public enum ComponentModes
        {
            Repeater,
            Languages,
            OrderHistory,
            Favorites
        }

        public enum LegacyComponentMode
        {
            NonLegacy,
            SimpleMenu,
            MlSimpleMenu,
            ProductModule
        }

        public enum DataSource
        {
            Query,
            DataSelector,
            Xml,
            Csv,
            Json,
            Wiser2ParentLinks,
            DirectoryOutput
        }

        #endregion

        #region Private fields

        private List<ProductBannerModel> productBanners = new();

        #endregion

        #region Constructor

        public Repeater(ILogger<Repeater> logger, IRepeatersService repeatersService, IStringReplacementsService stringReplacementsService, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IAccountsService accountsService, IFiltersService filtersService, IHttpContextAccessor httpContextAccessor, IPagesService pagesService)
        {
            this.repeatersService = repeatersService;
            this.filtersService = filtersService;
            this.httpContextAccessor = httpContextAccessor;
            this.pagesService = pagesService;

            Logger = logger;
            StringReplacementsService = stringReplacementsService;
            DatabaseConnection = databaseConnection;
            TemplatesService = templatesService;
            AccountsService = accountsService;

            Settings = new RepeaterCmsSettingsModel();
        }

        #endregion

        #region Handling settings

        /// <inheritdoc />
        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            /*
             * If the component is in legacy mode, there is a conversion needed for the settings.
             * The component always works with the normal settings object but saves and stores from the legacy model if in legacy mode
             */
            Settings = LegacyMode switch
            {
                LegacyComponentMode.NonLegacy => JsonConvert.DeserializeObject<RepeaterCmsSettingsModel>(settingsJson),
                LegacyComponentMode.SimpleMenu => JsonConvert.DeserializeObject<SimpleMenuLegacySettingsModel>(settingsJson)?.ToSettingsModel(),
                LegacyComponentMode.MlSimpleMenu => JsonConvert.DeserializeObject<MlSimpleMenuLegacySettingsModel>(settingsJson)?.ToSettingsModel(),
                LegacyComponentMode.ProductModule => JsonConvert.DeserializeObject<ProductModuleLegacySettingsModel>(settingsJson)?.ToSettingsModel(),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
        }

        /// <inheritdoc />
        public override string GetSettingsJson()
        {
            /*
             * If the component is in legacy mode, there is a conversion needed for the settings.
             * The component always works with the normal settings object but saves and stores from the legacy model if in legacy mode
             */
            return LegacyMode switch
            {
                LegacyComponentMode.NonLegacy => JsonConvert.SerializeObject(Settings),
                LegacyComponentMode.SimpleMenu => JsonConvert.SerializeObject(new SimpleMenuLegacySettingsModel().FromSettingModel(Settings)),
                LegacyComponentMode.MlSimpleMenu => JsonConvert.SerializeObject(new MlSimpleMenuLegacySettingsModel().FromSettingModel(Settings)),
                LegacyComponentMode.ProductModule => JsonConvert.SerializeObject(new ProductModuleLegacySettingsModel().FromSettingModel(Settings)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// Converts the dynamic content name from easy_dynamiccontent (freefield1) to <see cref="LegacyComponentMode"/>.
        /// </summary>
        /// <param name="dynamicContentName">The value of freefield1 from easy_dynamiccontent.</param>
        /// <returns>The parsed <see cref="LegacyComponentMode"/>.</returns>
        public static LegacyComponentMode ParseComponentMode(string dynamicContentName)
        {
            return dynamicContentName switch
            {
                "Repeater" => LegacyComponentMode.NonLegacy,
                "JuiceControlLibrary.MLSimpleMenu" => LegacyComponentMode.MlSimpleMenu,
                "JuiceControlLibrary.SimpleMenu" => LegacyComponentMode.SimpleMenu,
                "JuiceControlLibrary.ProductModule" => LegacyComponentMode.ProductModule,
                _ => throw new ArgumentOutOfRangeException(nameof(dynamicContentName)),
            };
        }

        #endregion

        #region Rendering

        /// <inheritdoc />
        public override async Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData)
        {
            ComponentId = dynamicContent.Id;
            LegacyMode = ParseComponentMode(dynamicContent.Name);
            ExtraDataForReplacements = extraData;
            ParseSettingsJson(dynamicContent.SettingsJson, forcedComponentMode);

            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }

            HandleDefaultSettingsFromComponentMode();

            // Security check by Account Component:
            var (renderHtml, debugInformation) = await ShouldRenderHtmlAsync();
            if (!renderHtml)
            {
                return new HtmlString(debugInformation);
            }

            if (Settings.PlaceProductBanners)
            {
                productBanners = await repeatersService.GetProductBannersAsync();
            }

            var parsedData = await ParseDataAsync();
            var generatedHtml = new StringBuilder();
            var groupingKeys = Settings.GroupingTemplates.Keys;
            var baseTemplate = Settings.GroupingTemplates[groupingKeys[0]];

            // Replacement data for some generic values.
            var genericReplacements = new Dictionary<string, string>
            {
                { "volgnr", "1" },
                { "rowindex", "0" },
                { "resultcount", parsedData.Rows.Count.ToString() }
            };
            string templateHtml;

            // Append base header template, but only if there's data or if the "ShowHeaderAndFooterOnNoData" setting is enabled.
            if (parsedData.Rows.Count > 0 || Settings.ShowBaseHeaderAndFooterOnNoData)
            {
                templateHtml = baseTemplate.HeaderTemplate;
                templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                generatedHtml.Append(await StringReplacementsService.DoAllReplacementsAsync(templateHtml, parsedData.Rows.Count > 0 ? parsedData.Rows[0] : null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
            }

            if (parsedData.Rows.Count == 0)
            {
                // If we have no data, show the no data template or set 404
                if (Settings.Return404OnNoData)
                {
                    HttpContextHelpers.Return404(httpContextAccessor.HttpContext);
                }
                else
                {
                    generatedHtml.Append(baseTemplate.NoDataTemplate);
                }
            }
            else
            {
                // Add SEO data.
                if (Settings.SetSeoInformationFromFirstItem)
                {
                    var seoTitle = parsedData.Rows[0].GetValueIfColumnExists<string>("SEOtitle");
                    var seoDescription = parsedData.Rows[0].GetValueIfColumnExists<string>("SEOdescription");
                    var seoKeyWords = parsedData.Rows[0].GetValueIfColumnExists<string>("SEOkeywords");
                    var seoCanonical = parsedData.Rows[0].GetValueIfColumnExists<string>("SEOcanonical");
                    var noIndex = Convert.ToBoolean(parsedData.Rows[0].GetValueIfColumnExists("noindex"));
                    var noFollow = Convert.ToBoolean(parsedData.Rows[0].GetValueIfColumnExists("nofollow"));
                    var robots = parsedData.Rows[0].GetValueIfColumnExists<string>("SEOrobots");
                    pagesService.SetPageSeoData(seoTitle, seoDescription, seoKeyWords, seoCanonical, noIndex, noFollow, robots?.Split(",", StringSplitOptions.RemoveEmptyEntries));
                }

                // Add Open Graph data.
                if (Settings.SetOpenGraphInformationFromFirstItem)
                {
                    var openGraphValues = parsedData.Columns.Cast<DataColumn>().Where(c => c.ColumnName.StartsWith("opengraph_", StringComparison.OrdinalIgnoreCase)).ToDictionary(c => c.ColumnName, c => Convert.ToString(parsedData.Rows[0][c]));
                    pagesService.SetOpenGraphData(openGraphValues);
                }

                if (Settings.GroupingTemplates.Keys.Count == 1)
                {
                    // If we have only one layer, generate the HTML for that. It works differently for single layer repeater than for multiple layers, that's why it's a different function.
                    // Single layer repeaters also have support for product banners and item groups, multi layer repeaters don't.
                    generatedHtml.Append(await ParseSingleLayerDataAsync(parsedData, Settings.GroupingTemplates[Settings.GroupingTemplates.Keys.First()]));
                }
                else
                {
                    // Parse multi layered data.
                    generatedHtml.Append(await ParseMultiLayerDataAsync(parsedData, Settings.GroupingTemplates));
                }
            }

            // Append base footer template, but only if there's data or if the "ShowHeaderAndFooterOnNoData" setting is enabled.
            if (parsedData.Rows.Count > 0 || Settings.ShowBaseHeaderAndFooterOnNoData)
            {
                templateHtml = baseTemplate.FooterTemplate;
                templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                generatedHtml.Append(await StringReplacementsService.DoAllReplacementsAsync(templateHtml, parsedData.Rows.Count > 0 ? parsedData.Rows[0] : null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
            }

            var output = generatedHtml.ToString();
            if (ExtraDataForReplacements != null && ExtraDataForReplacements.Any())
            {
                output = StringReplacementsService.DoReplacements(output, ExtraDataForReplacements);
            }

            output = await TemplatesService.DoReplacesAsync(output, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);

            return new HtmlString(output);
        }

        #endregion

        #region Parsing

        /// <summary>
        /// This parses the data based on the given data source type and converts it to a <see cref="DataTable"/>.
        /// </summary>
        /// <returns>A <see cref="DataTable"/> with all the parsed data.</returns>
        private async Task<DataTable> ParseDataAsync()
        {
            switch (Settings.DataSource)
            {
                case DataSource.Query:
                    var query = Settings.DataQuery;

                    // Replace the {filters} variable by the joins from the filter component
                    if (query.Contains("{filters}", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.ReplaceCaseInsensitive("{filters}", (await filtersService.GetFilterQueryPartAsync()).JoinPart);
                    }
                    if (query.Contains("{filters(", StringComparison.OrdinalIgnoreCase))
                    {
                        query = Regex.Replace(query, @"{filters\((.*?),(.*?)\)}", (await filtersService.GetFilterQueryPartAsync(productJoinPart: "$1", categoryJoinPart: "$2")).JoinPart);
                    }

                    // Replace the {page_limit} variable for paging.
                    if (query.Contains("{page_limit}", StringComparison.OrdinalIgnoreCase) || !query.Contains(" LIMIT ", StringComparison.OrdinalIgnoreCase))
                    {
                        var limitClause = "";

                        if (Settings.ItemsPerPage == 0)
                        {
                            query = query.ReplaceCaseInsensitive("{page_limit}", "");
                        }
                        else
                        {
                            if (httpContextAccessor.HttpContext == null || !Int32.TryParse(httpContextAccessor.HttpContext.Request.Query["pagenr"].ToString(), out var pageNumber))
                            {
                                pageNumber = 1;
                            }

                            var startIndex = (pageNumber - 1) * Settings.ItemsPerPage;
                            var totalBanners = 0;
                            var bannersForCurrentPage = 0;

                            if (Settings.BannerUsesProductBlockSpace)
                            {
                                for (var index = 0; index < pageNumber * Settings.ItemsPerPage; index++)
                                {
                                    var bannersCount = productBanners.Count(banner => (banner.Position == index + 1 + totalBanners && banner.Method == ProductBannerModel.PlacingMethods.Fixed) || ((index + 1 + totalBanners) % banner.Position == 0 && banner.Method == ProductBannerModel.PlacingMethods.Repeating));
                                    totalBanners += bannersCount;
                                    if (index >= startIndex)
                                    {
                                        bannersForCurrentPage += bannersCount;
                                    }
                                }
                            }

                            // Check if the property must be overruled
                            var loadUpToPageNumberOverrule = Settings.LoadItemsUpToPageNumber;
                            if (Boolean.TryParse(httpContextAccessor.HttpContext?.Request.Query["loadUptoPageNumberOverrule"].ToString(), out var tempUpToPageNumberOverrule))
                            {
                                loadUpToPageNumberOverrule = tempUpToPageNumberOverrule;
                            }

                            limitClause = loadUpToPageNumberOverrule
                                ? $" LIMIT 0, {Settings.ItemsPerPage * pageNumber + bannersForCurrentPage}"
                                : $" LIMIT {startIndex - totalBanners + bannersForCurrentPage}, {Settings.ItemsPerPage - bannersForCurrentPage}";
                        }

                        if (query.Contains("{page_limit}", StringComparison.OrdinalIgnoreCase))
                        {
                            query = query.ReplaceCaseInsensitive("{page_limit}", limitClause);
                        }
                        else
                        {
                            query += limitClause;
                        }
                    }

                    // Do all standard replacements
                    return await RenderAndExecuteQueryAsync(query, skipCache: true);
                case DataSource.Csv:
                    throw new NotImplementedException(); // Use Modules->DataParser
                case DataSource.DataSelector:
                    throw new NotImplementedException();
                case DataSource.Xml:
                    throw new NotImplementedException();
                case DataSource.Json:
                    throw new NotImplementedException();
                case DataSource.Wiser2ParentLinks:
                    throw new NotImplementedException();
                case DataSource.DirectoryOutput:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException("DataSource", Settings.DataSource, "Data source type not supported.");
            }
        }

        /// <summary>
        /// Generate HTML for single layer data. This has support for product banners and item grouping.
        /// </summary>
        /// <param name="data">The <see cref="DataTable"/> with the parsed data.</param>
        /// <param name="template">The <see cref="RepeaterTemplateModel"/> with the HTML templates.</param>
        /// <returns></returns>
        private async Task<string> ParseSingleLayerDataAsync(DataTable data, RepeaterTemplateModel template)
        {
            var html = new StringBuilder();
            var productBannerProperties = typeof(ProductBannerModel).GetProperties().Where(p => p.Name != "Images").ToList();
            var bannersPlaced = 0;
            var blocksPlaced = 0;
            var totalBlocks = data.Rows.Count;
            if (httpContextAccessor.HttpContext == null || !Int32.TryParse(httpContextAccessor.HttpContext.Request.Query["pagenr"].ToString(), out var pageNumber))
            {
                pageNumber = 1;
            }

            for (var index = 0; index < data.Rows.Count; index++)
            {
                // Replacement data for some generic values.
                var genericReplacements = new Dictionary<string, string>
                {
                    { "volgnr", (index + 1).ToString() },
                    { "rowindex", index.ToString() },
                    { "resultcount", data.Rows.Count.ToString() },
                    { "uniqueResultCount", data.Rows.Count.ToString() }
                };
                string templateHtml;

                // Check if we need to place a group header. A group header only needs to be placed if we want to have more than 1 item per group and if we're starting a new group, unless this is the first group and ShowGroupHeaderForFirstGroup is set to false.
                if (Settings.CreateGroupsOfNItems > 1 && ((index == 0 && Settings.ShowGroupHeaderForFirstGroup) || blocksPlaced % Settings.CreateGroupsOfNItems == 0))
                {
                    templateHtml = Settings.GroupHeader;
                    templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                    html.Append(templateHtml);
                }

                // Get the banner(s) that need to be places on this location. Usually this is only one, but in some cases there could be multiple that need to be placed in a row.
                var innerRow = data.Rows[index];
                var bannersToPlace = productBanners.Where(x =>
                    {
                        var currentPosition = ((pageNumber - 1) * Settings.ItemsPerPage) + index + 1 + bannersPlaced;
                        var result = (x.Position == currentPosition && x.Method == ProductBannerModel.PlacingMethods.Fixed) || (currentPosition % x.Position == 0 && x.Method == ProductBannerModel.PlacingMethods.Repeating);
                        if (result)
                        {
                            bannersPlaced++;
                        }

                        return result;
                    })
                    .ToList();

                // Add the banners we're going to place to the total placed items, we'll need this later to decide when to place group footers.
                totalBlocks += bannersToPlace.Count;

                foreach (var banner in bannersToPlace)
                {
                    if (Settings.CreateGroupsOfNItems > 1 && index > 0 && blocksPlaced % Settings.CreateGroupsOfNItems == 0)
                    {
                        // Place a new group header if we have already placed N items/blocks before this.
                        html.Append(Settings.GroupHeader);
                    }
                    else if (index > 0)
                    {
                        // Don't place BetweenItemsTemplate for the first item and also don't place it before the first item in a new group (if we're using groups).
                        html.Append(template.BetweenItemsTemplate);
                    }

                    // Do replacements on the banner HTML and then add that HTML.
                    var productBannerHtml = await StringReplacementsService.DoAllReplacementsAsync(Settings.ProductBannerTemplate, innerRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables);
                    productBannerHtml = productBannerProperties.Aggregate(productBannerHtml, (current, property) => current.ReplaceCaseInsensitive($"{{{property.Name}}}", property.GetValue(banner)?.ToString() ?? ""));
                    html.Append(productBannerHtml);
                    blocksPlaced++;

                    // Check if we need to place a group footer. A group fouter needs to be placed after every N items, unless this is the very last item and ShowGroupFooterForLastGroup is disabled.
                    if (Settings.CreateGroupsOfNItems <= 1 || ((blocksPlaced != totalBlocks || !Settings.ShowGroupFooterForLastGroup) && blocksPlaced % Settings.CreateGroupsOfNItems != 0))
                    {
                        continue;
                    }

                    templateHtml = Settings.GroupFooter;
                    templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                    html.Append(templateHtml);

                    // If we still have more items to place, then also add a group header again.
                    if (blocksPlaced < totalBlocks)
                    {
                        templateHtml = Settings.GroupHeader;
                        templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                        html.Append(templateHtml);
                    }
                }

                // Don't place BetweenItemsTemplate for the first item and also don't place it before the first item in a new group (if we're using groups).
                if (index > 0 && (Settings.CreateGroupsOfNItems == 1 || (Settings.CreateGroupsOfNItems > 1 && blocksPlaced % Settings.CreateGroupsOfNItems > 0)))
                {
                    templateHtml = template.BetweenItemsTemplate;
                    templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                    html.Append(await StringReplacementsService.DoAllReplacementsAsync(templateHtml, innerRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
                }

                templateHtml = template.ItemTemplate;
                templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                html.Append(await StringReplacementsService.DoAllReplacementsAsync(templateHtml, innerRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
                blocksPlaced++;

                // Check if we need to place a group footer. A group fouter needs to be placed after every N items, unless this is the very last item and ShowGroupFooterForLastGroup is disabled.
                if (Settings.CreateGroupsOfNItems > 1 && blocksPlaced % Settings.CreateGroupsOfNItems == 0 && (blocksPlaced < totalBlocks || Settings.ShowGroupFooterForLastGroup))
                {
                    templateHtml = Settings.GroupFooter;
                    templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                    html.Append(templateHtml);
                }
            }

            if (Settings.CreateGroupsOfNItems <= 1)
            {
                return html.ToString();
            }

            // Check if the last group has less than N items and if we have HTML for empty group items.
            var remainingItems = blocksPlaced % Settings.CreateGroupsOfNItems;
            if (remainingItems <= 0 || String.IsNullOrWhiteSpace(Settings.EmptyGroupItemHtml))
            {
                return html.ToString();
            }

            // Add the EmptyGroupItemHtml template for the remaining items in the last group.
            for (var index = 0; index < Settings.CreateGroupsOfNItems - remainingItems; index++)
            {
                html.Append(template.BetweenItemsTemplate);
                html.Append(Settings.EmptyGroupItemHtml);
            }

            // Add the last group footer if needed.
            if (Settings.ShowGroupFooterForLastGroup)
            {
                html.Append(Settings.GroupFooter);
            }

            return html.ToString();
        }

        /// <summary>
        /// Parses data and generates a string based on the templates, variables and actual data.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="templateCollection">The collection of templates, for parsing multi layered data.</param>
        /// <param name="depth">The current depth/level.</param>
        /// <returns>A string.</returns>
        private async Task<string> ParseMultiLayerDataAsync(DataTable data, SortedList<string, RepeaterTemplateModel> templateCollection, int depth = 1)
        {
            var html = new StringBuilder();

            // If the maximum depth has been released, stop processing.
            if (templateCollection.Keys.Count <= depth)
            {
                return "";
            }

            // Process multiple levels/depths.
            var currentIdentifier = templateCollection.Keys[depth];

            var dataColumns = data.Columns.Cast<DataColumn>();

            // Check column existence
            if (dataColumns.All(e => e.ColumnName != currentIdentifier))
            {
                throw new KeyNotFoundException($"Given key in identifier does not belong to table: '{currentIdentifier}'");
            }

            // Get all unique IDs for the current level/depth.
            var columnIds = data.AsEnumerable().Where(dataRow => !dataRow.IsNull(currentIdentifier)).Select(dataRow => dataRow[currentIdentifier].ToString()).Distinct().ToList();

            // Parse template for each row.
            for (var index = 0; index < columnIds.Count; index++)
            {
                var currentIdentifierValue = columnIds[index];
                // New main section.
                var relevantData = (from dataRow in data.AsEnumerable()
                                    where dataRow[currentIdentifier].ToString() == currentIdentifierValue
                                    select dataRow).CopyToDataTable();

                var firstRow = relevantData.Rows.Count != 0 ? relevantData.Rows[0] : null;

                // Replacement data for some generic values.
                var genericReplacements = new Dictionary<string, string>
                {
                    { "volgnr", (index + 1).ToString() },
                    { "rowindex", index.ToString() },
                    { "resultcount", data.Rows.Count.ToString() },
                    { "uniqueResultCount", columnIds.Count.ToString() }
                };
                string templateHtml;

                // Add the header template. 
                if ((Settings.LegacyMode && depth < templateCollection.Keys.Count - 1) || index == 0)
                {
                    templateHtml = templateCollection[currentIdentifier].HeaderTemplate;
                    templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                    html.Append(await StringReplacementsService.DoAllReplacementsAsync(templateHtml, firstRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
                }

                // Add the items to the HTML.
                if (relevantData.Rows.Count == 0)
                {
                    html.Append(templateCollection[currentIdentifier].NoDataTemplate);
                }
                else
                {
                    if (index > 0)
                    {
                        templateHtml = templateCollection[currentIdentifier].BetweenItemsTemplate;
                        templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                        html.Append(await StringReplacementsService.DoAllReplacementsAsync(templateHtml, firstRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
                    }

                    templateHtml = templateCollection[currentIdentifier].ItemTemplate;
                    templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                    html.Append(await StringReplacementsService.DoAllReplacementsAsync(templateHtml, firstRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));

                    // Recursive call to parse the next layer of data.
                    html.Append(await ParseMultiLayerDataAsync(relevantData, templateCollection, depth + 1));
                }

                // Add the footer template.
                if ((Settings.LegacyMode && depth < templateCollection.Keys.Count - 1) || index == columnIds.Count - 1)
                {
                    templateHtml = templateCollection[currentIdentifier].FooterTemplate;
                    templateHtml = StringReplacementsService.DoReplacements(templateHtml, genericReplacements);

                    html.Append(await StringReplacementsService.DoAllReplacementsAsync(templateHtml, firstRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
                }
            }

            return html.ToString();
        }

        #endregion
    }
}