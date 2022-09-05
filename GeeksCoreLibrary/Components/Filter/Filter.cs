using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Filter.Interfaces;
using GeeksCoreLibrary.Components.Filter.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Components.Filter
{
    public class Filter : CmsComponent<FilterCmsSettingsModel, Filter.ComponentModes>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IFiltersService filterService;
        private readonly ILanguagesService languageService;

        #region Enums

        public enum ComponentModes
        {
            /// <summary>
            /// Without using the default aggregation table
            /// </summary>
            Direct = 1,

            /// <summary>
            /// With using the default aggregation table
            /// </summary>
            Aggregation = 2
        }

        #endregion

        #region Private fields

        //private uint TotalItemCount { get; set; }

        #endregion

        #region Constructor

        public Filter(ILogger<Filter> logger, IStringReplacementsService stringReplacementsService, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IAccountsService accountsService, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, IFiltersService filterService, ILanguagesService languageService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.filterService = filterService;
            this.languageService = languageService;

            Logger = logger;
            StringReplacementsService = stringReplacementsService;
            TemplatesService = templatesService;
            DatabaseConnection = databaseConnection;
            AccountsService = accountsService;

            Settings = new FilterCmsSettingsModel();
        }

        #endregion

        #region Handling settings

        /// <inheritdoc />
        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<FilterCmsSettingsModel>(settingsJson);
            if (forcedComponentMode.HasValue && Settings != null)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
        }

        /// <inheritdoc />
        public override string GetSettingsJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(Settings);
        }

        #endregion

        #region Rendering

        /// <inheritdoc />
        public override async Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData)
        {
            ComponentId = dynamicContent.Id;
            ExtraDataForReplacements = extraData;
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
                return new HtmlString("");
            }

            var output = await GenerateFiltersAsync();

            if (ExtraDataForReplacements != null && ExtraDataForReplacements.Any())
            {
                output = StringReplacementsService.DoReplacements(output, ExtraDataForReplacements);
            }

            return new HtmlString(output);
        }

        /// <summary>
        /// Function which generates the HTML of all filters, based on settings and customer data
        /// </summary>
        /// <returns></returns>
        private async Task<string> GenerateFiltersAsync()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext is null.");
            }

            WriteToTrace("Start generating filters...");

            // Try to use the system objects if possible, reverting back to the previous value if they don't exist (by setting them as the default result)
            var filterParameter = await objectsService.FindSystemObjectByDomainNameAsync("filterparameterwiser2", defaultResult: "filterstring");
            var filterParameterMixedMode = (await objectsService.FindSystemObjectByDomainNameAsync("filterparametermixedmodewiser2")).Equals("1");
            var parametersToExclude = await objectsService.FindSystemObjectByDomainNameAsync("filterparameterstoexclude");
            ulong categoryId = 0;
            DataTable dataTable;
            Dictionary<string, List<string>> currentFiltersMulti = null;
            Dictionary<string, string> currentFiltersSingle = null;

            if ((!String.IsNullOrEmpty(filterParameter)) && await objectsService.FindSystemObjectByDomainNameAsync("filterenforcealphabeticalorder") == "true")
            {
                var set404 = false;
                List<string> filterList;

                if (!String.IsNullOrEmpty(filterParameter))
                {
                    filterList = filterService.GetFiltersByParameter(filterParameter).Keys.ToList();
                }
                else
                {
                    var exclude = parametersToExclude.ToLower().Split(',');
                    filterList = (from qs in httpContext.Request.Query.Keys
                                  where !exclude.Contains(qs.ToLower())
                                  select qs.ToLower()).ToList();
                }

                if (filterList.Count > 1)
                {
                    for (var i = 1; i <= filterList.Count - 1; i++)
                    {
                        if (String.Compare(filterList[i], filterList[i - 1], StringComparison.Ordinal) >= 0)
                        {
                            continue;
                        }
                        set404 = true;
                        break;
                    }

                    // Return a 404 if filters not alphabetical
                    if (set404)
                    {
                        WriteToTrace("GCL 404 Because filters not in alphabetical order", true);
                        HttpContextHelpers.Return404(httpContext);
                    }
                }
            }

            // Get the category id from a query if query is given
            if (!String.IsNullOrEmpty(Settings.FilterCategoryIdQuery))
            {
                dataTable = await RenderAndExecuteQueryAsync(Settings.FilterCategoryIdQuery);

                if (dataTable.Rows.Count >= 1)
                {
                    if (!String.IsNullOrEmpty(dataTable.Rows[0].ItemArray[0].ToString()))
                    {
                        categoryId = Convert.ToUInt64(dataTable.Rows[0].ItemArray[0].ToString());
                    }
                    else
                    {
                        WriteToTrace("No category id from query found, using category id 0", true);
                    }
                }
            }

            // Get filtergroups (items of entitytype "filter"), possibly connected to current category
            var filterGroups = await filterService.GetFilterGroupsAsync(categoryId, Settings.ExtraFilterProperties);
            if (filterGroups.Count == 0)
            {
                WriteToTrace("GCL Filter: No filter groups found", true);
            }

            // Add selected values to filter group, so selected templates will be used
            foreach (var filterGroup in filterGroups)
            {
                if (filterGroup.Value.FilterType == FilterGroup.FilterGroupType.MultiSelect)
                {
                    if (currentFiltersMulti == null)
                    {
                        if (!String.IsNullOrEmpty(filterParameter) & !filterParameterMixedMode)
                        {
                            currentFiltersMulti = ConvertQueryStringToDictionary("", filterParameter, parametersToExclude, filterParameterMixedMode);
                        }
                        else
                        {
                            var curUrl = httpContext.Request.QueryString.ToString();
                            var exclude = parametersToExclude.ToLowerInvariant().Split(",");

                            WriteToTrace($"GenerateFiltersAsync curUrl: {curUrl}", true);
                            WriteToTrace($"Exclude: {parametersToExclude.ToLowerInvariant()}", true);

                            var tempResult = new Dictionary<string, List<string>>();
                            foreach (var item in ConvertQueryStringToDictionary(curUrl, filterParameter, parametersToExclude, filterParameterMixedMode))
                            {
                                if (exclude.Contains(item.Key.ToLowerInvariant()))
                                {
                                    continue;
                                }

                                tempResult.Add(item.Key, item.Value);
                            }

                            currentFiltersMulti = tempResult;
                        }
                    }

                    foreach (var f in currentFiltersMulti)
                    {
                        if (!String.Equals(f.Key, filterGroup.Key, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }

                        foreach (var value in f.Value)
                        {
                            if (!String.IsNullOrEmpty(value))
                            {
                                filterGroup.Value.SelectedValues.Add(value);
                            }
                        }
                    }
                }
                else
                {
                    currentFiltersSingle ??= filterService.GetFiltersByParameter(filterParameter);

                    foreach (var f in currentFiltersSingle)
                    {
                        if (f.Key.ToLower() == filterGroup.Key.ToLower())
                        {
                            if (!String.IsNullOrEmpty(f.Value))
                            {
                                filterGroup.Value.SelectedValues.Add(f.Value);
                            }
                        }
                    }
                }
            }

            WriteToTrace(filterGroups.Count + " filter groups found");
            if (filterGroups.Count == 0)
            {
                return String.Empty;
            }

            var minimumItemsRequired = Int32.Parse(await objectsService.FindSystemObjectByDomainNameAsync("filterminimumitemsrequired", defaultResult: "1"));

            // Now retrieve the data and save the result in a dataset
            var languageCode = await languageService.GetLanguageCodeAsync();
            var filterItemsQuery = Settings.FilterItemsQuery;
            filterItemsQuery = filterItemsQuery.Replace("{categoryId}", categoryId.ToString());
            filterItemsQuery = filterItemsQuery.Replace("{languageCode}", languageCode);
            filterItemsQuery = filterItemsQuery.Replace("`wiser_filter_aggregation_`", "`wiser_filter_aggregation`"); // If no language code, trim trailing underscore

            // Replace the {filters} variable with the join and where parts to exclude not possible filter values when filtered
            if (filterItemsQuery.Contains("{filters}"))
            {
                // Generate search part for filter items query.
                // Multiple words in the search term are treated as one search term.
                // Functionality can be expanded in the future with more search options / functionalities.
                var searchPart = new StringBuilder();
                if (!string.IsNullOrEmpty(Settings.SearchQuerystring) && !string.IsNullOrEmpty(Settings.SearchKeys) && !string.IsNullOrEmpty(HttpContext.Request.Query[Settings.SearchQuerystring].ToString()))
                {
                    if (Settings.SearchKeys.Contains(',')) // Multiple keys for search provided
                    {
                        foreach (var searchKey in Settings.SearchKeys.Split(','))
                        {
                            searchPart.AppendLine("");
                            // TODO: Verder uitwerken
                        }
                    }
                    else
                    {
                        searchPart.AppendLine($"JOIN wiser_itemdetail search1 ON search1.item_id=f.product_id AND search1.`key`={Settings.SearchKeys.ToMySqlSafeValue(true)} AND (search1.language_code='{languageCode}' OR search1.language_code='') AND search1.`value` LIKE CONCAT('%',{HttpContext.Request.Query[Settings.SearchQuerystring].ToString().ToMySqlSafeValue(true)},'%')");
                    }
                }                
                
                if (Settings.ComponentMode == ComponentModes.Aggregation)
                {
                    var filterItemsQueryNew = "";
                    var notActiveFilters = "";

                    foreach (var filterGroup in filterGroups)
                    {
                        if (filterGroup.Value.SelectedValues.Count > 0)
                        {
                            var queryPart = await filterService.GetFilterQueryPartAsync(true, filterGroups, forActiveFilter: filterGroup.Value.GetParamKey());
                            if (!String.IsNullOrEmpty(filterItemsQueryNew))
                            {
                                filterItemsQueryNew += " UNION ALL ";
                            }

                            filterItemsQueryNew +=
                                $"({filterItemsQuery.Replace("{filters}", queryPart.JoinPart + searchPart.ToString()).Replace("{filterGroup}", $"AND f.filtergroup='{filterGroup.Value.GetParamKey().ToMySqlSafeValue(false)}'")})";
                        }
                        else
                        {
                            notActiveFilters += $"'{filterGroup.Value.GetParamKey()}',";
                        }
                    }

                    if (String.IsNullOrEmpty(filterItemsQueryNew)) //No active filters
                    {
                        filterItemsQueryNew = filterItemsQuery.Replace("{filters}", searchPart.ToString()).Replace("{filterGroup}", "");
                    }
                    else if (!String.IsNullOrEmpty(notActiveFilters)) //Active and no active filters combined
                    {
                        var queryPart = await filterService.GetFilterQueryPartAsync(true, filterGroups);
                        filterItemsQueryNew += $" UNION ALL ({filterItemsQuery.Replace("{filters}", queryPart.JoinPart + searchPart.ToString()).Replace("{filterGroup}", "AND f.filtergroup IN (" + notActiveFilters.TrimEnd(',') + ")")})";
                    }
                    
                    filterItemsQuery = filterItemsQueryNew;
                }
                else
                {
                    var queryPart = await filterService.GetFilterQueryPartAsync(true, filterGroups);

                    filterItemsQuery = filterItemsQuery.Replace("{filters}", queryPart.JoinPart + searchPart.ToString());
                    filterItemsQuery = filterItemsQuery.Replace("{filtersWhere}", queryPart.WherePart);
                    filterItemsQuery = filterItemsQuery.Replace("{filtersSelectStart}", queryPart.SelectPartStart);
                    filterItemsQuery = filterItemsQuery.Replace("{filtersSelectEnd}", queryPart.SelectPartEnd);
                }
            }

            // Replace user variables if present in query
            filterItemsQuery = await AccountsService.DoAccountReplacementsAsync(filterItemsQuery, true);

            dataTable = await RenderAndExecuteQueryAsync(filterItemsQuery);

            // Loop through filter groups and select rows in the dataset to add filteritems to the filter groups.
            foreach (DataRow row in dataTable.Rows)
            {
                var filterValueColumn = "filtervalue";
                var filterGroupColumn = "filtergroup";

                // Use old names if new name is not present in dataset
                if (!row.Table.Columns.Contains(filterValueColumn))
                {
                    filterValueColumn = "filteritem";
                }
                if (!row.Table.Columns.Contains(filterGroupColumn))
                {
                    filterGroupColumn = "filtergroupseo";
                }

                var filterGroup = row.Field<string>(filterGroupColumn).ToLower();

                if (!filterGroups.ContainsKey(filterGroup))
                {
                    continue;
                }

                WriteToTrace($"Add filter normal: {filterGroup} = {row[filterValueColumn]}");

                // Add extra details (selected with filteritems query) to the filteritem, so these details can be used as variables in templates
                var details = new SortedList<string, string>();
                foreach (DataColumn column in row.Table.Columns)
                {
                    // Add all columns of filteritemsquery as details if not yet present, so columns can be used as variables in templates

                    if (!details.ContainsKey(column.ColumnName))
                    {
                        details.Add(column.ColumnName, row[column.ColumnName].ToString());
                    }
                }

                var count = 0;
                if (row.Table.Columns.Contains("count"))
                {
                    count = Convert.ToInt16(row["count"]);
                }

                filterGroups[filterGroup].AddItem(row[filterValueColumn].ToString(), count, details);
            }

            // Add the filters items of the advanced (Wiser 2) filters
            Dictionary<string, string> replaceData;
            foreach (var filterGroup in filterGroups)
            {
                if (String.IsNullOrEmpty(filterGroup.Value.AdvancedFilter))
                {
                    continue;
                }

                foreach (var filter in filterGroup.Value.GetAdvancedFilters)
                {
                    WriteToTrace($"Add advanced filter: {filterGroup.Key} = {filter.Key}");
                    filterGroup.Value.AddItem(filter.Key, 1);
                }
            }

            // Create HTML for all the filter groups
            var filtersHtml = new StringBuilder();

            // NOTE: This code assumes that filters in the same group are sorted properly so that they are together in the results, otherwise this code gets too complicated.
            var groupHtml = new StringBuilder();
            var previousFilterType = "";
            var previousFilterGroup = new FilterGroup();

            foreach (var filterGroup in filterGroups.Values)
            {
                var filterGroupMinimumItemsRequired = filterGroup.MinimumItemsRequired;
                if (filterGroupMinimumItemsRequired <= 0)
                {
                    filterGroupMinimumItemsRequired = minimumItemsRequired;
                }

                if (!String.IsNullOrEmpty(filterGroup.Group))
                {
                    if (filterGroup.Group != previousFilterGroup.Group && previousFilterGroup.Group != null)
                    {
                        replaceData = new Dictionary<string, string>
                        {
                            { "name_seo", previousFilterGroup.Group.ConvertToSeo() },
                            { "name_cf", previousFilterGroup.Group.CapitalizeFirst() },
                            { "name", previousFilterGroup.Group },
                            { "items", groupHtml.ToString() },
                            { "type", previousFilterType },
                            { "typename", previousFilterType }
                        }; 
                        filterGroup.AddExtraPropertiesToList(replaceData);

                        var template = !String.IsNullOrEmpty(filterGroup.GroupTemplate) ? filterGroup.GroupTemplate : Settings.TemplateFilterGroup;
                        template = StringReplacementsService.DoReplacements(template, replaceData);

                        filtersHtml.Append(template);
                        groupHtml.Clear();
                    }

                    BuildFilterGroupHtml(filterGroups, filterGroupMinimumItemsRequired, filterGroup, groupHtml, filterGroup.Group, filterGroup.Group);
                    previousFilterType = filterGroup.FilterType.ToString("D");
                    previousFilterGroup = filterGroup;
                }
                else
                {
                    if (groupHtml.Length > 0)
                    {
                        replaceData = new Dictionary<string, string>
                        {
                            { "name_seo", previousFilterGroup.Group.ConvertToSeo() },
                            { "name_cf", previousFilterGroup.Group.CapitalizeFirst() },
                            { "name", previousFilterGroup.Group },
                            { "items", groupHtml.ToString() },
                            { "type", previousFilterType },
                            { "typename", previousFilterType }
                        };
                        filterGroup.AddExtraPropertiesToList(replaceData);

                        var template = !String.IsNullOrEmpty(filterGroup.GroupTemplate) ? filterGroup.GroupTemplate : Settings.TemplateFilterGroup;
                        template = StringReplacementsService.DoReplacements(template, replaceData);

                        filtersHtml.Append(template);
                        groupHtml.Clear();
                    }

                    BuildFilterGroupHtml(filterGroups, filterGroupMinimumItemsRequired, filterGroup, filtersHtml, filterGroup.Name, filterGroup.NameSeo);
                }
            }

            // If last item is part of group, add the group to the general string builder
            if (groupHtml.Length > 0)
            {
                replaceData = new Dictionary<string, string>
                {
                    { "name_seo", previousFilterGroup.Group.ConvertToSeo() },
                    { "name_cf", previousFilterGroup.Group.CapitalizeFirst() },
                    { "name", previousFilterGroup.Group },
                    { "items", groupHtml.ToString() },
                    { "type", previousFilterType },
                    { "typename", previousFilterType }
                };
                previousFilterGroup?.AddExtraPropertiesToList(replaceData);

                var template = !String.IsNullOrEmpty(previousFilterGroup.GroupTemplate) ? previousFilterGroup.GroupTemplate : Settings.TemplateFilterGroup;
                template = StringReplacementsService.DoReplacements(template, replaceData);

                filtersHtml.Append(template);
                groupHtml.Clear();
            }

            // Create summary
            var summary = new StringBuilder();
            var summaryGroups = new StringBuilder();
            foreach (var f in filterGroups.Values.Where(o => o.SelectedValues.Count > 0 || o.SelectedValueString != ""))
            {
                if (f.HideInSummary)
                {
                    continue;
                }

                var summaryGroupItems = new StringBuilder();
                var tempGroup = f.Name.Contains("/") ? f.Name.Split("/")[1] : f.Name;
                foreach (var selectedFilterItem in f.SelectedValues)
                {
                    var tempName = f.ContainsOrder ? selectedFilterItem.Split("|")[1] : selectedFilterItem.ToLower();
                    var filterItemFound = false;
                    WriteToTrace($"1 - CreateFilterURL({f.NameSeo}, {selectedFilterItem}), False");

                    replaceData = new Dictionary<string, string>
                    {
                        { "name_seo", tempName.ConvertToSeo() },
                        { "name_cf", tempName.CapitalizeFirst() },
                        { "name", tempName },
                        { "itemdetail_name", tempName },
                        { "groupname", tempGroup },
                        { "group", !String.IsNullOrEmpty(f.QueryString) ? f.QueryString : f.NameSeo },
                        { "url", CreateFilterUrl(filterGroups, f.NameSeo, selectedFilterItem) }
                    };

                    foreach (var item in f.Items)
                    {
                        if (!String.Equals(item.Key, tempName, StringComparison.OrdinalIgnoreCase) || item.Value.ItemDetails == null)
                        {                            
                            continue;
                        }

                        foreach (var itemDetail in item.Value.ItemDetails)
                        {
                               filterItemFound = true;
                               replaceData[itemDetail.Key] = itemDetail.Value;
                        }
                    }

                    if (f.FilterType== FilterGroup.FilterGroupType.Slider)
                    {
                        filterItemFound = true;
                    }

                    // When aggregation is used, then skip the filter summary item if it is excluded by other active filters
                    if ((Settings.ComponentMode != ComponentModes.Aggregation) || filterItemFound)
                    {
                        var summaryItem = StringReplacementsService.DoReplacements(Settings.TemplateSummaryFilterGroupItem, replaceData);
                        summaryGroupItems.Append(summaryItem);
                    }
                }
                WriteToTrace($"2 - CreateFilterURL({f.NameSeo}, , False)");

                replaceData = new Dictionary<string, string>
                {
                    { "groupname", tempGroup },
                    { "group", f.NameSeo },
                    { "selectedvalues", summaryGroupItems.ToString() },
                    { "url", CreateFilterUrl(filterGroups, f.NameSeo, "") }
                };

                var summaryGroupHtml = StringReplacementsService.DoReplacements(Settings.TemplateSummaryFilterGroup, replaceData);

                summaryGroups.Append(summaryGroupHtml);
            }

            var hasActiveFilters = filterGroups.Any(fg => fg.Value.SelectedValues.Count > 0 || !String.IsNullOrWhiteSpace(fg.Value.SelectedValueString));
            if (hasActiveFilters && filterGroups.Any(x => !x.Value.HideInSummary))
            {
                WriteToTrace("3 - CreateFilterURL(, , False)");

                replaceData = new Dictionary<string, string>
                {
                    { "items", summaryGroups.ToString() },
                    { "url", CreateFilterUrl(filterGroups, "", "") }
                };

                var templateSummaryHtml = StringReplacementsService.DoReplacements(Settings.TemplateSummary, replaceData);

                summary.Append(templateSummaryHtml);
            }

            // Create the full filter HTML result
            var result = Settings.TemplateFull.Replace("{filters}", filtersHtml.ToString())
                .Replace("{summary}", summary.ToString())
                .Replace("{category_id}", categoryId.ToString())
                .Replace("{has_active_filters}", hasActiveFilters ? "1" : "0");

            // Handle if-statements and translations
            result = await TemplatesService.DoReplacesAsync(result);

            return result;
        }

        /// <summary>
        /// Create the URL for adding or deleting filter items
        /// </summary>
        /// <param name="filterGroups"></param>
        /// <param name="groupName"></param>
        /// <param name="filter"></param>
        /// <param name="singleSelect"></param>
        /// <returns></returns>
        private string CreateFilterUrl(Dictionary<string, FilterGroup> filterGroups, string groupName, string filter, bool singleSelect = false)
        {
            try
            {
                var parameterList = new SortedList<string, string>();
                const string ValueSplit = ",";

                foreach (var f in filterGroups.Values.Where(group => group.SelectedValueString != ""))
                {
                    string nameStr;
                    if (!String.IsNullOrEmpty(f.QueryString))
                    {
                        nameStr = f.QueryString;
                    }
                    else if (!String.IsNullOrEmpty(f.NameSeo))
                    {
                        nameStr = f.NameSeo;
                    }
                    else
                    {
                        nameStr = f.Name.ToLower();
                    }

                    // Check existing selected filters and add value to it or remove value from it
                    if (nameStr == groupName.ToLower())
                    {
                        if (singleSelect)
                        {
                            WriteToTrace($"1 - adding item to parameter list, key = {groupName.ToLower()}, value = {filter.ToLower()}");
                            parameterList.Add(groupName.ToLower(), filter.ToLower());
                        }
                        else
                        {
                            WriteToTrace($"2 - adding item to parameter list, key = {groupName.ToLower()}, value = {f.SelectedValueString}{ValueSplit}{filter.ToLower()}");
                            parameterList.Add(groupName.ToLower(), f.SelectedValueString + ValueSplit + filter.ToLower());
                        }
                    }
                    else
                    {
                        // Add selected filterGroup to URL
                        WriteToTrace($"3 - adding item to parameter list, key = {nameStr}, value = {f.SelectedValueString}");
                        parameterList.Add(nameStr, f.SelectedValueString);
                    }
                }

                if (!filterGroups.Values.Any(group => group.SelectedValueString != "" && group.NameSeo == groupName.ToLower()))
                {
                    if (!parameterList.ContainsKey(groupName.ToLower()))
                    {
                        WriteToTrace($"4 - adding item to parameter list, key = {groupName.ToLower()}, value = {filter.ToLower()}");
                        parameterList.Add(groupName.ToLower(), filter.ToLower());
                    }
                }

                // Generate URL
                var tr = new StringBuilder();
                if (parameterList.Keys.Count > 0)
                {
                    tr.Append('?');

                    foreach (var p in parameterList.Keys)
                    {
                        if (tr.Length > 1)
                        {
                            tr.Append('&');
                        }
                        tr.Append(p.UrlEncode());
                        tr.Append('=');
                        tr.Append(parameterList[p]?.HtmlDecode().UrlEncode());
                    }
                }

                WriteToTrace($"ADD: {groupName}-{filter}-{tr}");

                return tr.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error on CreateFilterURL with groupName='{groupName}' and filter='{filter}'. Error: {ex}");
            }
        }

        /// <summary>
        /// Make a dictionary of the querystrings, based on settings for filtering
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="filterParameter"></param>
        /// <param name="parametersToExclude"></param>
        /// <param name="filterParameterMixedMode"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> ConvertQueryStringToDictionary(string queryString, string filterParameter, string parametersToExclude, bool filterParameterMixedMode)
        {
            var result = new Dictionary<string, List<string>>();
            var curQs = new Dictionary<string, string>();

            var chosenFilters = new List<string>();
            if (!String.IsNullOrEmpty(filterParameter))
            {
                curQs = filterService.GetFiltersByParameter(filterParameter);
                chosenFilters.AddRange(curQs.Keys.ToList());
            }

            if (String.IsNullOrEmpty(filterParameter) || filterParameterMixedMode)
            {
                if (String.IsNullOrWhiteSpace(queryString))
                {
                    return result;
                }

                // If there's no question mark, the index will become -1, which is fine
                var startIndex = queryString.IndexOf("?", StringComparison.Ordinal);
                curQs = curQs.Concat(queryString[(startIndex + 1)..].ToDictionary("&", "=").Where(p => !curQs.Keys.Contains(p.Key))).ToDictionary(p => p.Key, p => p.Value);
                var exclude = parametersToExclude.Split(',');
                chosenFilters.AddRange(curQs.Keys.Where(qs => !exclude.Contains(qs, StringComparer.OrdinalIgnoreCase)).ToList());
            }

            foreach (var cf in chosenFilters.Distinct())
            {
                var value = curQs.FirstOrDefault(p => p.Key == cf).Value;

                if (value.Contains("%"))
                {
                    value = value.UrlDecode();
                }

                result.Add(cf, value.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList());
            }

            return result;
        }

        private void BuildFilterGroupHtml(Dictionary<string, FilterGroup> filterGroups, int minimumItemsRequired, FilterGroup filterGroup, StringBuilder htmlBuilder, string filterGroupName, string filterGroupNameSeo)
        {
            string tempValue;
            string tempValueSeo;

            var filterHtml = new StringBuilder();
            Dictionary<string, string> replaceData;
            // loop through all filter items
            switch (filterGroup.FilterType)
            {
                case FilterGroup.FilterGroupType.SingleSelect:
                {
                    if (filterGroup.Items.Count >= minimumItemsRequired)
                    {
                        foreach (var k in filterGroup.Items.Keys)
                        {
                            tempValue = filterGroup.Items[k].Value;
                            tempValueSeo = filterGroup.Items[k].ValueSEO;
                            if (filterGroup.ContainsOrder)
                            {
                                tempValue = tempValue.Split('|').Last();
                                tempValueSeo = tempValueSeo.Split('|').Last();
                            }

                            if (filterGroup.SelectedValues.Contains(tempValue))
                            {
                                WriteToTrace($"4 - CreateFilterURL({filterGroup.GetParamKey()}, {filterGroup.Items[k].Value})");

                                replaceData = new Dictionary<string, string>
                                {
                                    { "name_seo", tempValueSeo },
                                    { "name_cf", tempValue.CapitalizeFirst() },
                                    { "name", tempValue },
                                    { "count", filterGroup.Items[k].Count.ToString() },
                                    { "url", CreateFilterUrl(filterGroups, filterGroup.GetParamKey(), filterGroup.Items[k].Value, true) }
                                };

                                if (filterGroup.Items[k].ItemDetails != null)
                                {
                                    foreach (var itemDetail in filterGroup.Items[k].ItemDetails)
                                    {
                                        replaceData[itemDetail.Key] = itemDetail.Value;
                                    }
                                }

                                var tempTemplate = !String.IsNullOrEmpty(filterGroup.SelectedItemTemplate) ? filterGroup.SelectedItemTemplate : Settings.TemplateSingleSelectItemSelected;
                                tempTemplate = StringReplacementsService.DoReplacements(tempTemplate, replaceData);

                                filterHtml.Append(tempTemplate);
                            }
                            else
                            {
                                WriteToTrace($"5 - CreateFilterURL({filterGroup.GetParamKey()}, {filterGroup.Items[k].Value})");

                                replaceData = new Dictionary<string, string>
                                {
                                    { "name_seo", tempValueSeo },
                                    { "name_cf", tempValue.CapitalizeFirst() },
                                    { "name", tempValue },
                                    { "count", filterGroup.Items[k].Count.ToString() },
                                    { "url", CreateFilterUrl(filterGroups, filterGroup.GetParamKey(), filterGroup.Items[k].Value, true) }
                                };

                                if (filterGroup.Items[k].ItemDetails != null)
                                {
                                    foreach (var itemDetail in filterGroup.Items[k].ItemDetails)
                                    {
                                        replaceData[itemDetail.Key] = itemDetail.Value;
                                    }
                                }

                                var tempTemplate = !String.IsNullOrEmpty(filterGroup.ItemTemplate) ? filterGroup.ItemTemplate : Settings.TemplateSingleSelectItem;
                                tempTemplate = StringReplacementsService.DoReplacements(tempTemplate, replaceData);

                                filterHtml.Append(tempTemplate);
                            }
                        }
                    }

                    break;
                }

                case FilterGroup.FilterGroupType.MultiSelect:
                {
                    if (filterGroup.Items.Count >= minimumItemsRequired)
                    {
                        foreach (var k in filterGroup.Items.Keys)
                        {
                            tempValue = filterGroup.Items[k].Value;
                            tempValueSeo = filterGroup.Items[k].ValueSEO;

                            if (filterGroup.ContainsOrder)
                            {
                                tempValue = tempValue.Split("|").Last();
                                tempValueSeo = tempValueSeo.Split("|").Last();
                            }

                            WriteToTrace("BuildFilterGroupHTML: " + filterGroup.Items[k].Value + ":" + String.Join(",", filterGroup.SelectedValues.ToArray()));

                            if (filterGroup.SelectedValues.Any(v => v.Equals(filterGroup.Items[k].Value, StringComparison.OrdinalIgnoreCase)))
                            {
                                WriteToTrace("6 - CreateFilterURL(" + filterGroup.GetParamKey() + ", " + filterGroup.Items[k].Value + ")");

                                replaceData = new Dictionary<string, string>
                                {
                                    { "name_seo", tempValueSeo },
                                    { "name_cf", tempValue.CapitalizeFirst() },
                                    { "name", tempValue },
                                    { "count", filterGroup.Items[k].Count.ToString() },
                                    { "url", CreateFilterUrl(filterGroups, filterGroup.GetParamKey(), filterGroup.Items[k].Value) }
                                };

                                if (filterGroup.Items[k].ItemDetails != null)
                                {
                                    foreach (var itemDetail in filterGroup.Items[k].ItemDetails)
                                    {
                                        replaceData[itemDetail.Key] = itemDetail.Value;
                                    }
                                }

                                var tempTemplate = !String.IsNullOrEmpty(filterGroup.SelectedItemTemplate) ? filterGroup.SelectedItemTemplate : Settings.TemplateMultiSelectItemSelected;
                                tempTemplate = StringReplacementsService.DoReplacements(tempTemplate, replaceData);

                                filterHtml.Append(tempTemplate);
                            }
                            else
                            {
                                WriteToTrace("7 - CreateFilterURL(" + filterGroup.GetParamKey() + ", " + filterGroup.Items[k].Value + ")");

                                replaceData = new Dictionary<string, string>
                                {
                                    { "name_seo", tempValueSeo },
                                    { "name_cf", tempValue.CapitalizeFirst() },
                                    { "name", tempValue },
                                    { "count", filterGroup.Items[k].Count.ToString() },
                                    { "group", filterGroup.NameSeo },
                                    { "url", CreateFilterUrl(filterGroups, filterGroup.GetParamKey(), filterGroup.Items[k].Value) }
                                };

                                if (filterGroup.Items[k].ItemDetails != null)
                                {
                                    foreach (var itemDetail in filterGroup.Items[k].ItemDetails)
                                    {
                                        replaceData[itemDetail.Key] = itemDetail.Value;
                                    }
                                }

                                var tempTemplate = !String.IsNullOrEmpty(filterGroup.ItemTemplate) ? filterGroup.ItemTemplate : Settings.TemplateMultiSelectItem;
                                tempTemplate = StringReplacementsService.DoReplacements(tempTemplate, replaceData);

                                filterHtml.Append(tempTemplate);
                            }
                        }
                    }

                    break;
                }

                case FilterGroup.FilterGroupType.Slider:
                {
                    WriteToTrace($"1 - BuildFilterGroupHtml Slider({filterGroup.NameSeo})");
                    // Only show the slider if we can actually use them.
                    if (filterGroup.MinValue >= filterGroup.MaxValue)
                    {
                        WriteToTrace($"Not showing slider filter '{filterGroup.Name}' because of invalid min and max values. MinValue: {filterGroup.MinValue}, MaxValue: {filterGroup.MaxValue}");
                    }
                    else
                    {
                        WriteToTrace($"2 - BuildFilterGroupHtml Slider({filterGroup.NameSeo})");

                        var selectedMinValue = filterGroup.MinValue;
                        var selectedMaxValue = filterGroup.MaxValue;
                        var requestParameter = "";

                        if (!String.IsNullOrEmpty(HttpContext.Request.Query[filterGroup.GetParamKey()]))
                        {
                            requestParameter = HttpContext.Request.Query[filterGroup.GetParamKey()].ToString();
                        }
                        else if (filterGroup.SelectedValues.Count > 0) // when one filter querystring instead of querystrings per filter
                        {
                            requestParameter = filterGroup.SelectedValues[0];
                        }

                        if (!string.IsNullOrEmpty(requestParameter))
                        {
                            if (requestParameter.Contains("-"))
                            {
                                selectedMinValue = Decimal.Parse(requestParameter.Split("-")[0].Replace(",", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator).Replace(".", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                                selectedMaxValue = Decimal.Parse(requestParameter.Split("-")[1].Replace(",", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator).Replace(".", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                            }
                            else
                            {
                                selectedMinValue = 0;
                                selectedMaxValue = Decimal.Parse(requestParameter.Replace(",", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator).Replace(".", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                            }

                            WriteToTrace("Set selected slider values: " + filterGroup.SelectedValueString);
                        }
                            
                        var tempTemplate = !String.IsNullOrEmpty(filterGroup.ItemTemplate) ? filterGroup.ItemTemplate : Settings.TemplateSlider;
                        filterHtml.Append(tempTemplate.Replace("{minValue}", filterGroup.MinValue.ToString(CultureInfo.InvariantCulture))
                                              .Replace("{maxValue}", filterGroup.MaxValue.ToString(CultureInfo.InvariantCulture))
                                              .Replace("{selectedMin}", selectedMinValue.ToString(CultureInfo.InvariantCulture))
                                              .Replace("{selectedMax}", selectedMaxValue.ToString(CultureInfo.InvariantCulture))
                                              .Replace("{filterName}", filterGroup.Name)
                                              .Replace("{filterNameSeo}", filterGroup.NameSeo));
                }

                    break;
                }
            }

            // Add filter HTML
            // Dim filterGroupName As String = filterGroup.Name
            // Dim filterGroupNameSEO As String = filterGroup.NameSEO

            if (filterHtml.Length > 0)
            {
                // Don't add filter if it has no items
                if (filterGroup.FilterType == FilterGroup.FilterGroupType.Slider && ((Settings.TemplateFilterGroupSlider != "") || (filterGroup.GroupTemplate != "")))
                {
                    replaceData = new Dictionary<string, string>
                    {
                        { "name_seo", filterGroup.NameSeo },
                        { "name_cf", filterGroup.Name.CapitalizeFirst() },
                        { "name", filterGroup.Name },
                        { "querystring", !String.IsNullOrEmpty(filterGroup.QueryString) ? filterGroup.QueryString : filterGroup.NameSeo },
                        { "items", filterHtml.ToString() },
                        { "type", filterGroup.FilterType.ToString("D") },
                        { "typename", filterGroup.FilterType.ToString("G") }
                    };

                    filterGroup.AddExtraPropertiesToList(replaceData);

                    // Use the special slider filter group template if it's filled
                    var template = !String.IsNullOrEmpty(filterGroup.GroupTemplate) ? filterGroup.GroupTemplate : Settings.TemplateFilterGroupSlider;
                    template = StringReplacementsService.DoReplacements(template, replaceData);

                    htmlBuilder.Append(template);
                }
                else if (!String.IsNullOrEmpty(filterGroup.Group))
                {
                    htmlBuilder.Append(filterHtml);
                }
                else
                {
                    replaceData = new Dictionary<string, string>
                    {
                        { "name_seo", filterGroupNameSeo },
                        { "name_cf", filterGroupName.CapitalizeFirst() },
                        { "name", filterGroupName },
                        { "querystring", !String.IsNullOrEmpty(filterGroup.QueryString) ? filterGroup.QueryString : filterGroup.NameSeo },
                        { "items", filterHtml.ToString() },
                        { "type", filterGroup.FilterType.ToString("D") },
                        { "typename", filterGroup.FilterType.ToString("G") }
                    };

                    filterGroup.AddExtraPropertiesToList(replaceData);

                    var template = !String.IsNullOrEmpty(filterGroup.GroupTemplate) ? filterGroup.GroupTemplate : Settings.TemplateFilterGroup;
                    template = StringReplacementsService.DoReplacements(template, replaceData);

                    htmlBuilder.Append(template);
                }
            }

            // Replace selected count
            htmlBuilder.Replace("{selectedcount}", filterGroup.SelectedValues.Count.ToString());

            // Replace all instances of group name within the generated HTML
            htmlBuilder.Replace("{groupname}", filterGroupName);
            htmlBuilder.Replace("{groupname_seo}", filterGroupNameSeo);

            // Replace all instances of classes within the generated HTML
            htmlBuilder.Replace("{classes}", filterGroup.Classes);
        }

        #endregion
    }
}
