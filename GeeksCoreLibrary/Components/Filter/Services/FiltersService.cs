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
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Constants = GeeksCoreLibrary.Components.Filter.Models.Constants;

namespace GeeksCoreLibrary.Components.Filter.Services
{
    public class FiltersService : IFiltersService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly ILogger<FiltersService> logger;
        private readonly ILanguagesService languagesService;
        private readonly IEntityTypesService entityTypesService;

        public FiltersService(IDatabaseConnection databaseConnection,
            IObjectsService objectsService,
            ILogger<FiltersService> logger,
            ILanguagesService languagesService,
            IEntityTypesService entityTypesService,
            IHttpContextAccessor httpContextAccessor = null)
        {
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.logger = logger;
            this.languagesService = languagesService;
            this.entityTypesService = entityTypesService;
        }

        /// <inheritdoc />
        public async Task<QueryPartModel> GetFilterQueryPartAsync(bool forFilterItemsQuery = false, Dictionary<string, FilterGroup> givenFilterGroups = null, string productJoinPart = "", string categoryJoinPart = "", string forActiveFilter = "")
        {
            var httpContext = httpContextAccessor?.HttpContext;

            if (httpContext == null)
            {
                throw new Exception("HttpContext is null.");
            }

            try
            {
                var output = new QueryPartModel();
                var filters = new SortedList<string, string>();
                var filterParameter = await objectsService.FindSystemObjectByDomainNameAsync("filterparameterwiser2", defaultResult: "filterstring");
                var filterParameterMixedMode = (await objectsService.FindSystemObjectByDomainNameAsync("filterparametermixedmodewiser2", defaultResult: "0")).Equals("1");
                var filterParametersToExclude = (await objectsService.FindSystemObjectByDomainNameAsync("filterparameterstoexclude", defaultResult: "templateid,pagenr,gclid,_ga")).Split(",").ToList();

                // Make sure that the language code is filled.
                if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
                {
                    // This function fills the property "CurrentLanguageCode".
                    await languagesService.GetLanguageCodeAsync();
                }

                databaseConnection.AddParameter("sql_currentLanguageCode", languagesService.CurrentLanguageCode);

                // Get a list of filters from the URL
                if (!String.IsNullOrEmpty(filterParameter))
                {
                    foreach (var item in GetFiltersByParameter(filterParameter))
                    {
                        filters.Add(item.Key, item.Value);
                    }
                }

                if (String.IsNullOrEmpty(filterParameter) || filterParameterMixedMode)
                {
                    foreach (var key in httpContext.Request.Query.Keys)
                    {
                        if (String.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        if (filters.TryGetValue(key, out var filter))
                        {
                            if (!String.IsNullOrEmpty(filter))
                            {
                                // Skip if already defined in case of mixed mode
                                continue;
                            }
                        }

                        if (!filterParametersToExclude.Contains(key, StringComparer.OrdinalIgnoreCase))
                        {
                            filters.Add(key, httpContext.Request.Query[key]);
                        }
                    }
                }

                try
                {
                    Dictionary<string, FilterGroup> filterGroups;
                    var filterConnectionPart = await objectsService.FindSystemObjectByDomainNameAsync("filterconnectionpart", "i.id");
                    var filterConnectionParts = new List<FilterConnectionPart>();

                    if (givenFilterGroups is {Count: > 0})
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
                        foreach (var filterGroup in filterGroups.Where(filterGroup => !String.IsNullOrEmpty(filterGroup.Value.CustomJoin)).Where(filterGroup => !filters.ContainsKey(filterGroup.Value.NameSeo)))
                        {
                            // Abuse value to force LEFT JOIN.
                            filters.Add(filterGroup.Value.NameSeo, "LEFT JOIN");
                        }
                    }

                    // Add the different entity names with the corresponding connection parts to the list.
                    if (filterConnectionPart.Contains(';'))
                    {
                        foreach (var value in filterConnectionPart.Split('~'))
                        {
                            var splitValue = value.Split(';');
                            filterConnectionParts.Add(splitValue.Length > 2
                                ? new FilterConnectionPart(splitValue[0], splitValue[1], String.Equals(splitValue[2], "linkdetail", StringComparison.OrdinalIgnoreCase))
                                : new FilterConnectionPart(splitValue[0], splitValue[1], false));
                        }
                    }

                    // Build JOINS.
                    var filterCounter = 0;
                    var filterCount = 0;
                    foreach (var filterName in filters.Keys)
                    {
                        if (filterName.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (String.IsNullOrEmpty(filterParameter) && filterParametersToExclude.Contains(filterName, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var filterNameFromGroup = filterName;
                        var filterValues = filters[filterName];
                        var isAdvancedFilter = false;

                        FilterGroup filterGroup = null;
                        if (filterGroups != null && filterGroups.TryGetValue(filterName, out var value))
                        {
                            filterGroup = value;
                            filterNameFromGroup = filterGroup.NameSeo;
                        }

                        if (filterGroup == null || String.IsNullOrEmpty(filterValues))
                        {
                            continue;
                        }

                        var tablePrefix = await entityTypesService.GetTablePrefixForEntityAsync(filterGroup.EntityName);
                        var connectedEntityTablePrefix = await entityTypesService.GetTablePrefixForEntityAsync(filterGroup.ConnectedEntity);

                        if (!String.IsNullOrEmpty(filterGroup.AdvancedFilter))
                        {
                            foreach (var filter in filterGroup.GetAdvancedFilters.Where(filter => filterValues.Split(Constants.ValueSplitter).Contains(filter.Key, StringComparer.OrdinalIgnoreCase)))
                            {
                                output.JoinPart.AppendLine(filter.Value);
                            }

                            isAdvancedFilter = true;
                        }
                        else if (!String.IsNullOrEmpty(filterGroup.CustomJoin))
                        {
                            if (forFilterItemsQuery)
                            {
                                if (filterValues == "LEFT JOIN")
                                {
                                    output.JoinPart.Append("LEFT ");
                                }

                                if (!String.IsNullOrEmpty(filterGroup.CustomSelect))
                                {
                                    var customSelectSplit = filterGroup.CustomSelect.Split("{select}");
                                    output.SelectPartStart.Append(customSelectSplit[0]);
                                    if (customSelectSplit.Length > 1)
                                    {
                                        output.SelectPartEnd.Append(filterGroup.CustomSelect.Split("{select}")[1]);
                                    }
                                }
                            }
                            output.JoinPart.AppendLine(filterGroup.CustomJoin);
                            isAdvancedFilter = true;
                        }
                        else
                        {
                            var filterConnectionPartIsLinkType = false;
                            var filterConnectionPartObject = filterConnectionParts.FirstOrDefault(x => x.TypeName == filterGroup.EntityName);
                            if (filterConnectionPartObject != null)
                            {
                                filterConnectionPart = filterConnectionPartObject.JoinPart;
                                filterConnectionPartIsLinkType = filterConnectionPartObject.IsLinkType;
                            }

                            if (filterGroup.IsGroupFilter && filterValues.Contains(Constants.ValueSplitter))
                            {
                                foreach (var filterValue in filterValues.Split(Constants.ValueSplitter))
                                {
                                    if (!filterGroup.IsGroupFilter && forFilterItemsQuery)
                                    {
                                        output.JoinPart.Append("LEFT ");

                                        if (!String.IsNullOrEmpty(filterGroup.QueryString))
                                        {
                                            output.WherePart.Append($"AND (fi{filterCounter}.id IS NOT NULL OR queryString .`value` = {filterGroup.QueryString.ToMySqlSafeValue(true)}){Environment.NewLine}");
                                        }
                                        else
                                        {
                                            output.WherePart.Append($"AND (fi{filterCounter}.id IS NOT NULL OR filterName.`value`={filterNameFromGroup.ToMySqlSafeValue(true)}){Environment.NewLine}");
                                        }
                                    }

                                    if (filterConnectionPartIsLinkType)
                                    {
                                        if (filterGroup.IsMultiLanguage)
                                        {
                                            output.JoinPart.Append($"JOIN {WiserTableNames.WiserItemLinkDetail} fi{filterCounter} ON (fi{filterCounter}.language_code = ?sql_currentLanguageCode OR fi{filterCounter}.language_code = '') AND fi{filterCounter}.itemlink_id = {filterConnectionPart} ");
                                        }
                                        else
                                        {
                                            output.JoinPart.Append($"JOIN {WiserTableNames.WiserItemLinkDetail} fi{filterCounter} ON fi{filterCounter}.itemlink_id = {filterConnectionPart} ");
                                        }
                                    }
                                    else if (filterGroup.IsMultiLanguage)
                                    {
                                        output.JoinPart.Append($"JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} fi{filterCounter} ON (fi{filterCounter}.language_code = ?sql_currentLanguageCode OR fi{filterCounter}.language_code = '') AND fi{filterCounter}.item_id = {filterConnectionPart} ");
                                    }
                                    else
                                    {
                                        output.JoinPart.Append($"JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} fi{filterCounter} ON fi{filterCounter}.item_id = {filterConnectionPart} ");
                                    }

                                    var joinPart = AppendFilterJoinPart(filterCounter, filterNameFromGroup, filterValue, filterGroup, false);
                                    if (joinPart != "")
                                    {
                                        output.JoinPart.Append($"AND {joinPart}");
                                    }
                                    output.JoinPart.AppendLine();

                                    filterCounter++;
                                    // So the AppendFilterJoinPart will not be called and the filterCounter will not be increased.
                                    isAdvancedFilter = true;
                                }
                            }
                            else
                            {
                                if (!filterGroup.IsGroupFilter && forFilterItemsQuery)
                                {
                                    if (!filterGroup.UseAggregationTable)
                                    {
                                        output.JoinPart.Append("LEFT ");
                                    }
                                    if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity))
                                    {
                                        if (!String.IsNullOrEmpty(filterGroup.ConnectedEntityProperty))
                                        {
                                            if (!String.IsNullOrEmpty(filterGroup.QueryString))
                                            {
                                                output.WherePart.Append($"AND (fi{filterCounter}d.id IS NOT NULL OR queryString .`value` = {filterGroup.QueryString.ToMySqlSafeValue(true)}){Environment.NewLine}");
                                            }
                                            else
                                            {
                                                output.WherePart.Append($"AND (fi{filterCounter}d.id IS NOT NULL OR filterName.`value` = {filterNameFromGroup.ToMySqlSafeValue(true)}){Environment.NewLine}");
                                            }

                                        }
                                        else if (!String.IsNullOrEmpty(filterGroup.QueryString))
                                        {
                                            output.WherePart.Append($"AND (fi{filterCounter}i.id IS NOT NULL OR queryString .`value` = {filterGroup.QueryString.ToMySqlSafeValue(true)}){Environment.NewLine}");
                                        }

                                        else
                                        {
                                            output.WherePart.Append($"AND (fi{filterCounter}i.id IS NOT NULL OR filterName.`value` = {filterNameFromGroup.ToMySqlSafeValue(true)}){Environment.NewLine}");
                                        }

                                    }
                                    else if (!String.IsNullOrEmpty(filterGroup.QueryString))
                                    {
                                        output.WherePart.Append($"AND (fi{filterCounter}.id IS NOT NULL OR queryString .`value` = {filterGroup.QueryString.ToMySqlSafeValue(true)}){Environment.NewLine}");
                                    }

                                    else
                                    {
                                        output.WherePart.Append($"AND (fi{filterCounter}.id IS NOT NULL OR filterName.`value` = {filterNameFromGroup.ToMySqlSafeValue(true)}){Environment.NewLine}");
                                    }

                                }

                                if (filterGroup.UseAggregationTable)
                                {
                                    if (forFilterItemsQuery)
                                    {
                                        if (filterGroup.GetParamKey() != forActiveFilter) //Don't include the JOIN of the filter for which the query - part is requested
                                        {
                                            // Join to table with alias "f" in FilterItemsQuery
                                            output.JoinPart.Append($"JOIN `wiser_filter_aggregation{(String.IsNullOrEmpty(languagesService.CurrentLanguageCode) ? "" : $"_{languagesService.CurrentLanguageCode.ToMySqlSafeValue(false)}")}` f{filterCounter} ON f{filterCounter}.category_id = f.category_id AND f{filterCounter}.product_id = f.product_id ");
                                        }
                                    }
                                    else
                                    {
                                        // Join to product-part and category-part given to function (from variable in overview query)
                                        output.JoinPart.Append($"JOIN `wiser_filter_aggregation{(String.IsNullOrEmpty(languagesService.CurrentLanguageCode) ? "" : $"_{languagesService.CurrentLanguageCode.ToMySqlSafeValue(false)}")}` f{filterCounter} ON f{filterCounter}.category_id = {categoryJoinPart} AND f{filterCounter}.product_id = {productJoinPart} ");
                                    }

                                }
                                else if (String.Equals(filterNameFromGroup, "itemtitle", StringComparison.OrdinalIgnoreCase))
                                {
                                    output.JoinPart.Append($"JOIN {tablePrefix}{WiserTableNames.WiserItem} fi{filterCounter} ON fi{filterCounter}.id = {filterConnectionPart} ");
                                }
                                else if (filterConnectionPartIsLinkType)
                                {
                                    if (filterGroup.IsMultiLanguage)
                                    {
                                        output.JoinPart.Append($"JOIN {WiserTableNames.WiserItemLinkDetail} fi{filterCounter} ON (fi{filterCounter}.language_code = ?sql_currentLanguageCode OR fi{filterCounter}.language_code = '') AND fi{filterCounter}.itemlink_id = {filterConnectionPart} ");
                                    }
                                    else
                                    {
                                        output.JoinPart.Append($"JOIN {WiserTableNames.WiserItemLinkDetail} fi{filterCounter} ON fi{filterCounter}.itemlink_id = {filterConnectionPart} ");
                                    }

                                }
                                else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntityLinkType))
                                {
                                    output.JoinPart.Append($"JOIN {WiserTableNames.WiserItemLink} fi{filterCounter}l ON fi{filterCounter}l.destination_item_id = {filterConnectionPart} "); // AND fi{filterCounter}l.type=800
                                }
                                else if (filterGroup.IsMultiLanguage)
                                {
                                    output.JoinPart.Append($"JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} fi{filterCounter} ON (fi{filterCounter}.language_code = ?sql_currentLanguageCode OR fi{filterCounter}.language_code = '') AND fi{filterCounter}.item_id = {filterConnectionPart} ");
                                }
                                else
                                {
                                    output.JoinPart.Append($"JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} fi{filterCounter} ON fi{filterCounter}.item_id = {filterConnectionPart} ");
                                }
                            }
                        }

                        if (!isAdvancedFilter)
                        {
                            string joinPart;
                            if (filterGroup != null && ((!String.IsNullOrEmpty(filterGroup.ConnectedEntityLinkType) && !filterGroup.UseAggregationTable) || (filterGroup.UseAggregationTable && forFilterItemsQuery && (filterGroup.GetParamKey() == forActiveFilter)) == false))
                            {
                                joinPart = AppendFilterJoinPart(filterCounter, filterNameFromGroup, filterValues, filterGroup, false, filterGroup.UseAggregationTable);
                                if (joinPart != "")
                                {
                                    output.JoinPart.Append($"AND {joinPart}");
                                }
                            }

                            output.JoinPart.AppendLine();

                            // Handle join and join part if detail value is item-id.
                            if (filterGroup != null && !String.IsNullOrEmpty(filterGroup.ConnectedEntity) && !filterGroup.UseAggregationTable)
                            {
                                if (!filterGroup.IsGroupFilter && forFilterItemsQuery)
                                {
                                    output.JoinPart.Append("LEFT ");
                                }

                                output.JoinPart.Append($"JOIN {connectedEntityTablePrefix}{WiserTableNames.WiserItem} fi{filterCounter}i ON fi{filterCounter}i.entity_type = {filterGroup.ConnectedEntity.ToMySqlSafeValue(true)} ");

                                if (!String.IsNullOrEmpty(filterGroup.ConnectedEntityLinkType))
                                {
                                    output.JoinPart.Append($"AND fi{filterCounter}i.id = fi{filterCounter}l.item_id ");
                                }
                                else if (filterGroup.SingleConnectedItem)
                                {
                                    // For singleselect inputtypes, single id in wiser_itemdetail.
                                    output.JoinPart.Append($"AND fi{filterCounter}i.id = fi{filterCounter}.`value` ");
                                }
                                else
                                {
                                    // For multiselect inputtypes, multiple id's in wiser_itemdetail.
                                    output.JoinPart.Append($"AND FIND_IN_SET(fi{filterCounter}i.id, fi{filterCounter}.`value`) ");
                                }

                                if (!String.IsNullOrEmpty(filterGroup.ConnectedEntityProperty))
                                {
                                    if (!filterGroup.IsGroupFilter && forFilterItemsQuery)
                                    {
                                        output.JoinPart.Append("LEFT ");
                                    }

                                    output.JoinPart.Append($"JOIN {connectedEntityTablePrefix}{WiserTableNames.WiserItemDetail} fi{filterCounter}d ON fi{filterCounter}d.item_id = fi{filterCounter}i.id AND fi{filterCounter}d.`key` = '{filterGroup.ConnectedEntityProperty.ToMySqlSafeValue(false)}{(filterGroup.FilterOnSeoValue ? "_SEO" : "")}' ");
                                    if (filterGroup.IsMultiLanguage)
                                    {
                                        output.JoinPart.Append($"AND fi{filterCounter}d.language_code = ?sql_currentLanguageCode ");
                                    }
                                    else
                                    {
                                        output.JoinPart.Append($"AND fi{filterCounter}d.language_code = '' ");
                                    }
                                }

                                joinPart = AppendFilterJoinPart(filterCounter, filterNameFromGroup, filterValues, filterGroup, true);
                                if (joinPart != "")
                                {
                                    output.JoinPart.Append($"AND {joinPart}");
                                }
                                output.JoinPart.AppendLine();
                            }

                            filterCounter++;
                        }

                        if (filterCount == 0)
                        {
                            filterCount = 1;
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception($"Error on GetFilterQueryPart. Message: {exception}");
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
            var filterParameterRequest = HttpContextHelpers.GetRequestValue(httpContextAccessor?.HttpContext, filterParameter);

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


                if (filter.Contains('-'))
                {
                    filters.Add(filter.Split("-")[0], filter[(filter.IndexOf('-') + 1)..]);
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
            var filtersToItemType = Int32.Parse(await objectsService.FindSystemObjectByDomainNameAsync("filtertoitemtype", "6001"));

            var w2FiltersQuery = $@"SELECT 
                                filterstoitem.id AS filterstoitemid,   
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
                                IFNULL(useaggregationtable.`value`, '0') AS useaggregationtable,
                                urlregex.`value` AS urlregex,
                                IFNULL(NULLIF(grouptemplate.`value`,''),grouptemplate.`long_value`) AS grouptemplate,
                                IFNULL(NULLIF(itemtemplate.`value`,''),itemtemplate.`long_value`) AS itemtemplate,
                                IFNULL(NULLIF(selecteditemtemplate.`value`,''),selecteditemtemplate.`long_value`) AS selecteditemtemplate
                                {{selectPart}}
                            FROM {WiserTableNames.WiserItem} AS filters
                            {{joinPart}}
                            LEFT JOIN {WiserTableNames.WiserItemLink} AS filterstoitem ON filterstoitem.item_id = filters.id AND filterstoitem.type = ?filtertoitemtype AND filterstoitem.destination_item_id=?category_id
                            LEFT JOIN {WiserTableNames.WiserItemLink} AS filterstoparent ON filterstoparent.item_id = filters.id AND filterstoparent.type = 1
                            JOIN {WiserTableNames.WiserItemDetail} AS filtertype ON filtertype.item_id = filters.id AND filtertype.`key` = 'filtertype' {GetLanguageQueryPart("filtertype", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS property ON property.item_id = filters.id AND property.`key` = 'filtername' {GetLanguageQueryPart("property", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS name ON name.item_id = filters.id AND name.`key` = 'name' {GetLanguageQueryPart("name", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS filtergroupname ON filtergroupname.item_id = filters.id AND filtergroupname.`key` = 'filtergroupname' {GetLanguageQueryPart("filtergroupname", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS showcount ON showcount.item_id = filters.id AND showcount.`key` = 'showcount' {GetLanguageQueryPart("showcount", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS columnname ON columnname.item_id = filters.id AND columnname.`key` = 'columnname' {GetLanguageQueryPart("columnname", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS dependson ON dependson.item_id = filters.id AND dependson.`key` = 'dependson' {GetLanguageQueryPart("dependson", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS dependsontext ON dependsontext.item_id = filters.id AND dependsontext.`key` = 'dependsontext' {GetLanguageQueryPart("dependsontext", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS dependsonvalue ON dependsonvalue.item_id = filters.id AND dependsonvalue.`key` = 'dependsonvalue' {GetLanguageQueryPart("dependsonvalue", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS classes ON classes.item_id = filters.id AND classes.`key` = 'classes' {GetLanguageQueryPart("classes", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS entity ON entity.item_id = filters.id AND entity.`key` = 'entity' {GetLanguageQueryPart("entity", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS matchvalue ON matchvalue.item_id = filters.id AND matchvalue.`key` = 'matchvalue' {GetLanguageQueryPart("matchvalue", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS advancedfilter ON advancedfilter.item_id = filters.id AND advancedfilter.`key` = 'advancedfilter' {GetLanguageQueryPart("advancedfilter", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS customjoin ON customjoin.item_id = filters.id AND customjoin.`key` = 'customjoin' {GetLanguageQueryPart("customjoin", languageCode)}                                                            
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS customselect ON customselect.item_id = filters.id AND customselect.`key` = 'customselect' {GetLanguageQueryPart("customselect", languageCode)}                            
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS `group` ON `group`.item_id = filters.id AND `group`.`key` = 'group' {GetLanguageQueryPart("group", languageCode)}                            
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS connectedentity ON connectedentity.item_id = filters.id AND connectedentity.`key` = 'connectedentity' {GetLanguageQueryPart("connectedentity", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS connectedentityproperty ON connectedentityproperty.item_id = filters.id AND connectedentityproperty.`key` = 'connectedentityproperty' {GetLanguageQueryPart("connectedentityproperty", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS connectedentitylinktype ON connectedentitylinktype.item_id = filters.id AND connectedentitylinktype.`key` = 'connectedentitylinktype' {GetLanguageQueryPart("connectedentitylinktype", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS ismultilanguage ON ismultilanguage.item_id = filters.id AND ismultilanguage.`key` = 'ismultilanguage' {GetLanguageQueryPart("ismultilanguage", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS querystring ON querystring.item_id = filters.id AND querystring.`key` = 'querystring' {GetLanguageQueryPart("querystring", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS hideinsummary ON hideinsummary.item_id = filters.id AND hideinsummary.`key` = 'hideinsummary' {GetLanguageQueryPart("hideinsummary", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS filteronseovalue ON filteronseovalue.item_id = filters.id AND filteronseovalue.`key` = 'filteronseovalue' {GetLanguageQueryPart("filteronseovalue", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS singleconnecteditem ON singleconnecteditem.item_id = filters.id AND singleconnecteditem.`key` = 'singleconnecteditem' {GetLanguageQueryPart("singleconnecteditem", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS minimumitemsrequired ON minimumitemsrequired.item_id = filters.id AND minimumitemsrequired.`key` = 'minimumitemsrequired' {GetLanguageQueryPart("minimumitemsrequired", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS useaggregationtable ON useaggregationtable.item_id = filters.id AND useaggregationtable.`key` = 'useaggregationtable' {GetLanguageQueryPart("useaggregationtable", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS urlregex ON urlregex.item_id = filters.id AND urlregex.`key` = 'urlregex' {GetLanguageQueryPart("urlregex", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS grouptemplate ON grouptemplate.item_id = filters.id AND grouptemplate.`key` = 'grouptemplate' {GetLanguageQueryPart("grouptemplate", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS itemtemplate ON itemtemplate.item_id = filters.id AND itemtemplate.`key` = 'itemtemplate' {GetLanguageQueryPart("itemtemplate", languageCode)}
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS selecteditemtemplate ON selecteditemtemplate.item_id = filters.id AND selecteditemtemplate.`key` = 'selecteditemtemplate' {GetLanguageQueryPart("selecteditemtemplate", languageCode)}
                            WHERE filters.entity_type = 'filter'
                            ORDER BY filterstoitem.ordering ASC, filterstoparent.ordering ASC";

            // Add extra joins and select-parts if extra properties are necessary for use in template
            if (!String.IsNullOrEmpty(extraFilterProperties))
            {
                var selectPart = new StringBuilder(",");
                var joinPart = new StringBuilder();
                foreach (var extraFilterProperty in extraFilterProperties.Split(',', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries))
                {
                    selectPart.Append($"`{extraFilterProperty}`.`value` AS `{extraFilterProperty}`,");
                    joinPart.AppendLine($"LEFT JOIN {WiserTableNames.WiserItemDetail} AS `{extraFilterProperty}` ON `{extraFilterProperty}`.item_id = filters.id AND `{extraFilterProperty}`.`key` = '{extraFilterProperty}' {GetLanguageQueryPart(extraFilterProperty, languageCode)}");
                }
                w2FiltersQuery = w2FiltersQuery.Replace("{selectPart}", selectPart.ToString().TrimEnd(','));
                w2FiltersQuery = w2FiltersQuery.Replace("{joinPart}", joinPart.ToString());
            }
            else
            {
                w2FiltersQuery = w2FiltersQuery.Replace("{selectPart}", "");
                w2FiltersQuery = w2FiltersQuery.Replace("{joinPart}", "");
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("lang_id", languageCode);
            databaseConnection.AddParameter("category_id", categoryId > 0 ? categoryId : 0);
            databaseConnection.AddParameter("filtertoitemtype", filtersToItemType);

            var dataTable = await databaseConnection.GetAsync(w2FiltersQuery);
            var ignoreNotLinkedFilters = dataTable.Rows.Cast<DataRow>().Any(row => !row.IsNull("filterstoitemid"));

            // LEFT JOIN to filterstoitem because category id is always present for getting filter items from aggregation table
            // filterstoitem.id is checked in code below. If this column has a value for one or more filters, then the filters where filterstoitem.id IS NULL are ignored
            // The aggregation table ensures that only filters that apply to the relevant category are shown

            foreach (DataRow row in dataTable.Rows)
            {
                // Skip filter which is not connected to category (check in code above)
                if (ignoreNotLinkedFilters && row.IsNull("filterstoitemid"))
                {
                    continue;
                }

                // If URL not matches with regex, then skip this filter
                if (dataTable.Columns.Contains("urlregex") && !String.IsNullOrEmpty(row["urlregex"].ToString()) && !System.Text.RegularExpressions.Regex.IsMatch(HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext).ToString(), row["urlregex"].ToString()))
                {
                    continue;
                }

                FilterGroup filterGroup;
                var filterName = row.Field<string>("filtername");
                var filterGroupNameSeo = !dataTable.Columns.Contains("filtergroupnameseo") ? "" : row.Field<string>("filtergroupnameseo");
                if (!String.IsNullOrWhiteSpace(filterGroupNameSeo))
                {
                    filterGroup = new FilterGroup(filterName, filterGroupNameSeo);
                    filterGroup.IsGroupFilter = true;
                }
                else
                {
                    var filterSeoName = row.Field<string>("filternameseo");
                    filterGroup = !String.IsNullOrEmpty(filterSeoName) ? new FilterGroup(filterName, filterSeoName) : new FilterGroup(filterName);
                }

                var filterType = row.Field<string>("filtertype");
                if (Int32.TryParse(filterType, out var filterTypeAsNumber))
                {
                    filterGroup.FilterType = (FilterGroup.FilterGroupType)filterTypeAsNumber;
                }
                else
                {
                    filterGroup.FilterType = (FilterGroup.FilterGroupType)Enum.Parse(typeof(FilterGroup.FilterGroupType), filterType);
                }

                filterGroup.ShowCount = !row.IsNull("showcount") && Convert.ToBoolean(row["showcount"]);
                filterGroup.HideInSummary = row.Field<string>("hideinsummary") == "1";
                filterGroup.FilterOnSeoValue = row.Field<string>("filteronseovalue") == "1";
                filterGroup.SingleConnectedItem = row.Field<string>("singleconnecteditem") == "1";
                var filterGroupColumnName = row.Field<string>("columnname");
                filterGroup.ColumnName = String.IsNullOrWhiteSpace(filterGroupColumnName) ? filterGroup.Name : filterGroupColumnName;

                // The "classes" columns was added later, so check for its availability.
                filterGroup.Classes = row.GetValueIfColumnExists("classes", "");

                // The "entity" columns property added later, so check for its availability.
                filterGroup.EntityName = row.GetValueIfColumnExists("entity", "");

                // Match value can be used in combination with Wiser group filters to give the value on which the detail must match when the filter is selected.
                filterGroup.MatchValue = row.GetValueIfColumnExists("matchValue", filterGroup.MatchValue);

                // Advanced filter to give values and join statements.
                filterGroup.AdvancedFilter = row.GetValueIfColumnExists("advancedfilter", filterGroup.AdvancedFilter);

                filterGroup.CustomJoin = row.GetValueIfColumnExists("customjoin", filterGroup.CustomJoin);
                filterGroup.CustomSelect = row.GetValueIfColumnExists("customselect", filterGroup.CustomSelect);
                filterGroup.Group = row.GetValueIfColumnExists("group", filterGroup.Group);
                filterGroup.ConnectedEntity = row.GetValueIfColumnExists("connectedentity", filterGroup.ConnectedEntity);
                filterGroup.ConnectedEntityProperty = row.GetValueIfColumnExists("connectedentityproperty", filterGroup.ConnectedEntityProperty);
                filterGroup.ConnectedEntityLinkType = row.GetValueIfColumnExists("connectedentitylinktype", filterGroup.ConnectedEntityLinkType);
                filterGroup.IsMultiLanguage = row.GetValueIfColumnExists("ismultilanguage", filterGroup.IsMultiLanguage ? "1" : "0") == "1";
                filterGroup.QueryString = row.GetValueIfColumnExists("querystring", filterGroup.QueryString);
                filterGroup.MinimumItemsRequired = Int32.Parse(row.GetValueIfColumnExists("minimumitemsrequired", filterGroup.MinimumItemsRequired.ToString()));
                filterGroup.UseAggregationTable = row.GetValueIfColumnExists("useaggregationtable", filterGroup.UseAggregationTable ? "1" : "0") == "1";
                filterGroup.GroupTemplate = row.GetValueIfColumnExists("grouptemplate", filterGroup.GroupTemplate);
                filterGroup.ItemTemplate = row.GetValueIfColumnExists("itemtemplate", filterGroup.ItemTemplate);
                filterGroup.SelectedItemTemplate = row.GetValueIfColumnExists("selecteditemtemplate", filterGroup.SelectedItemTemplate);

                // Extra properties on filter level for use in templates
                if (!String.IsNullOrEmpty(extraFilterProperties))
                {
                    filterGroup.ExtraProperties = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var extraFilterProperty in extraFilterProperties.Split(',', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Where(p => !filterGroup.ExtraProperties.ContainsKey(p)))
                    {
                        filterGroup.ExtraProperties.Add(extraFilterProperty, row.IsNull(extraFilterProperty) ? "" : row[extraFilterProperty].ToString());
                    }
                }

                result.TryAdd(!String.IsNullOrEmpty(filterGroup.QueryString) ? filterGroup.QueryString : filterGroup.NameSeo, filterGroup);
            }

            return result;
        }

        private static string GetLanguageQueryPart(string columnName, string languageCode)
        {
            if (String.IsNullOrWhiteSpace(languageCode) || languageCode == "0")
            {
                return $"AND `{columnName}`.language_code = ''";
            }

            return $"AND (`{columnName}`.language_code = '' OR `{columnName}`.language_code = '{languageCode}')";
        }

        private static string AppendFilterJoinPart(int filterCounter, string filterName, string filterValue, FilterGroup filterGroup, bool forItemPart, bool forAggregationTable = false)
        {
            if (filterGroup is null)
            {
                return String.Empty;
            }

            var splitFilterValue = filterValue.Split('-');
            var filterValueIsNumber = Int32.TryParse(filterValue, out var filterValueAsNumber);
            var output = "";

            if (filterGroup.FilterType == FilterGroup.FilterGroupType.Slider)
            {
                if (forAggregationTable)
                {
                    if (filterValueIsNumber)
                    {
                        // One (minimum) value.
                        output = $"f{filterCounter}.filtergroup = {filterGroup.GetParamKey().ToMySqlSafeValue(true)} AND f{filterCounter}.filtervalue < {filterValueAsNumber}";
                    }
                    else if (splitFilterValue.Length == 2 && Int32.TryParse(splitFilterValue[0], out var minValue) && Int32.TryParse(splitFilterValue[1], out var maxValue))
                    {
                        // Two values (min and max).
                        output = $"f{filterCounter}.filtergroup = {filterGroup.GetParamKey().ToMySqlSafeValue(true)} AND f{filterCounter}.filtervalue >= {minValue} AND f{filterCounter}.filtervalue <= {maxValue}";
                    }
                }
                else
                {
                    if (filterValueIsNumber)
                    {
                        // One (minimum) value.
                        output = $"(fi{filterCounter}.`key` = {filterName.ToMySqlSafeValue(true)} AND REPLACE(fi{filterCounter}.`value`,',','.') < {filterValueAsNumber})";
                    }
                    else if (splitFilterValue.Length == 2 && Int32.TryParse(splitFilterValue[0], out var minValue) && Int32.TryParse(splitFilterValue[1], out var maxValue))
                    {
                        // Two values (min and max).
                        output = $"(fi{filterCounter}.`key` = {filterName.ToMySqlSafeValue(true)} AND fi{filterCounter}.`value` >= {minValue} AND fi{filterCounter}.`value` <= {maxValue})";
                    }
                }
            }
            else if (!String.IsNullOrWhiteSpace(filterValue))
            {
                // Get the value query part (in case of a Wiser 2 group filter)
                var valueQueryPart = $"AND fi{filterCounter}.`value` != '' AND fi{filterCounter}.`value` != '0'";
                if (filterGroup.IsGroupFilter)
                {
                    if (!String.IsNullOrEmpty(filterGroup.MatchValue))
                    {
                        if (filterGroup.MatchValue.Contains('~'))
                        {
                            valueQueryPart = $"AND fi{filterCounter}.`value` IN ({filterGroup.MatchValue.ToMySqlSafeValue(true).Replace("~", "','")})";
                        }
                        else
                        {
                            valueQueryPart = $"AND fi{filterCounter}.`value` = {filterGroup.MatchValue.ToMySqlSafeValue(true)}";
                        }
                    }
                }

                if (filterValue.Contains(Constants.ValueSplitter))
                {
                    // multiple values selected
                    if (forAggregationTable)
                    {
                        output = $"f{filterCounter}.filtergroup = {filterGroup.GetParamKey().ToMySqlSafeValue(true)} AND f{filterCounter}.filtervalue IN ({filterValue.ToMySqlSafeValue(true).Replace(Constants.ValueSplitter, "','")})";
                    }
                    else if (filterGroup.IsGroupFilter)
                    {
                        output = $"(fi{filterCounter}.groupname = {filterName.ToMySqlSafeValue(true)} AND  (fi{filterCounter}.`key` IN ({filterValue.ToMySqlSafeValue(true).Replace(Constants.ValueSplitter, "','")}) {valueQueryPart}))";
                    }
                    else if (filterName == "itemtitle")
                    {
                        output = $"(fi{filterCounter}.title IN ({filterValue.ToMySqlSafeValue(true).Replace(Constants.ValueSplitter, "','")}))";
                    }
                    else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity) && forItemPart)
                    {
                        if (String.IsNullOrEmpty(filterGroup.ConnectedEntityProperty))
                        {
                            output = $"(fi{filterCounter}i.title IN ({filterValue.ToMySqlSafeValue(true).Replace(Constants.ValueSplitter, "','")}))";
                        }
                        else
                        {
                            output = $"(fi{filterCounter}d.`value` IN ({filterValue.ToMySqlSafeValue(true).Replace(Constants.ValueSplitter, "','")}))";
                        }
                    }
                    else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity) && !forItemPart)
                    {
                        output = $"(fi{filterCounter}.`key` = CONCAT('{filterName.ToMySqlSafeValue(true)}, '{(filterGroup.FilterOnSeoValue ? "_SEO" : "")}'))";
                    }
                    else
                    {
                        output = $"(fi{filterCounter}.`key` = CONCAT('{filterName.ToMySqlSafeValue(true)}, '{(filterGroup.FilterOnSeoValue ? "_SEO" : "")}') AND (fi{filterCounter}.`value` IN ({filterValue.ToMySqlSafeValue(true).Replace(Constants.ValueSplitter, "','")})))";
                    }
                }
                else // single value selected
                {
                    if (forAggregationTable)
                    {
                        output = $"f{filterCounter}.filtergroup = {filterGroup.GetParamKey().ToMySqlSafeValue(true)} AND f{filterCounter}.filtervalue = {filterValue.ToMySqlSafeValue(true)}";
                    }
                    else if (filterGroup.IsGroupFilter)
                    {
                        output = $"(fi{filterCounter}.groupname = {filterName.ToMySqlSafeValue(true)} AND (fi{filterCounter}.`key` = {filterValue.ToMySqlSafeValue(true)} {valueQueryPart}))";
                    }
                    else if (filterName == "itemtitle")
                    {
                        output = $"(fi{filterCounter}.title = {filterValue.ToMySqlSafeValue(true)})";
                    }
                    else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity) && forItemPart)
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
                    else if (!String.IsNullOrEmpty(filterGroup.ConnectedEntity) && !forItemPart)
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