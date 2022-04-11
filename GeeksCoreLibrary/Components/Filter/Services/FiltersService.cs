using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Filter.Interfaces;
using GeeksCoreLibrary.Components.Filter.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;

namespace GeeksCoreLibrary.Components.Filter.Services
{
    public class FiltersService : IFiltersService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly ILogger<FiltersService> logger;
        private readonly ILanguagesService languagesService;

        public FiltersService(IDatabaseConnection databaseConnection, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, ILogger<FiltersService> logger, IOptions<GclSettings> gclSettings, ILanguagesService languagesService)
        {
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.languagesService = languagesService;
        }

        /// <inheritdoc />
        public async Task<QueryPartModel> GetFilterQueryPartAsync(bool forFilterItemsQuery = false, Dictionary<string, FilterGroup> givenFilterGroups = null, string productJoinPart = "", string categoryJoinPart = "", string forActiveFilter = "")
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new Exception("HttpContext is null.");
            }

            try
            {
                const string ValueSplit = ",";

                var output = new QueryPartModel();
                var queryJoinPart = new StringBuilder();
                var filters = new SortedList<string, string>();
                var filterParameter = await objectsService.FindSystemObjectByDomainNameAsync("filterparameterwiser2", defaultResult: "filterstring");
                var filterParameterMixedMode = (await objectsService.FindSystemObjectByDomainNameAsync("filterparametermixedmodewiser2", defaultResult: "0")).Equals("1");
                var filterParametersToExclude = await objectsService.FindSystemObjectByDomainNameAsync("filterparameterstoexclude", defaultResult: "templateid,pagenr,gclid,_ga");

                // Get a list of filters from the URL
                if (!String.IsNullOrEmpty(filterParameter))
                {
                    foreach (var item in GetFiltersByParameter(filterParameter))
                    {
                        filters.Add(item.Key, item.Value);
                    }
                }
                if (String.IsNullOrEmpty(filterParameter) | filterParameterMixedMode)
                {
                    foreach (var key in httpContext.Request.Query.Keys)
                    {
                        if (key != "")
                        {
                            if (filters.ContainsKey(key))
                            {
                                if (!String.IsNullOrEmpty(filters[key]))
                                {
                                    continue;// Skip if already defined in case of mixed mode
                                }
                            }
                            if (!filterParametersToExclude.Split(',').ToList().Contains(key))
                            {
                                filters.Add(key, httpContext.Request.Query[key]);
                            }
                        }
                    }
                }

                // TODO: Functionaliteit voor onthouden filters over categorieën heen
                //if (filters.Count == 0 && (JCLUtils.ReadCookie("GclFiltersRemembrance") != ""))
                //{
                //    var filtersRemembrance = JCLUtils.ReadCookie("GclFiltersRemembrance");

                //    logger.LogTrace("GetFilterQueryPart - filtersRemembrance: " + filtersRemembrance, true);
                //    foreach (var item in filtersRemembrance.Split("&"))
                //    {
                //        if (item.Contains("="))
                //            filters.Add(item.Split("=")(0), item.Split("=")(1));
                //    }
                //}

                try
                {
                    Dictionary<string, FilterGroup> filterGroups;
                    var filterConnectionPart = await objectsService.FindSystemObjectByDomainNameAsync("filterconnectionpart", "i.id");
                    List<FilterConnectionPart> filterConnectionParts = new List<FilterConnectionPart>();

                    if (givenFilterGroups != null && givenFilterGroups.Count > 0)
                    {
                        filterGroups = givenFilterGroups;
                    }
                    else
                    {
                        filterGroups = await GetFilterGroupsAsync();
                    }

                    // Add the filters with a custom join part if the query-part is for the filteritemsquery
                    if (forFilterItemsQuery)
                    {
                        foreach (var filterGroup in filterGroups)
                        {
                            if (String.IsNullOrEmpty(filterGroup.Value.CustomJoin))
                            {
                                continue;
                            }

                            if (!filters.ContainsKey(filterGroup.Value.NameSeo))
                            {
                                filters.Add(filterGroup.Value.NameSeo, "LEFT JOIN");// Abuse value to force LEFT JOIN
                            }
                        }
                    }

                    // Add the different entitynames with the corresponding connectionparts to the list
                    if (filterConnectionPart.Contains(";"))
                    {
                        foreach (var value in filterConnectionPart.Split("~"))
                        {
                            if (value.Split(';').Count() > 2)
                            {
                                filterConnectionParts.Add(new FilterConnectionPart(value.Split(';')[0], value.Split(';')[1], value.Split(';')[2] == "linkdetail"));
                                logger.LogTrace("Add to filterConnectionParts: " + value.Split(';')[0] + ";" + value.Split(';')[1] + ";" + value.Split(';')[2]);


                            }
                            else
                            {
                                filterConnectionParts.Add(new FilterConnectionPart(value.Split(';')[0], value.Split(';')[1], false));
                                logger.LogTrace("Add to filterConnectionParts: " + value.Split(';')[0] + ";" + value.Split(';')[1]);
                            }
                        }
                    }

                    // Build JOINS
                    var filterCounter = 0;
                    var filterCount = 0;
                    foreach (var filterName in filters.Keys)
                    {
                        if (filterName.ToLower().StartsWith("utm_", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (String.IsNullOrEmpty(filterParameter) && filterParametersToExclude.Split(",").ToList().Contains(filterName))
                        {
                            continue;
                        }

                        var filterNameFromGroup = filterName;
                        var filterValue = filters[filterName];
                        var isAdvancedFilter = false;

                        logger.LogTrace($"DynamicFilter - {filterName}:{filterValue}");
                        logger.LogTrace($"DynamicFilter - Filter groups: {String.Join(", ", filterGroups?.Select(x => x.Key))}");

                        FilterGroup filterGroup = null;
                        if (filterGroups != null && filterGroups.ContainsKey(filterName))
                        {
                            filterGroup = filterGroups[filterName];
                            filterNameFromGroup = filterGroup.NameSeo;
                            logger.LogTrace($"DynamicFilter - Found filter group, type = {filterGroup.FilterType.ToString()}");
                        }

                        if (filterGroup != null && !String.IsNullOrEmpty(filterValue))
                        {
                            if (!String.IsNullOrEmpty(filterGroup.AdvancedFilter))
                            {
                                foreach (var filter in filterGroup.GetAdvancedFilters)
                                {
                                    if (filterValue.ToLower().Split(ValueSplit).Contains(filter.Key.ToLower()))
                                        queryJoinPart.AppendLine(filter.Value);
                                }
                                isAdvancedFilter = true;
                            }
                            else if (!String.IsNullOrEmpty(filterGroup.CustomJoin))
                            {
                                if (forFilterItemsQuery)
                                {
                                    if (filterValue == "LEFT JOIN")
                                        queryJoinPart.Append("LEFT ");

                                    if (!String.IsNullOrEmpty(filterGroup.CustomSelect))
                                    {
                                        output.SelectPartStart += filterGroup.CustomSelect.Split("{select}")[0];
                                        if (filterGroup.CustomSelect.Contains("{select}"))
                                        {
                                            output.SelectPartEnd += filterGroup.CustomSelect.Split("{select}")[1];
                                        }
                                    }
                                }
                                queryJoinPart.AppendLine(filterGroup.CustomJoin);
                                isAdvancedFilter = true;
                            }
                            else
                            {
                                logger.LogTrace("Checking filterConnectionParts");
                                var filterConnectionPartIsLinkType = false;
                                if (filterConnectionParts.Count > 0)
                                {
                                    logger.LogTrace("using filterConnectionParts");
                                    if (filterConnectionParts.Exists(x => x.TypeName == filterGroup.EntityName))
                                    {
                                        filterConnectionPart = filterConnectionParts.Find(x => x.TypeName == filterGroup.EntityName).JoinPart;
                                        filterConnectionPartIsLinkType = filterConnectionParts.Find(x => x.TypeName == filterGroup.EntityName).IsLinkType;
                                        logger.LogTrace("current filterConnectionPart: " + filterConnectionPart);
                                    }
                                }

                                if (filterGroup.IsGroupFilter && filterValue.Contains(ValueSplit))
                                {
                                    foreach (var filterV in filterValue.Split(ValueSplit))
                                    {
                                        if (!filterGroup.IsGroupFilter && forFilterItemsQuery)
                                        {
                                            queryJoinPart.Append("LEFT ");

                                            if (!String.IsNullOrEmpty(filterGroup.QueryString))
                                            {
                                                output.WherePart += $"AND (fi{filterCounter}.id IS NOT NULL OR queryString .`value`={filterGroup.QueryString.ToMySqlSafeValue(true)})" + Environment.NewLine;
                                            }

                                            else
                                            {
                                                output.WherePart += $"AND (fi{filterCounter}.id IS NOT NULL OR filterName.`value`={filterNameFromGroup.ToMySqlSafeValue(true)})" + Environment.NewLine;
                                            }

                                        }

                                        if (filterConnectionPartIsLinkType)
                                        {
                                            if (filterGroup.IsMultiLanguage)
                                            {
                                                queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemLinkDetail} fi{filterCounter} ON (fi{filterCounter}.language_code = '{languagesService.CurrentLanguageCode}' OR fi{filterCounter}.language_code = '') AND fi{filterCounter}.itemlink_id = {filterConnectionPart} ");
                                            }
                                            else
                                            {
                                                queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemLinkDetail} fi{filterCounter} ON fi{filterCounter}.itemlink_id = {filterConnectionPart} ");
                                            }
                                        }
                                        else if (filterGroup.IsMultiLanguage)
                                        {
                                            queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemDetail} fi{filterCounter} ON (fi{filterCounter}.language_code = '{languagesService.CurrentLanguageCode}' OR fi{filterCounter}.language_code = '') AND fi{filterCounter}.item_id = {filterConnectionPart} ");
                                        }
                                        else
                                        {
                                            queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemDetail} fi{filterCounter} ON fi{filterCounter}.item_id = {filterConnectionPart} ");
                                        }

                                        var joinPart = AppendFilterJoinPart(filterCounter, filterNameFromGroup, filterV, filterGroup, false);
                                        if (joinPart != "")
                                        {
                                            queryJoinPart.Append("AND " + joinPart);
                                        }
                                        queryJoinPart.AppendLine();

                                        filterCounter += 1;
                                        isAdvancedFilter = true; // So the AppendFilterJoinPart will not be called and the filterCounter will not be increased
                                    }
                                }
                                else
                                {
                                    if (!filterGroup.IsGroupFilter && forFilterItemsQuery)
                                    {
                                        if (!filterGroup.UseAggregationTable)
                                        {
                                            queryJoinPart.Append("LEFT ");
                                        }
                                        if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity))
                                        {
                                            if (!String.IsNullOrEmpty(filterGroup.ConnectedEntityProperty))
                                            {
                                                if (!String.IsNullOrEmpty(filterGroup.QueryString))
                                                {
                                                    output.WherePart += $"AND (fi{filterCounter}d.id IS NOT NULL OR queryString .`value`={filterGroup.QueryString.ToMySqlSafeValue(true)})" + Environment.NewLine;
                                                }
                                                else
                                                {
                                                    output.WherePart += $"AND (fi{filterCounter}d.id IS NOT NULL OR filterName.`value`={filterNameFromGroup.ToMySqlSafeValue(true)})" + Environment.NewLine;
                                                }

                                            }
                                            else if (!String.IsNullOrEmpty(filterGroup.QueryString))
                                            {
                                                output.WherePart += $"AND (fi{filterCounter}i.id IS NOT NULL OR queryString .`value`={filterGroup.QueryString.ToMySqlSafeValue(true)})" + Environment.NewLine;
                                            }

                                            else
                                            {
                                                output.WherePart += $"AND (fi{filterCounter}i.id IS NOT NULL OR filterName.`value`={filterNameFromGroup.ToMySqlSafeValue(true)})" + Environment.NewLine;
                                            }

                                        }
                                        else if (!String.IsNullOrEmpty(filterGroup.QueryString))
                                        {
                                            output.WherePart += $"AND (fi{filterCounter}.id IS NOT NULL OR queryString .`value`={filterGroup.QueryString.ToMySqlSafeValue(true)})" + Environment.NewLine;
                                        }

                                        else
                                        {
                                            output.WherePart += $"AND (fi{filterCounter}.id IS NOT NULL OR filterName.`value`={filterNameFromGroup.ToMySqlSafeValue(true)})" + Environment.NewLine;
                                        }

                                    }

                                    if (filterGroup.UseAggregationTable)
                                    {
                                        if (forFilterItemsQuery)
                                        {
                                            if (filterGroup.GetParamKey() != forActiveFilter) //Don't include the JOIN of the filter for which the query - part is requested
                                            {
                                                // Join to table with alias "f" in FilterItemsQuery
                                                queryJoinPart.Append($"JOIN `wiser_filter_aggregation{(string.IsNullOrEmpty(languagesService.CurrentLanguageCode) ? "" : "_" + languagesService.CurrentLanguageCode)}` f{filterCounter} ON f{filterCounter}.category_id=f.category_id AND f{filterCounter}.product_id=f.product_id ");
                                            }
                                        }
                                        else
                                        {
                                            // Join to product-part and category-part given to function (from variable in overview query)
                                            queryJoinPart.Append($"JOIN `wiser_filter_aggregation{(string.IsNullOrEmpty(languagesService.CurrentLanguageCode) ? "" : "_" + languagesService.CurrentLanguageCode)}` f{filterCounter} ON f{filterCounter}.category_id={categoryJoinPart} AND f{filterCounter}.product_id={productJoinPart} ");
                                        }

                                    }
                                    else if (filterNameFromGroup == "itemtitle")
                                    {
                                        queryJoinPart.Append($"JOIN {WiserTableNames.WiserItem} fi{filterCounter} ON fi{filterCounter}.id = {filterConnectionPart} ");
                                    }
                                    else if (filterConnectionPartIsLinkType)
                                    {
                                        if (filterGroup.IsMultiLanguage)
                                        {
                                            queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemLinkDetail} fi{filterCounter} ON (fi{filterCounter}.language_code = '{languagesService.CurrentLanguageCode}' OR fi{filterCounter}.language_code = '') AND fi{filterCounter}.itemlink_id = {filterConnectionPart} ");
                                        }
                                        else
                                        {
                                            queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemLinkDetail} fi{filterCounter} ON fi{filterCounter}.itemlink_id = {filterConnectionPart} ");
                                        }

                                    }
                                    else if (!string.IsNullOrEmpty(filterGroup.ConnectedEntityLinkType))
                                    {
                                        queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemLink} fi{filterCounter}l ON fi{filterCounter}l.destination_item_id = {filterConnectionPart} "); // AND fi{filterCounter}l.type=800
                                    }
                                    else if (filterGroup.IsMultiLanguage)
                                    {
                                        queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemDetail} fi{filterCounter} ON (fi{filterCounter}.language_code = '{languagesService.CurrentLanguageCode}' OR fi{filterCounter}.language_code = '') AND fi{filterCounter}.item_id = {filterConnectionPart} ");
                                    }
                                    else
                                    {
                                        queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemDetail} fi{filterCounter} ON fi{filterCounter}.item_id = {filterConnectionPart} ");
                                    }
                                }
                            }

                            if (!isAdvancedFilter)
                            {
                                var joinPart = "";
                                if (filterGroup != null && ((!string.IsNullOrEmpty(filterGroup.ConnectedEntityLinkType) && !filterGroup.UseAggregationTable) || (filterGroup.UseAggregationTable && forFilterItemsQuery && (filterGroup.GetParamKey() == forActiveFilter)) == false))
                                {
                                    joinPart = AppendFilterJoinPart(filterCounter, filterNameFromGroup, filterValue, filterGroup, false, filterGroup.UseAggregationTable);
                                    if (joinPart != "")
                                    {
                                        queryJoinPart.Append("AND " + joinPart);
                                    }
                                }

                                queryJoinPart.AppendLine();

                                // Handle join and join part if detail value is item-id
                                if (filterGroup != null && !string.IsNullOrEmpty(filterGroup.ConnectedEntity) && !filterGroup.UseAggregationTable)
                                {
                                    if (!filterGroup.IsGroupFilter && forFilterItemsQuery)
                                    {
                                        queryJoinPart.Append("LEFT ");
                                    }

                                    queryJoinPart.Append($"JOIN {WiserTableNames.WiserItem} fi{filterCounter}i ON fi{filterCounter}i.entity_type={filterGroup.ConnectedEntity.ToMySqlSafeValue(true)} ");

                                    if (!string.IsNullOrEmpty(filterGroup.ConnectedEntityLinkType))
                                    {
                                        queryJoinPart.Append($"AND fi{filterCounter}i.id=fi{filterCounter}l.item_id ");
                                    }
                                    else if (filterGroup.SingleConnectedItem)
                                    {
                                        queryJoinPart.Append($"AND fi{filterCounter}i.id=fi{filterCounter}.`value` "); // For singleselect inputtypes, single id in wiser_itemdetail
                                    }
                                    else
                                    {
                                        queryJoinPart.Append($"AND FIND_IN_SET(fi{filterCounter}i.id, fi{filterCounter}.`value`) ");// For multiselect inputtypes, multiple id's in wiser_itemdetail
                                    }

                                    if (!String.IsNullOrEmpty(filterGroup.ConnectedEntityProperty))
                                    {
                                        if (!filterGroup.IsGroupFilter && forFilterItemsQuery)
                                        {
                                            queryJoinPart.Append("LEFT ");
                                        }

                                        queryJoinPart.Append($"JOIN {WiserTableNames.WiserItemDetail} fi{filterCounter}d ON fi{filterCounter}d.item_id=fi{filterCounter}i.id AND fi{filterCounter}d.`key`='{filterGroup.ConnectedEntityProperty.ToMySqlSafeValue(false)}{(filterGroup.FilterOnSeoValue ? "_SEO" : "")}' ");
                                        if (filterGroup.IsMultiLanguage)
                                        {
                                            queryJoinPart.Append($"AND fi{filterCounter}d.language_code='{languagesService.CurrentLanguageCode}' ");
                                        }
                                        else
                                        {
                                            queryJoinPart.Append($"AND fi{filterCounter}d.language_code='' ");
                                        }
                                    }

                                    joinPart = AppendFilterJoinPart(filterCounter, filterNameFromGroup, filterValue, filterGroup, true);
                                    if (joinPart != "")
                                    {
                                        queryJoinPart.Append("AND " + joinPart);
                                    }
                                    queryJoinPart.AppendLine();
                                }

                                filterCounter += 1;
                            }

                            if (filterCount == 0)
                            {
                                filterCount = 1;
                            }
                        }
                    }

                    output.JoinPart = queryJoinPart.ToString();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error on GetFilterQueryPart. Message: {ex}");
                }

                return output;

            }
            catch (Exception exception)
            {
                logger.LogError($"Filter - Exception occurred in GetFilterQueryPart: {exception}");
                return new QueryPartModel();
            }
        }

        /// <inheritdoc />
        public Dictionary<string, string> GetFiltersByParameter(string filterParameter)
        {
            var filters = new Dictionary<string, string>();
            var filterParameterRequest = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, filterParameter);

            if (String.IsNullOrEmpty(filterParameterRequest))
            {
                return filters;
            }

            foreach (var filter in filterParameterRequest.Split("/"))
            {
                if (String.IsNullOrEmpty(filter))
                {
                    continue;
                }


                if (filter.Contains("-"))
                {
                    filters.Add(filter.Split("-")[0], filter.Substring(filter.IndexOf('-') + 1));
                }
                else
                {
                    filters.Add(filter, "");
                }
            }

            return filters;
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, FilterGroup>> GetFilterGroupsAsync(ulong categoryId = 0, string extraFilterProperties = "")
        {
            var languageCode = await languagesService.GetLanguageCodeAsync();
            var result = new Dictionary<string, FilterGroup>(StringComparer.OrdinalIgnoreCase);
            var filterGroupConnectionPart = await objectsService.FindSystemObjectByDomainNameAsync("filtergroupconnectionpart");
            var filtersToItemType = Int32.Parse(await objectsService.FindSystemObjectByDomainNameAsync("filtertoitemtype", "6001"));
            var joinFiltersToItemPart = "";
            var orderingPart = "";

            logger.LogTrace("1 - Filter connection: " + filterGroupConnectionPart);
            logger.LogTrace("1 - categoryId: " + categoryId.ToString());

            var w2FiltersQuery = $@"SELECT 
	                            IFNULL(NULLIF(name.`value`,''),filters.title) AS filtername,
                                property.`value` AS filternameseo,                                    
                                filtergroupname.`value` AS filtergroupnameseo,
	                            filtertype.`value` AS filtertype,
                                showcount.`value` AS showcount,
                                columnname.`value` AS columnname,
                                dependson.`value` AS dependson,
                                dependsontext.`value` AS dependsontext,
                                dependsonvalue.`value` AS dependsonvalue,
                                classes.`value` AS classes,
                                entity.`value` AS entity,
                                matchvalue.`value` AS matchvalue,
                                IFNULL(NULLIF(advancedfilter.`value`,''),advancedfilter.`long_value`) AS advancedfilter,
                                IFNULL(NULLIF(customjoin.`value`,''),customjoin.`long_value`) AS customjoin,                                    
                                customselect.`value` AS customselect,
                                `group`.`value` AS `group`,
                                connectedentity.`value` AS connectedentity,
                                connectedentityproperty.`value` AS connectedentityproperty,
                                connectedentitylinktype.`value` AS connectedentitylinktype,
                                ismultilanguage.`value` AS ismultilanguage,
                                querystring.`value` AS querystring,
                                IFNULL(hideinsummary.`value`, '0') AS hideinsummary,
                                IFNULL(filteronseovalue.`value`, '0') AS filteronseovalue,
                                IFNULL(singleconnecteditem.`value`, '0') AS singleconnecteditem,
                                IFNULL(minimumitemsrequired.`value`, '0') AS minimumitemsrequired,
                                IFNULL(useaggregationtable.`value`, '0') AS useaggregationtable
                                {{selectPart}}
                            FROM {WiserTableNames.WiserItem} filters
                            {{joinFiltersToItem}}
                            JOIN {WiserTableNames.WiserItemDetail} filtertype ON filtertype.item_id=filters.id AND filtertype.`key`='filtertype' {GetLanguageQueryPart("filtertype", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} property ON property.item_id=filters.id AND property.`key`='filtername' {GetLanguageQueryPart("property", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} name ON name.item_id=filters.id AND name.`key`='name' {GetLanguageQueryPart("name", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} filtergroupname ON filtergroupname.item_id=filters.id AND filtergroupname.`key`='filtergroupname' {GetLanguageQueryPart("filtergroupname", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} showcount ON showcount.item_id=filters.id AND showcount.`key`='showcount' {GetLanguageQueryPart("showcount", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} columnname ON columnname.item_id=filters.id AND columnname.`key`='columnname' {GetLanguageQueryPart("columnname", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} dependson ON dependson.item_id=filters.id AND dependson.`key`='dependson' {GetLanguageQueryPart("dependson", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} dependsontext ON dependsontext.item_id=filters.id AND dependsontext.`key`='dependsontext' {GetLanguageQueryPart("dependsontext", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} dependsonvalue ON dependsonvalue.item_id=filters.id AND dependsonvalue.`key`='dependsonvalue' {GetLanguageQueryPart("dependsonvalue", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} classes ON classes.item_id=filters.id AND classes.`key`='classes' {GetLanguageQueryPart("classes", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} entity ON entity.item_id=filters.id AND entity.`key`='entity' {GetLanguageQueryPart("entity", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} matchvalue ON matchvalue.item_id=filters.id AND matchvalue.`key`='matchvalue' {GetLanguageQueryPart("matchvalue", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} advancedfilter ON advancedfilter.item_id=filters.id AND advancedfilter.`key`='advancedfilter' {GetLanguageQueryPart("advancedfilter", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} customjoin ON customjoin.item_id=filters.id AND customjoin.`key`='customjoin' {GetLanguageQueryPart("customjoin", languageCode)}                                                            
                            LEFT JOIN {WiserTableNames.WiserItemDetail} customselect ON customselect.item_id=filters.id AND customselect.`key`='customselect' {GetLanguageQueryPart("customselect", languageCode)}                            
                            LEFT JOIN {WiserTableNames.WiserItemDetail} `group` ON `group`.item_id=filters.id AND `group`.`key`='group' {GetLanguageQueryPart("group", languageCode)}                            
                            LEFT JOIN {WiserTableNames.WiserItemDetail} connectedentity ON connectedentity.item_id=filters.id AND connectedentity.`key`='connectedentity' {GetLanguageQueryPart("connectedentity", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} connectedentityproperty ON connectedentityproperty.item_id=filters.id AND connectedentityproperty.`key`='connectedentityproperty' {GetLanguageQueryPart("connectedentityproperty", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} connectedentitylinktype ON connectedentitylinktype.item_id=filters.id AND connectedentitylinktype.`key`='connectedentitylinktype' {GetLanguageQueryPart("connectedentitylinktype", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} ismultilanguage ON ismultilanguage.item_id=filters.id AND ismultilanguage.`key`='ismultilanguage' {GetLanguageQueryPart("ismultilanguage", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} querystring ON querystring.item_id=filters.id AND querystring.`key`='querystring' {GetLanguageQueryPart("querystring", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} hideinsummary ON hideinsummary.item_id=filters.id AND hideinsummary.`key`='hideinsummary' {GetLanguageQueryPart("hideinsummary", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} filteronseovalue ON filteronseovalue.item_id=filters.id AND filteronseovalue.`key`='filteronseovalue' {GetLanguageQueryPart("filteronseovalue", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} singleconnecteditem ON singleconnecteditem.item_id=filters.id AND singleconnecteditem.`key`='singleconnecteditem' {GetLanguageQueryPart("singleconnecteditem", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} minimumitemsrequired ON minimumitemsrequired.item_id=filters.id AND minimumitemsrequired.`key`='minimumitemsrequired' {GetLanguageQueryPart("minimumitemsrequired", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} useaggregationtable ON useaggregationtable.item_id=filters.id AND useaggregationtable.`key`='useaggregationtable' {GetLanguageQueryPart("useaggregationtable", languageCode)}
                            WHERE filters.entity_type='filter' {{levelsWherePart}}
                            {{ordering}}";

            // Add extra joins and select-parts if extra properties are necessary for use in template
            if (!String.IsNullOrEmpty(extraFilterProperties))
            {
                var selectPart = ",";
                var joinPart = "";
                foreach (var p in extraFilterProperties.Split(","))
                {
                    var prop = p.Trim();
                    if (!String.IsNullOrEmpty(prop))
                    {
                        selectPart += $"`{prop}`.`value` AS `{prop}`,";
                        joinPart += $"LEFT JOIN {WiserTableNames.WiserItemDetail} `{prop}` ON `{prop}`.item_id=filters.id AND `{prop}`.`key`='{prop}' {GetLanguageQueryPart(prop, languageCode)}" + Environment.NewLine;
                    }
                }
                w2FiltersQuery = w2FiltersQuery.Replace("{selectPart}", selectPart.TrimEnd(','));
                w2FiltersQuery = w2FiltersQuery.Replace("{joinFiltersToItem}", joinPart + " {joinFiltersToItem}");
            }
            else
            {
                w2FiltersQuery = w2FiltersQuery.Replace("{selectPart}", "");
            }

            databaseConnection.AddParameter("lang_id", languageCode);
            databaseConnection.AddParameter("category_id", categoryId > 0 ? categoryId : 0);
            databaseConnection.AddParameter("filtertoitemtype", filtersToItemType);

            joinFiltersToItemPart = $"LEFT JOIN {WiserTableNames.WiserItemLink} filterstoitem ON filterstoitem.item_id=filters.id AND filterstoitem.type=?filtertoitemtype AND filterstoitem.destination_item_id=?category_id ";
            joinFiltersToItemPart += $"LEFT JOIN {WiserTableNames.WiserItemLink} filterstoparent ON filterstoparent.item_id=filters.id AND filterstoparent.type=1 ";
            orderingPart = "ORDER BY filterstoitem.ordering,filterstoparent.ordering";

            var dataTable = await databaseConnection.GetAsync(w2FiltersQuery.Replace("{levelsWherePart}", "").Replace("{ordering}", orderingPart).Replace("{joinFiltersToItem}", joinFiltersToItemPart));

            foreach (DataRow row in dataTable.Rows)
            {
                FilterGroup f;
                if (dataTable.Columns.Contains("filtergroupnameseo") && !String.IsNullOrEmpty(row["filtergroupnameseo"].ToString()))
                {
                    if (!String.IsNullOrEmpty(row["filtergroupnameseo"].ToString()))
                    {
                        f = new FilterGroup(row["filtername"].ToString(), row["filtergroupnameseo"].ToString());
                    }
                    else
                    {
                        f = new FilterGroup(row["filtername"].ToString());
                    }
                    f.IsGroupFilter = true;
                }
                else if (!String.IsNullOrEmpty(row["filternameseo"].ToString()))
                {
                    f = new FilterGroup(row["filtername"].ToString(), row["filternameseo"].ToString());
                }
                else
                {
                    f = new FilterGroup(row["filtername"].ToString());
                }

                if (row["filtertype"].ToString().All(Char.IsNumber))
                {
                    f.FilterType = (FilterGroup.FilterGroupType)Convert.ToInt16(row["filtertype"]);
                }
                else
                {
                    f.FilterType = (FilterGroup.FilterGroupType)Enum.Parse(typeof(FilterGroup.FilterGroupType), row["filtertype"].ToString());
                }

                f.ShowCount = !row.IsNull("showcount") && Convert.ToBoolean(row["showcount"]);
                f.HideInSummary = row.Field<string>("hideinsummary") == "1";
                f.FilterOnSeoValue = row.Field<string>("filteronseovalue") == "1";
                f.SingleConnectedItem = row.Field<string>("singleconnecteditem") == "1";
                if (row["columnname"].ToString() == "")
                {
                    f.ColumnName = f.Name;
                }
                else
                {
                    f.ColumnName = row["columnname"].ToString();
                }

                // The "classes" columns was added later, so check for its availability
                if (dataTable.Columns.Contains("classes") && row["classes"] != DBNull.Value && !String.IsNullOrEmpty(row.Field<string>("classes")))
                {
                    f.Classes = row.Field<string>("classes") ?? "";
                }

                // The "entity" columns property added later, so check for its availability
                if (dataTable.Columns.Contains("entity") && row["entity"] != DBNull.Value && !String.IsNullOrEmpty(row.Field<string>("entity")))
                {
                    f.EntityName = row.Field<string>("entity");
                }

                // Match value can be used in combination with Wiser 2 group filters to give the value on which the detail must match when the filter is selected
                if (dataTable.Columns.Contains("matchvalue") && !String.IsNullOrEmpty(row["matchvalue"].ToString()))
                {
                    f.MatchValue = row["matchvalue"].ToString();
                }

                // Advanced filter to give values and join statements
                if (dataTable.Columns.Contains("advancedfilter") && !String.IsNullOrEmpty(row["advancedfilter"].ToString()))
                {
                    f.AdvancedFilter = row["advancedfilter"].ToString();
                }

                if (dataTable.Columns.Contains("customjoin") && !String.IsNullOrEmpty(row["customjoin"].ToString()))
                {
                    f.CustomJoin = row["customjoin"].ToString();
                }

                if (dataTable.Columns.Contains("customselect") && !String.IsNullOrEmpty(row["customselect"].ToString()))
                {
                    f.CustomSelect = row["customselect"].ToString();
                }

                if (dataTable.Columns.Contains("group") && !String.IsNullOrEmpty(row["group"].ToString()))
                {
                    f.Group = row["group"].ToString();
                }

                if (dataTable.Columns.Contains("connectedentity") && !String.IsNullOrEmpty(row["connectedentity"].ToString()))
                {
                    f.ConnectedEntity = row["connectedentity"].ToString();
                }

                if (dataTable.Columns.Contains("connectedentityproperty") && !String.IsNullOrEmpty(row["connectedentityproperty"].ToString()))
                {
                    f.ConnectedEntityProperty = row["connectedentityproperty"].ToString();
                }

                if (dataTable.Columns.Contains("connectedentitylinktype") && !String.IsNullOrEmpty(row["connectedentitylinktype"].ToString()))
                {
                    f.ConnectedEntityLinkType = row["connectedentitylinktype"].ToString();
                }

                if (dataTable.Columns.Contains("ismultilanguage") && !String.IsNullOrEmpty(row["ismultilanguage"].ToString()))
                {
                    f.IsMultiLanguage = row["ismultilanguage"].ToString() == "1";
                }

                if (dataTable.Columns.Contains("querystring") && !String.IsNullOrEmpty(row["querystring"].ToString()))
                {
                    f.QueryString = row["querystring"].ToString();
                }

                if (dataTable.Columns.Contains("minimumitemsrequired") && !String.IsNullOrWhiteSpace(row["minimumitemsrequired"].ToString()))
                {
                    f.MinimumItemsRequired = Int32.TryParse(row["minimumitemsrequired"].ToString(), out var tempIntValue) ? tempIntValue : 0;
                }

                if (dataTable.Columns.Contains("useaggregationtable") && !String.IsNullOrEmpty(row["useaggregationtable"].ToString()))
                {
                    f.UseAggregationTable = row["useaggregationtable"].ToString() == "1";
                }

                // Extra properties on filter level for use in templates
                if (!String.IsNullOrEmpty(extraFilterProperties))
                {
                    f.ExtraProperties = new SortedList<string, string>();
                    foreach (var p in extraFilterProperties.Split(","))
                    {
                        var prop = p.Trim();
                        if (!String.IsNullOrEmpty(prop))
                        {
                            if (!f.ExtraProperties.ContainsKey(prop))
                            {
                                f.ExtraProperties.Add(prop, row[prop].ToString());
                            }
                        }
                    }
                }

                if (!String.IsNullOrEmpty(f.QueryString))
                {
                    if (!result.ContainsKey(f.QueryString))
                    {
                        result.Add(f.QueryString, f);
                    }
                }
                else
                {
                    if (!result.ContainsKey(f.NameSeo))
                    {
                        result.Add(f.NameSeo, f);
                    }
                }
            }


            return result;
        }

        private string GetLanguageQueryPart(string columnName, string languageCode)
        {
            if (languageCode == "" || languageCode == "0")
            {
                return "AND `" + columnName + "`.language_code=''";
            }

            return "AND (`" + columnName + "`.language_code='' OR `" + columnName + "`.language_code='" + languageCode + "')";
        }

        private string AppendFilterJoinPart(int filterCounter, string filterName, string filterValue, FilterGroup filterGroup, bool forItemPart, bool forAggregationTable = false)
        {
            if (filterGroup is null)
            {
                return String.Empty;
            }

            var output = "";
            const string ValueSplit = ",";

            if (filterGroup.FilterType == FilterGroup.FilterGroupType.Slider)
            {
                if (forAggregationTable)
                {
                    if (Information.IsNumeric(filterValue)) // One (minimum) value
                    {
                        output = $"f{filterCounter}.filtergroup = {filterGroup.GetParamKey().ToMySqlSafeValue(true)} AND f{filterCounter}.filtervalue < {filterValue.ToMySqlSafeValue(true)}";
                    }
                    else
                    {
                        output = $"f{filterCounter}.filtergroup = {filterGroup.GetParamKey().ToMySqlSafeValue(true)} AND f{filterCounter}.filtervalue >= {filterValue.Split('-')[0].ToMySqlSafeValue(false)} AND f{filterCounter}.filtervalue <= {filterValue.Split('-')[1].ToMySqlSafeValue(false)}";
                    }
                }
                else
                {
                    if (Information.IsNumeric(filterValue)) // One (minimum) value
                    {
                        output = $"(fi{filterCounter}.`key` = {filterName.ToMySqlSafeValue(true)} AND REPLACE(fi{filterCounter}.`value`,',','.') < {filterValue.ToMySqlSafeValue(true)})";
                    }
                    else if (filterValue.Contains("-") && Information.IsNumeric(filterValue.Split('-')[0]) && Information.IsNumeric(filterValue.Split('-')[1])) // Two values (min and max)
                    {
                        output = $"(fi{filterCounter}.`key` = {filterName.ToMySqlSafeValue(true)} AND fi{filterCounter}.`value` >= {filterValue.Split('-')[0].ToMySqlSafeValue(true)} AND fi{filterCounter}.`value` <= {filterValue.Split('-')[1].ToMySqlSafeValue(true)})";
                    }
                }
            }
            else if (!String.IsNullOrWhiteSpace(filterValue))
            {
                // Get the value query part (in case of a Wiser 2 group filter)
                var valueQueryPart = $"AND fi{filterCounter}.`value`<>'' AND fi{filterCounter}.`value`<>'0'";
                if (filterGroup.IsGroupFilter)
                {
                    if (!String.IsNullOrEmpty(filterGroup.MatchValue))
                    {
                        if (filterGroup.MatchValue.Contains("~"))
                        {
                            valueQueryPart = $"AND fi{filterCounter}.`value` IN ({filterGroup.MatchValue.ToMySqlSafeValue(true).Replace("~", "','")})";
                        }
                        else
                        {
                            valueQueryPart = $"AND fi{filterCounter}.`value`={filterGroup.MatchValue.ToMySqlSafeValue(true)}";
                        }
                    }
                }

                if (filterValue.Contains(ValueSplit))
                {
                    // multiple values selected
                    if (forAggregationTable)
                    {
                        output = $"f{filterCounter}.filtergroup={filterGroup.GetParamKey().ToMySqlSafeValue(true)} AND f{filterCounter}.filtervalue IN ({filterValue.ToMySqlSafeValue(true).Replace(ValueSplit, "','")})";
                    }
                    else if (filterGroup.IsGroupFilter)
                    {
                        output = $"(fi{filterCounter}.groupname = {filterName.ToMySqlSafeValue(true)} AND  (fi{filterCounter}.`key` IN ({filterValue.ToMySqlSafeValue(true).Replace(ValueSplit, "','")}) {valueQueryPart}))";
                    }
                    else if (filterName == "itemtitle")
                    {
                        output = $"(fi{filterCounter}.title IN ({filterValue.ToMySqlSafeValue(true).Replace(ValueSplit, "','")}))";
                    }
                    else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity) && forItemPart)
                    {
                        if (String.IsNullOrEmpty(filterGroup.ConnectedEntityProperty))
                        {
                            output = $"(fi{filterCounter}i.title IN ({filterValue.ToMySqlSafeValue(true).Replace(ValueSplit, "','")}))";
                        }
                        else
                        {
                            output = $"(fi{filterCounter}d.`value` IN ({filterValue.ToMySqlSafeValue(true).Replace(ValueSplit, "','")}))";
                        }
                    }
                    else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity) & !forItemPart)
                    {
                        output = $"(fi{filterCounter}.`key` = CONCAT('{filterName.ToMySqlSafeValue(true)}, '{(filterGroup.FilterOnSeoValue ? "_SEO" : "")}'))";
                    }
                    else
                    {
                        output = $"(fi{filterCounter}.`key` = CONCAT('{filterName.ToMySqlSafeValue(true)}, '{(filterGroup.FilterOnSeoValue ? "_SEO" : "")}') AND (fi{filterCounter}.`value` IN ({filterValue.ToMySqlSafeValue(true).Replace(ValueSplit, "','")})))";
                    }
                }
                else // single value selected
                {
                    if (forAggregationTable)
                    {
                        output = $"f{filterCounter}.filtergroup={filterGroup.GetParamKey().ToMySqlSafeValue(true)} AND f{filterCounter}.filtervalue={filterValue.ToMySqlSafeValue(true)}";
                    }
                    else if (filterGroup.IsGroupFilter)
                    {
                        output = $"(fi{filterCounter}.groupname = {filterName.ToMySqlSafeValue(true)} AND (fi{filterCounter}.`key` = {filterValue.ToMySqlSafeValue(true)} {valueQueryPart}))";
                    }
                    else if (filterName == "itemtitle")
                    {
                        output = $"(fi{filterCounter}.title = {filterValue.ToMySqlSafeValue(true)})";
                    }
                    else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity) & forItemPart)
                    {
                        if (String.IsNullOrEmpty(filterGroup.ConnectedEntityProperty))
                        {
                            output = $"(fi{filterCounter}i.title = {filterValue.ToMySqlSafeValue(true)})";
                        }
                        else
                        {
                            output = $"(fi{filterCounter}d.`value` = {filterValue.ToMySqlSafeValue(true)})";
                        }
                    }
                    else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity) & !forItemPart)
                    {
                        output = $"(fi{filterCounter}.`key` = CONCAT({filterName.ToMySqlSafeValue(true)}, '{(filterGroup.FilterOnSeoValue ? "_SEO" : "")}')";
                    }
                    else
                    {
                        // if the filter name is genericFilter dont add it to the join statement, we want all items with the name, but there is no group
                        var filterNamePart = filterName == "genericFilter" ? "TRUE" : $"fi{filterCounter}.`key` = CONCAT({filterName.ToMySqlSafeValue(true)}, '{(filterGroup.FilterOnSeoValue ? "_SEO" : "")}')  ";
                        output = $"({filterNamePart} AND (fi{filterCounter}.`value` = {filterValue.ToMySqlSafeValue(true)}))";
                    }
                }
            }

            return output;
        }
    }
}
