using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Models;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.DataSelector.Services
{
    public class DataSelectorsService : IDataSelectorsService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly ITemplatesService templatesService;
        private readonly IHtmlToPdfConverterService htmlToPdfConverterService;
        private readonly IExcelService excelService;
        private readonly ILogger<DataSelectorsService> logger;
        private readonly ILanguagesService languagesService;

        public DataSelectorsService(IOptions<GclSettings> gclSettings, IDatabaseConnection databaseConnection, IStringReplacementsService stringReplacementsService, ITemplatesService templatesService, IHtmlToPdfConverterService htmlToPdfConverterService, IExcelService excelService, ILogger<DataSelectorsService> logger, ILanguagesService languagesService)
        {
            this.gclSettings = gclSettings.Value;
            this.databaseConnection = databaseConnection;
            this.stringReplacementsService = stringReplacementsService;
            this.templatesService = templatesService;
            this.htmlToPdfConverterService = htmlToPdfConverterService;
            this.excelService = excelService;
            this.logger = logger;
            this.languagesService = languagesService;
        }

        /// <inheritdoc />
        public async Task<string> GetDataSelectorJsonAsync(int dataSelectorId)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("id", dataSelectorId);
            var getJsonResult = await databaseConnection.GetAsync($"SELECT request_json FROM `{WiserTableNames.WiserDataSelector}` WHERE id = ?id");

            return getJsonResult.Rows.Count > 0 ? getJsonResult.Rows[0].Field<string>("request_json") : String.Empty;
        }

        /// <inheritdoc />
        public async Task<string> GetWiserQueryAsync(int queryId)
        {
            databaseConnection.AddParameter("id", queryId);
            var getQueryResult = await databaseConnection.GetAsync($"SELECT query FROM `{WiserTableNames.WiserQuery}` WHERE id = ?id");

            return getQueryResult.Rows.Count > 0
                ? getQueryResult.Rows[0].Field<string>("query")
                : String.Empty;
        }

        /// <inheritdoc />
        public async Task<string> GetQueryAsync(ItemsRequest itemsRequest)
        {
            if (!String.IsNullOrWhiteSpace(itemsRequest.Query))
            {
                return itemsRequest.Query;
            }

            if (String.IsNullOrWhiteSpace(itemsRequest.Selector?.Main?.EntityName) && (itemsRequest.Selector?.Main?.Scopes == null || itemsRequest.Selector.Main.Scopes.Length == 0) && (itemsRequest.Selector?.Having == null || itemsRequest.Selector.Having.Length == 0) && String.IsNullOrWhiteSpace(itemsRequest.ContainsPath) && String.IsNullOrWhiteSpace(itemsRequest.EntityTypes))
            {
                return String.Empty;
            }

            // Use data selector.
            var queryBuilder = new StringBuilder();
            StringBuilder queryPart;
            var containsParentItemFields = false;
            int fileTypeCounter;
            var addFields = new List<Field>();
            var allEntityTypes = new List<string>();
            var mainEntityTablePrefix = "";

            if (!String.IsNullOrWhiteSpace(itemsRequest.GetFileTypes))
            {
                itemsRequest.FileTypes.AddRange(itemsRequest.GetFileTypes.Split(','));
            }

            // Main entities.
            if (!String.IsNullOrWhiteSpace(itemsRequest.Selector?.Main?.EntityName))
            {
                itemsRequest.EntityTypes = itemsRequest.Selector.Main.EntityName;
                allEntityTypes.Add(itemsRequest.Selector.Main.EntityName);
            }

            // Main scopes.
            if (itemsRequest.Selector?.Main?.Scopes != null)
            {
                await ProcessScopesAsync(itemsRequest, itemsRequest.Selector.Main.Scopes, "ilc1.id", "idv_");
            }

            // Main fields.
            if (itemsRequest.Selector?.Main?.Fields != null && itemsRequest.Selector.Main.Fields.Length > 0)
            {
                itemsRequest.FieldsInternal.AddRange(UpdateFieldsWithInternals("ilc1.id", "idv_", itemsRequest.Selector.Main.Fields));
            }

            // Process the query addition (custom where).
            if (!String.IsNullOrWhiteSpace(itemsRequest.Selector?.QueryAddition))
            {
                var queryAdditionBuilder = new StringBuilder(itemsRequest.QueryAddition);

                if (!itemsRequest.Selector.QueryAddition.Trim().StartsWith("AND", StringComparison.Ordinal))
                {
                    itemsRequest.Selector.QueryAddition = $"AND {itemsRequest.Selector.QueryAddition}";
                }

                queryAdditionBuilder.Append(itemsRequest.Selector.QueryAddition);
                itemsRequest.QueryAddition = queryAdditionBuilder.ToString();
            }

            // Get all used entity types, so we can get the settings from them.
            if (itemsRequest.Selector?.Connections != null && itemsRequest.Selector.Connections.Length > 0)
            {
                allEntityTypes.AddRange(itemsRequest.Selector.Connections.SelectMany(c => c.ConnectionRows.Select(r => r.EntityName)));

                // Get the link type settings.
                if (!String.IsNullOrWhiteSpace(itemsRequest.Selector?.Main?.EntityName))
                {
                    var getLinkTypesResult = await databaseConnection.GetAsync($"SELECT type, destination_entity_type, connected_entity_type, use_item_parent_id, use_dedicated_table FROM `{WiserTableNames.WiserLink}`");

                    foreach (var connection in itemsRequest.Selector.Connections)
                    {
                        foreach (var connectionRow in connection.ConnectionRows)
                        {
                            if (String.IsNullOrWhiteSpace(connectionRow.EntityName))
                            {
                                continue;
                            }

                            var goUp = connectionRow.Modes.Contains("up");
                            var settings = new LinkTypeSettings
                            {
                                Type = connectionRow.TypeNumber,
                                DestinationEntityType = goUp ? connectionRow.EntityName : itemsRequest.Selector.Main.EntityName,
                                SourceEntityType = goUp ? itemsRequest.Selector.Main.EntityName : connectionRow.EntityName
                            };
                            itemsRequest.LinkTypeSettings.Add(settings);

                            foreach (DataRow dataRow in getLinkTypesResult.Rows)
                            {
                                var typeNumber = dataRow.Field<int>("type");
                                var destinationEntityType = dataRow.Field<string>("destination_entity_type");
                                var sourceEntityType = dataRow.Field<string>("connected_entity_type");

                                if (!destinationEntityType.Equals(settings.DestinationEntityType, StringComparison.OrdinalIgnoreCase) || !sourceEntityType.Equals(settings.SourceEntityType, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                if (settings.Type > 0 && settings.Type != typeNumber)
                                {
                                    continue;
                                }

                                settings.UseParentItemId = Convert.ToBoolean(dataRow["use_item_parent_id"]);
                                settings.Type = typeNumber;
                                settings.DedicatedTablePrefix = Convert.ToBoolean(dataRow["use_dedicated_table"]) ? $"{typeNumber}_" : "";
                            }
                        }
                    }
                }
            }

            // Get all entity type settings.
            if (allEntityTypes.Count > 0)
            {
                var getEntitySettingsResult = await databaseConnection.GetAsync($"SELECT `name`, dedicated_table_prefix FROM `{WiserTableNames.WiserEntity}` WHERE `name` IN ({String.Join(", ", allEntityTypes.Select(e => e.ToMySqlSafeValue(true)))})");
                foreach (DataRow dataRow in getEntitySettingsResult.Rows)
                {
                    var entityType = dataRow.Field<string>("name");
                    var tablePrefix = dataRow.Field<string>("dedicated_table_prefix");
                    if (String.IsNullOrWhiteSpace(tablePrefix) || itemsRequest.DedicatedTables.ContainsKey(entityType))
                    {
                        continue;
                    }

                    itemsRequest.DedicatedTables.Add(entityType, tablePrefix);

                    if (!String.IsNullOrWhiteSpace(itemsRequest.Selector?.Main?.EntityName) && itemsRequest.Selector.Main.EntityName.Equals(entityType, StringComparison.OrdinalIgnoreCase))
                    {
                        mainEntityTablePrefix = tablePrefix;
                    }
                }
            }

            // Process connections recursive.
            if (itemsRequest.Selector?.Connections != null && itemsRequest.Selector.Connections.Length > 0)
            {
                itemsRequest.WhereLink.Add(" AND ");
                await ProcessConnectionsAsync(itemsRequest, itemsRequest.Selector.Connections, "ilc1.id");
            }

            // Process fields from fields (getting details if field is item id).
            foreach (var field in itemsRequest.FieldsInternal.Where(field => field.Fields is { Length: > 0 }))
            {
                addFields.AddRange(UpdateFieldsWithInternals(field.TableAlias + ".`value`", $"{field.TableAlias}_", field.Fields, fieldsFromField: true));
            }

            if (addFields.Count > 0)
            {
                itemsRequest.FieldsInternal.AddRange(addFields);
            }

            // Process the order by part of the data selector.
            if (itemsRequest.Selector?.OrderBy != null)
            {
                var orderByBuilder = new StringBuilder(itemsRequest.OrderPart);

                foreach (var orderByField in itemsRequest.Selector.OrderBy)
                {
                    if (orderByField.FieldName != "treeview")
                    {
                        if (orderByBuilder.Length > 0)
                        {
                            orderByBuilder.Append(',');
                        }

                        if (itemsRequest.FieldsInternal.Count > 0)
                        {
                            var done = false;
                            foreach (var item in itemsRequest.FieldsInternal.Where(item => item.FieldAlias == orderByField.FieldName))
                            {
                                orderByBuilder.Append($"`{item.SelectAlias.ToMySqlSafeValue(false)}` {orderByField.Direction}");
                                done = true;
                                break;
                            }

                            if (!done)
                            {
                                orderByBuilder.Append($"`{orderByField.FieldName.ToMySqlSafeValue(false)}` {orderByField.Direction}");
                            }
                        }
                        else
                        {
                            orderByBuilder.Append($"`{orderByField.FieldName.ToMySqlSafeValue(false)}` {orderByField.Direction}");
                        }
                    }
                    else
                    {
                        itemsRequest.AutoSortOrder = orderByField.Direction;
                    }
                }

                itemsRequest.OrderPart = orderByBuilder.ToString();
            }

            // Change inputs if necessary.
            if (!itemsRequest.LinkType.HasValue)
            {
                itemsRequest.LinkType = 1;
            }

            if (itemsRequest.NumberOfLevels == 0)
            {
                itemsRequest.NumberOfLevels = 2;
            }

            if (!String.IsNullOrWhiteSpace(itemsRequest.ContainsPath))
            {
                if (!itemsRequest.ContainsPath.StartsWith("/"))
                {
                    itemsRequest.ContainsPath = $"/{itemsRequest.ContainsPath}";
                }
                if (!itemsRequest.ContainsPath.EndsWith("/"))
                {
                    itemsRequest.ContainsPath = $"{itemsRequest.ContainsPath}/";
                }
            }

            if (!String.IsNullOrWhiteSpace(itemsRequest.ContainsUrl))
            {
                if (!itemsRequest.ContainsUrl.StartsWith("/"))
                {
                    itemsRequest.ContainsUrl = $"/{itemsRequest.ContainsUrl}";
                }
                if (!itemsRequest.ContainsUrl.EndsWith("/"))
                {
                    itemsRequest.ContainsUrl = $"{itemsRequest.ContainsUrl}/";
                }
            }

            if (itemsRequest.PageNumber == 0)
            {
                itemsRequest.PageNumber = 1;
            }

            // Build query.
            queryBuilder.AppendLine("SELECT ilc1.id");

            // Select necessary fields.
            if (itemsRequest.FieldsInternal.Count > 0)
            {
                foreach (var field in itemsRequest.FieldsInternal)
                {
                    if (field.FieldName == "idencrypted")
                    {
                        if (field.TableAliasPrefix == "idv_")
                        {
                            if (String.IsNullOrWhiteSpace(field.FieldAlias))
                            {
                                field.FieldAlias = "idencrypted_encrypt_withdate";
                            }
                            else if (!field.FieldAlias.EndsWith("_encrypt_withdate"))
                            {
                                field.FieldAlias = $"{field.FieldAlias}_encrypt_withdate";
                            }
                            queryBuilder.AppendLine($", {GetFormattedField(field, "ilc1.id")} AS `{field.FieldAlias}`");
                        }
                        else if (field.FieldFromField)
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAlias.ToMySqlSafeValue(false)}`.id")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}_encrypt_withdate`");
                        }
                        else
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAliasPrefix.Replace("idv_", "").ToMySqlSafeValue(false)}item`.id")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}_encrypt_withdate`");
                        }
                    }
                    else if (field.FieldName == "itemtitle")
                    {
                        if (field.TableAliasPrefix == "idv_")
                        {
                            if (String.IsNullOrWhiteSpace(field.FieldAlias))
                            {
                                field.FieldAlias = "itemtitle";
                            }

                            queryBuilder.AppendLine($", {GetFormattedField(field, "ilc1.title")} AS `{field.FieldAlias}`");
                        }
                        else if (field.FieldFromField)
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAlias.ToMySqlSafeValue(false)}`.title")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                        else
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAliasPrefix.Replace("idv_", "").ToMySqlSafeValue(false)}item`.title")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                    }
                    else if (field.FieldName == "unique_uuid")
                    {
                        if (field.TableAliasPrefix == "idv_")
                        {
                            if (String.IsNullOrWhiteSpace(field.FieldAlias))
                            {
                                field.FieldAlias = "unique_uuid";
                            }

                            queryBuilder.AppendLine($", {GetFormattedField(field, "ilc1.unique_uuid")} AS `{field.FieldAlias.ToMySqlSafeValue(false)}`");
                        }
                        else if (field.FieldFromField)
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAlias.ToMySqlSafeValue(false)}`.unique_uuid")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                        else
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAliasPrefix.Replace("idv_", "").ToMySqlSafeValue(false)}item`.unique_uuid")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                    }
                    else if (field.FieldName == "parentitemtitle")
                    {
                        // TODO: Add possibility to retrieve ParentItemTitle from connected rows, but we'll need to join up a couple of times, or not if we're joining down.
                        if (String.IsNullOrWhiteSpace(field.FieldAlias))
                        {
                            field.FieldAlias = "parentitemtitle";
                        }

                        queryBuilder.AppendLine($", {GetFormattedField(field, "ilc2.title")} AS `{field.FieldAlias}`");
                        containsParentItemFields = true;
                    }
                    else if (field.FieldName == "id")
                    {
                        if (field.TableAliasPrefix == "idv_")
                        {
                            if (String.IsNullOrWhiteSpace(field.FieldAlias))
                            {
                                field.FieldAlias = "id";
                            }

                            queryBuilder.AppendLine($", {GetFormattedField(field, "ilc1.id")} AS `{field.FieldAlias.ToMySqlSafeValue(false)}`");
                        }
                        else if (field.FieldFromField)
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAlias.ToMySqlSafeValue(false)}`.id")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                        else if (field.IsLinkField)
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAliasPrefix.ToMySqlSafeValue(false)}`.id")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                        else
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAliasPrefix.Replace("idv_", "").ToMySqlSafeValue(false)}item`.id")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                    }
                    else if (field.FieldName == "moduleid")
                    {
                        if (field.TableAliasPrefix == "idv_")
                        {
                            if (String.IsNullOrWhiteSpace(field.FieldAlias))
                            {
                                field.FieldAlias = "moduleid";
                            }

                            queryBuilder.AppendLine($", {GetFormattedField(field, "ilc1.moduleid")} AS `{field.FieldAlias.ToMySqlSafeValue(false)}`");
                        }
                        else if (field.FieldFromField)
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAlias.ToMySqlSafeValue(false)}`.moduleid")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                        else
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAliasPrefix.Replace("idv_", "")}item`.moduleid")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                    }
                    else if (field.FieldName.InList("changed_on", "changed_by"))
                    {
                        if (field.TableAliasPrefix == "idv_")
                        {
                            if (String.IsNullOrWhiteSpace(field.FieldAlias))
                            {
                                field.FieldAlias = field.FieldName;
                            }

                            queryBuilder.AppendLine($", {GetFormattedField(field, $"ilc1.`{field.FieldName.ToMySqlSafeValue(false)}`")} AS `{field.FieldAlias.ToMySqlSafeValue(false)}`");
                        }
                        else if (field.FieldFromField)
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAlias.ToMySqlSafeValue(false)}`.`{field.FieldName.ToMySqlSafeValue(false)}`")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                        else
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAliasPrefix.Replace("idv_", "").ToMySqlSafeValue(false)}item`.`{field.FieldName.ToMySqlSafeValue(false)}`")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                    }
                    else if (field.FieldName.Equals("item_ordering"))
                    {
                        if (field.TableAliasPrefix == "idv_")
                        {
                            if (String.IsNullOrWhiteSpace(field.FieldAlias))
                            {
                                field.FieldAlias = "item_ordering";
                            }

                            queryBuilder.AppendLine($", {GetFormattedField(field, "ilc1.ordering")} AS `{field.FieldAlias}`");
                        }
                        else if (field.FieldFromField)
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAlias.ToMySqlSafeValue(false)}`.ordering")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                        else
                        {
                            queryBuilder.AppendLine($", {GetFormattedField(field, $"`{field.TableAliasPrefix.Replace("idv_", "").ToMySqlSafeValue(false)}item`.ordering")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                        }
                    }
                    else if (field.FieldName.Equals("link_ordering"))
                    {
                        var tableAliasPrefix = field.TableAliasPrefix.Replace("idv_", "").TrimEnd('_');

                        queryBuilder.AppendLine($", {GetFormattedField(field, $"`{tableAliasPrefix.ToMySqlSafeValue(false)}`.ordering")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                    }
                    else
                    {
                        queryBuilder.AppendLine($", {GetFormattedField(field, $"CONCAT_WS('', `{field.TableAlias.ToMySqlSafeValue(false)}`.`value`, `{field.TableAlias.ToMySqlSafeValue(false)}`.long_value)")} AS `{field.SelectAlias.ToMySqlSafeValue(false)}`");
                    }

                    if (itemsRequest.JoinDetail.All(f => f.TableAlias != field.TableAlias))
                    {
                        if (!String.IsNullOrWhiteSpace(field.JoinOn))
                        {
                            itemsRequest.JoinDetail.Add(field);
                        }
                    }
                }

                fileTypeCounter = 1;
                foreach (var fileType in itemsRequest.FileTypes)
                {
                    queryBuilder.AppendLine($", file{fileTypeCounter}.content_url AS `{fileType}_url`, file{fileTypeCounter}.content AS `{fileType}_content`, file{fileTypeCounter}.content_type AS `{fileType}_mimetype`, file{fileTypeCounter}.title AS `{fileType}_title`");
                    fileTypeCounter += 1;
                }
            }

            queryBuilder.AppendLine($"FROM `{mainEntityTablePrefix}{WiserTableNames.WiserItem}` AS ilc1");

            // File types.
            fileTypeCounter = 1;
            foreach (var fileType in itemsRequest.FileTypes)
            {
                queryBuilder.AppendLine($"LEFT JOIN `{mainEntityTablePrefix}{WiserTableNames.WiserItemFile}` AS file{fileTypeCounter} ON file{fileTypeCounter}.property_name = '{fileType}' AND file{fileTypeCounter}.item_id = ilc1.id");
                fileTypeCounter += 1;
            }

            // Other JOINs.
            foreach (var item in itemsRequest.JoinLink)
            { 
                queryBuilder.AppendLine(item);
            }

            if (!String.IsNullOrWhiteSpace(itemsRequest.ContainsUrl))
            {
                queryBuilder.AppendLine($"LEFT JOIN `{mainEntityTablePrefix}{WiserTableNames.WiserItemDetail}` AS id1 ON id1.item_id = ilc1.id AND id1.`key` = 'url'{(String.IsNullOrWhiteSpace(itemsRequest.LanguageCode) ? "" : $" AND id1.language_code = {itemsRequest.LanguageCode.ToMySqlSafeValue(true)}")}");
            }

            if (!String.IsNullOrWhiteSpace(itemsRequest.ParentId))
            {
                var linkTypeSettings = itemsRequest.LinkTypeSettings.FirstOrDefault(x => x.Type == itemsRequest.LinkType);
                queryBuilder.AppendLine($"LEFT JOIN `{linkTypeSettings?.DedicatedTablePrefix ?? ""}{WiserTableNames.WiserItemLink}` AS ilp1 ON ilp1.item_id = ilc1.id AND ilp1.type = {itemsRequest.LinkType}");
            }
            else if (itemsRequest.LinkTables.Count == 0 && !String.IsNullOrWhiteSpace(itemsRequest.AutoSortOrder))
            {
                var linkTypeSettings = itemsRequest.LinkTypeSettings.FirstOrDefault(x => x.Type == 1);
                queryBuilder.AppendLine($"LEFT JOIN `{linkTypeSettings?.DedicatedTablePrefix ?? ""}{WiserTableNames.WiserItemLink}` AS ilp1 ON lip1.item_id = ilc1.id AND ilp1.type = 1");
            }

            if (!String.IsNullOrWhiteSpace(itemsRequest.ContainsPath) || !String.IsNullOrWhiteSpace(itemsRequest.ContainsUrl) || (!String.IsNullOrWhiteSpace(itemsRequest.ParentId) && itemsRequest.Descendants) || containsParentItemFields)
            {
                var linkTypeSettings = itemsRequest.LinkTypeSettings.FirstOrDefault(x => x.Type == itemsRequest.LinkType);
                for (var i = 2; i <= itemsRequest.NumberOfLevels; i++)
                {
                    queryBuilder.AppendLine($"LEFT JOIN `{linkTypeSettings?.DedicatedTablePrefix ?? ""}{WiserTableNames.WiserItemLink}` AS ilp{i} ON ilp{i}.item_id = ilp{i - 1}.destination_item_id AND ilp{i}.type = {itemsRequest.LinkType}");
                    queryBuilder.AppendLine($"LEFT JOIN `{mainEntityTablePrefix}{WiserTableNames.WiserItem}` AS ilc{i} ON ilc{i}.id = ilp{i - 1}.destination_item_id AND ilc{i}.published_environment & {itemsRequest.Environment} = {itemsRequest.Environment}");
                    queryBuilder.AppendLine($"LEFT JOIN `{mainEntityTablePrefix}{WiserTableNames.WiserItemDetail}` AS id{i}.item_id = ilp{i - 1}.destination_item_id AND id{i}.`key` = 'url' {(String.IsNullOrWhiteSpace(itemsRequest.LanguageCode) ? "" : $" AND id{i}.language_code = {itemsRequest.LanguageCode.ToMySqlSafeValue(true)}")}");
                }
            }

            // Join necessary fields.
            if (itemsRequest.FieldsInternal.Count > 0)
            {
                foreach (var item in itemsRequest.JoinDetail)
                {
                    var languageCodePart = "";
                    if (!String.IsNullOrWhiteSpace(item.LanguageCode))
                    {
                        languageCodePart = $" AND `{item.TableAlias.ToMySqlSafeValue(false)}`.language_code = {item.LanguageCode.ToMySqlSafeValue(true)}";
                    }
                    else if (!String.IsNullOrWhiteSpace(itemsRequest.LanguageCode))
                    {
                        languageCodePart = $" AND `{item.TableAlias.ToMySqlSafeValue(false)}`.language_code = {itemsRequest.LanguageCode.ToMySqlSafeValue(true)}";
                    }

                    if (item.IsLinkField)
                    {
                        queryBuilder.AppendLine($"LEFT JOIN `{WiserTableNames.WiserItemLinkDetail}` AS `{item.TableAlias.ToMySqlSafeValue(false)}` ON `{item.TableAlias.ToMySqlSafeValue(false)}`.itemlink_id = {item.JoinOn} AND `{item.TableAlias.ToMySqlSafeValue(false)}`.`key` = '{item.FieldName.ToMySqlSafeValue(false)}' {languageCodePart}");
                    }
                    else if (item.FieldFromField && item.IsReservedFieldName)
                    {
                        queryBuilder.AppendLine($"LEFT JOIN `{mainEntityTablePrefix}{WiserTableNames.WiserItem}` AS `{item.TableAlias}` ON `{item.TableAlias.ToMySqlSafeValue(false)}`.id = {item.JoinOn}");
                    }
                    else
                    {
                        queryBuilder.AppendLine($"LEFT JOIN `{mainEntityTablePrefix}{WiserTableNames.WiserItemDetail}` AS `{item.TableAlias}` ON `{item.TableAlias.ToMySqlSafeValue(false)}`.item_id = {item.JoinOn} AND `{item.TableAlias.ToMySqlSafeValue(false)}`.`key` = '{item.FieldName.ToMySqlSafeValue(false)}' {languageCodePart}");
                    }
                }
            }

            queryBuilder.AppendLine("WHERE TRUE");
            queryBuilder.AppendLine(String.IsNullOrWhiteSpace(itemsRequest.ModuleId) ? "" : $" AND ilc1.moduleid = {itemsRequest.ModuleId.ToMySqlSafeValue(true)}");
            if (!String.IsNullOrWhiteSpace(itemsRequest.EntityTypes))
            {
                if (itemsRequest.EntityTypes.Contains(","))
                {
                    var inValue = String.Join(", ", itemsRequest.EntityTypes.Split(',').Select(v => v.ToMySqlSafeValue(true)));
                    queryBuilder.AppendLine($" AND ilc1.entity_type IN ({inValue})");
                }
                else
                {
                    queryBuilder.AppendLine($" AND ilc1.entity_type = {itemsRequest.EntityTypes.ToMySqlSafeValue(true)}");
                }
            }

            queryBuilder.AppendLine($" AND ilc1.published_environment & {itemsRequest.Environment} = {itemsRequest.Environment}");
            queryBuilder.AppendLine(itemsRequest.QueryAddition);

            // Check if given path matches the path in the database.
            if (!String.IsNullOrWhiteSpace(itemsRequest.ContainsPath))
            {
                queryPart = new StringBuilder();
                for (var i = itemsRequest.NumberOfLevels; i >= 2; i--)
                {
                    queryPart.Append($"ilc{i}.title, ");
                }

                queryBuilder.AppendLine(
                    itemsRequest.Descendants
                        ? $"AND CONCAT_WS('/', '', {queryPart}'') LIKE '%{itemsRequest.ContainsPath.ToMySqlSafeValue(false)}%'"
                        : $"AND CONCAT_WS('/', '', {queryPart}'') = {itemsRequest.ContainsPath.ToMySqlSafeValue(true)}"
                );
            }

            // Check if given url matches url in the database.
            if (!String.IsNullOrWhiteSpace(itemsRequest.ContainsUrl))
            {
                queryPart = new StringBuilder();
                for (var i = itemsRequest.NumberOfLevels; i >= 2; i--)
                {
                    queryPart.Append($"id{i}.`value`, ");
                }

                queryBuilder.AppendLine(
                    itemsRequest.Descendants
                        ? $"AND CONCAT_WS('/', '', {queryPart}'') LIKE '%{itemsRequest.ContainsUrl.ToMySqlSafeValue(false)}%'"
                        : $"AND CONCAT_WS('/', '', {queryPart}'') = {itemsRequest.ContainsUrl.ToMySqlSafeValue(true)}"
                );
            }

            // Match if given parent ID matches the parent IDs in the database.
            if (!String.IsNullOrWhiteSpace(itemsRequest.ParentId))
            {
                if (itemsRequest.Descendants)
                {
                    queryPart = new StringBuilder();
                    for (var i = 1; i <= itemsRequest.NumberOfLevels; i++)
                    {
                        queryPart.Append($"ilp{i}.destination_item_id,");
                    }

                    queryBuilder.AppendLine($"AND {itemsRequest.ParentId.ToMySqlSafeValue(true)} IN ({queryPart.ToString().TrimEnd(',')})");
                }
            }

            // The where parts for the left joins of the link tables.
            foreach (var item in itemsRequest.WhereLink)
            {
                queryBuilder.AppendLine(item);
            }

            // Handle grouping.
            if (itemsRequest.Selector?.GroupBy != null && itemsRequest.Selector.GroupBy.Length > 0)
            {
                queryBuilder.AppendLine("GROUP BY");
                for (var i = 0; i < itemsRequest.Selector.GroupBy.Length; i++)
                {
                    if (i != 0)
                    {
                        queryBuilder.Append(", ");
                    }

                    queryBuilder.AppendLine(GetFullSelectAlias(itemsRequest, itemsRequest.Selector.GroupBy[i]));
                }
            }

            // Handle having.
            if (itemsRequest.Selector?.Having != null && itemsRequest.Selector.Having.Length > 0)
            {
                var havingPart = new StringBuilder();

                foreach (var havingGroup in itemsRequest.Selector.Having)
                {
                    if (havingGroup.HavingRows.Length <= 0)
                    {
                        continue;
                    }

                    queryPart = new StringBuilder();
                    foreach (var row in havingGroup.HavingRows)
                    {
                        if (queryPart.Length > 0)
                        {
                            queryPart.Append(" OR ");
                        }

                        queryPart.Append(await CreateHavingRowQueryPart(row, row.Key.FieldName));
                    }

                    // Add group to having part.
                    if (havingPart.Length > 0)
                    {
                        havingPart.Append(" AND ");
                    }

                    havingPart.Append($"({queryPart})");
                }

                if (havingPart.Length > 0)
                {
                    queryBuilder.AppendLine($"HAVING {havingPart}");
                }
            }

            var queryAsString = queryBuilder.ToString();
            if (!String.IsNullOrWhiteSpace(itemsRequest.OrderPart))
            {
                queryBuilder.AppendLine($"ORDER BY {itemsRequest.OrderPart}");
            }
            else if (!String.IsNullOrWhiteSpace(itemsRequest.ContainsPath) || !String.IsNullOrWhiteSpace(itemsRequest.ContainsUrl) || (!String.IsNullOrWhiteSpace(itemsRequest.ParentId) && itemsRequest.Descendants))
            {
                queryPart = new StringBuilder();
                for (var i = itemsRequest.NumberOfLevels; i >= 1; i--)
                {
                    queryPart.Append($"ilp{i}.ordering,");
                }

                queryBuilder.AppendLine($"ORDER BY {queryPart.ToString().TrimEnd(',')}");
            }
            else if (queryAsString.Contains(" ilp1 "))
            {
                queryBuilder.AppendLine("ORDER BY ilp1.ordering");
            }
            else if (!String.IsNullOrWhiteSpace(itemsRequest.AutoSortOrder))
            {
                var autoOrder = new StringBuilder();
                itemsRequest.LinkTables.Sort((x, y) => x.Length.CompareTo(y.Length));

                foreach (var table in itemsRequest.LinkTables)
                {
                    autoOrder.Append($"{table}.ordering {itemsRequest.AutoSortOrder},");
                }

                if (autoOrder.Length > 0)
                {
                    queryBuilder.AppendLine($"ORDER BY {autoOrder.ToString().TrimEnd(',')}");
                }
            }

            if (!String.IsNullOrWhiteSpace(itemsRequest.Selector?.Limit) && itemsRequest.Selector.Limit != "0")
            {
                // Split limit, convert values to int and then join again, to make it safe against SQL injection.
                queryBuilder.AppendLine($"LIMIT {String.Join(",", itemsRequest.Selector.Limit.Split(",", StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries).Select(Int32.Parse))}");
            }
            else if (Int32.TryParse(itemsRequest.NumberOfItems, out var numberOfItems))
            {
                queryBuilder.AppendLine($"LIMIT {(itemsRequest.PageNumber - 1) * numberOfItems},{numberOfItems}");
            }

            itemsRequest.Query = queryBuilder.ToString();

            return itemsRequest.Query;
        }

        /// <inheritdoc />
        public void ReplaceVariableValuesInDataSelector(Models.DataSelector selector)
        {
            if (selector?.Main?.Scopes != null)
            {
                ReplaceVariableValuesInScopes(selector.Main.Scopes);
            }

            if (selector?.Connections != null)
            {
                ReplaceVariableValuesInConnections(selector.Connections);
            }
        }

        /// <inheritdoc />
        public void ReplaceVariableValuesInConnections(IEnumerable<Connection> connections)
        {
            foreach (var connection in connections)
            {
                if (connection.ConnectionRows == null)
                {
                    continue;
                }

                foreach (var row in connection.ConnectionRows)
                {
                    if (row.Scopes != null)
                    {
                        ReplaceVariableValuesInScopes(row.Scopes);
                    }

                    if (row.Connections != null)
                    {
                        // Recursively update connections.
                        ReplaceVariableValuesInConnections(row.Connections);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void ReplaceVariableValuesInScopes(IEnumerable<Scope> scopes)
        {
            if (scopes == null)
            {
                return;
            }

            foreach (var scope in scopes)
            {
                if (scope.ScopeRows == null)
                {
                    continue;
                }

                foreach (var row in scope.ScopeRows)
                {
                    switch (row.Value)
                    {
                        case null:
                            continue;
                        case JArray valueArray:
                            {
                                for (var i = 0; i < valueArray.Count; i++)
                                {
                                    valueArray[i] = stringReplacementsService.DoHttpRequestReplacements(valueArray[i].ToString(), true);
                                }

                                row.Value = JArray.FromObject(valueArray);
                                break;
                            }
                        default:
                            row.Value = stringReplacementsService.DoHttpRequestReplacements(row.Value.ToString(), true);
                            break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<(JArray Result, HttpStatusCode StatusCode, string Error)> GetJsonResponseAsync(DataSelectorRequestModel data, bool skipSecurity = false)
        {
            var (itemsRequest, statusCode, error) = await InitializeItemsRequestAsync(data, skipSecurity);
            if (statusCode != HttpStatusCode.OK)
            {
                return (null, statusCode, error);
            }

            var dataSelectorQuery = await GetQueryAsync(itemsRequest);
            var queryTemplate = new QueryTemplate
            {
                Content = dataSelectorQuery
            };

            return (await templatesService.GetJsonResponseFromQueryAsync(queryTemplate, recursive: true), HttpStatusCode.OK, String.Empty);
        }
        
        /// <inheritdoc />
        public async Task<(ItemsRequest Result, HttpStatusCode StatusCode, string Error)> InitializeItemsRequestAsync(DataSelectorRequestModel data, bool skipSecurity = false)
        {
            var itemsRequest = new ItemsRequest();

            if (data.Settings != null)
            {
                itemsRequest.Selector = data.Settings;

                if (!itemsRequest.Selector.Insecure && !skipSecurity)
                {
                    return (null, HttpStatusCode.BadRequest, "This data selector may not be invoked unsecured.");
                }
            }
            else if (data.DataSelectorId is > 0)
            {
                var json = await GetDataSelectorJsonAsync(data.DataSelectorId.Value);
                var dataSelector = JsonConvert.DeserializeObject<Models.DataSelector>(json);

                if (!skipSecurity && dataSelector is { Insecure: false })
                {
                    // When trying to load the data selector without security.
                    if (String.IsNullOrWhiteSpace(data.Hash))
                    {
                        return (null, HttpStatusCode.BadRequest, "This data selector may not be invoked unsecured.");
                    }

                    var validateHashResult = ValidateHash(data);
                    if (!String.IsNullOrWhiteSpace(validateHashResult))
                    {
                        return (null, HttpStatusCode.BadRequest, $"Hash check failed, reason: {validateHashResult}");
                    }
                }

                // Proceed.
                itemsRequest.Selector = dataSelector;
                itemsRequest.Environment = data.Environment ?? 0;
            }
            else if (!String.IsNullOrWhiteSpace(data.QueryId))
            {
                int queryId;
                if (!String.IsNullOrWhiteSpace(data.Hash))
                {
                    if (!Int32.TryParse(data.QueryId, out queryId))
                    {
                        queryId = Int32.Parse(data.QueryId.DecryptWithAesWithSalt(withDateTime: true));
                    }
                }
                else
                {
                    queryId = Int32.Parse(data.QueryId.DecryptWithAesWithSalt(withDateTime: true));
                }

                var query = await GetWiserQueryAsync(queryId);
                itemsRequest.Query = query;
            }

            itemsRequest.Query = stringReplacementsService.DoHttpRequestReplacements(itemsRequest.Query, true);

            itemsRequest.ModuleId = data.ModuleId;
            itemsRequest.NumberOfLevels = data.NumberOfLevels ?? 0;
            itemsRequest.Descendants = data.Descendants ?? false;
            itemsRequest.LanguageCode = data.LanguageCode;
            itemsRequest.NumberOfItems = data.NumberOfItems;
            itemsRequest.PageNumber = data.PageNumber ?? 0;
            itemsRequest.ContainsPath = data.ContainsPath;
            itemsRequest.ContainsUrl = data.ContainsUrl;
            itemsRequest.ParentId = data.ParentId;
            itemsRequest.EntityTypes = data.EntityTypes;
            itemsRequest.LinkType = data.LinkType;
            itemsRequest.QueryAddition = data.QueryAddition;
            itemsRequest.OrderPart = data.OrderPart;
            itemsRequest.GetFileTypes = data.FileTypes;

            if (data.Fields != null)
            {
                foreach (var item in data.Fields.Split(','))
                {
                    itemsRequest.FieldsInternal.Add(new Field { FieldName = item, JoinOn = "ilc1.id", TableAliasPrefix = "idv_" });
                }
            }

            if (itemsRequest.Selector != null)
            {
                ReplaceVariableValuesInDataSelector(itemsRequest.Selector);
            }

            return (itemsRequest, HttpStatusCode.OK, String.Empty);
        }
        
        /// <inheritdoc />
        public async Task<(FileContentResult Result, HttpStatusCode StatusCode, string Error)> ToExcelAsync(DataSelectorRequestModel data)
        {
            if (data == null)
            {
                return (null, HttpStatusCode.BadRequest, String.Empty);
            }

            var (jsonResult, statusCode, error) = await GetJsonResponseAsync(data);
            if (statusCode != HttpStatusCode.OK)
            {
                return (null, statusCode, error);
            }

            var excelFile = excelService.JsonArrayToExcel(jsonResult);
            return (new FileContentResult(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"), HttpStatusCode.OK, String.Empty);
        }

        /// <inheritdoc />
        public async Task<(string Result, HttpStatusCode StatusCode, string Error)> ToHtmlAsync(DataSelectorRequestModel data)
        {
            if (data == null)
            {
                return (null, HttpStatusCode.BadRequest, String.Empty);
            }

            databaseConnection.ClearParameters();
            var (jsonResult, statusCode, error) = await GetJsonResponseAsync(data);

            if (statusCode != HttpStatusCode.OK)
            {
                return (null, statusCode, error);
            }

            ulong contentItemId = 0;
            if (!String.IsNullOrWhiteSpace(data.ContentItemId) && !UInt64.TryParse(data.ContentItemId, out contentItemId))
            {
                contentItemId = Convert.ToUInt64(data.ContentItemId.DecryptWithAesWithSalt(withDateTime: true));
            }

            if (contentItemId > 0)
            {
                if (String.IsNullOrWhiteSpace(data.ContentPropertyName))
                {
                    data.ContentPropertyName = "html_template";
                }

                if (String.IsNullOrWhiteSpace(data.LanguageCode))
                {
                    data.LanguageCode = languagesService.CurrentLanguageCode;
                }

                var query = $@"
                    SELECT `key`, CONCAT_WS('', `value`, `long_value`) AS value, language_code
                    FROM `{WiserTableNames.WiserItemDetail}`
                    WHERE item_id = ?contentItemId
                    AND `key` = ?contentPropertyName
                    ORDER BY language_code ASC";

                databaseConnection.AddParameter("contentItemId", contentItemId);
                databaseConnection.AddParameter("contentPropertyName", data.ContentPropertyName);

                var getContentSettingsResult = await databaseConnection.GetAsync(query);

                if (getContentSettingsResult.Rows.Count > 0)
                {
                    foreach (DataRow dataRow in getContentSettingsResult.Rows)
                    {
                        var languageCode = dataRow.Field<string>("language_code");
                        if (String.Equals(languageCode, data.LanguageCode) || (String.IsNullOrWhiteSpace(languageCode) && String.IsNullOrWhiteSpace(data.LanguageCode)))
                        {
                            data.OutputTemplate = dataRow.Field<string>("value");
                        }
                    }

                    // If it's still empty here, then we haven't found a value for the correct language code, then just try the first row. This point should not be reached in most cases.
                    if (String.IsNullOrWhiteSpace(data.OutputTemplate))
                    {
                        data.OutputTemplate = getContentSettingsResult.Rows[0].Field<string>("value");
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(data.OutputTemplate))
            {
                return (data.OutputTemplate, HttpStatusCode.OK, String.Empty);
            }

            var outputTemplate = data.OutputTemplate.Replace("{html}", "{html:Raw}");
            var output = stringReplacementsService.FillStringByClassList(jsonResult, outputTemplate);

            output = stringReplacementsService.EvaluateTemplate(output);

            return (output, HttpStatusCode.OK, String.Empty);
        }

        /// <inheritdoc />
        public async Task<(FileContentResult Result, HttpStatusCode StatusCode, string Error)> ToPdfAsync(DataSelectorRequestModel data)
        {
            var (htmlResult, statusCode, error) = await ToHtmlAsync(data);
            if (statusCode != HttpStatusCode.OK)
            {
                return (null, StatusCode: statusCode, Error: error);
            }
            
            ulong contentItemId = 0;
            if (!String.IsNullOrWhiteSpace(data.ContentItemId) && !UInt64.TryParse(data.ContentItemId, out contentItemId))
            {
                contentItemId = Convert.ToUInt64(data.ContentItemId.DecryptWithAesWithSalt(withDateTime: true));
            }

            var pdfSettings = await htmlToPdfConverterService.GetHtmlToPdfSettingsAsync(contentItemId, data.LanguageCode, data.ContentPropertyName);

            pdfSettings.FileName = data.FileName;
            pdfSettings.Html = htmlResult;
            
            var pdfFile = await htmlToPdfConverterService.ConvertHtmlStringToPdfAsync(pdfSettings);
            return (pdfFile, HttpStatusCode.OK, String.Empty);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllDataSelectorsAsync(string template)
        {
            if (String.IsNullOrWhiteSpace(template))
            {
                return template;
            }

            // Data selectors with templates.
            var regEx = new Regex(@"<div[^<>]*?(?:class=['""]dynamic-content['""][^<>]*?)?(data-selector-id)=['""](?<dataSelectorId>\d+)['""]([^<>]*?)?(template-id)=['""](?<templateId>\d+)['""][^>]*?>[^<>]*?<h2>[^<>]*?(?<title>[^<>]*?)<\/h2>[^<>]*?<\/div>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMinutes(3));

            var matches = regEx.Matches(template);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                if (!Int32.TryParse(match.Groups["dataSelectorId"].Value, out var dataSelectorId) || dataSelectorId <= 0)
                {
                    logger.LogWarning($"Found dynamic content with invalid dataSelectorId of '{match.Groups["dataSelectorId"].Value}', so ignoring it.");
                    continue;
                }
                if (!Int64.TryParse(match.Groups["templateId"].Value, out var templateId) || templateId <= 0)
                {
                    logger.LogWarning($"Found dynamic content with invalid dataSelectorId of '{match.Groups["templateId"].Value}', so ignoring it.");
                    continue;
                }

                try
                {
                    var dataSelectorRequestModel = new DataSelectorRequestModel
                    {
                        DataSelectorId = dataSelectorId,
                        ContentItemId = templateId.ToString(),
                        ContentPropertyName = "template"
                    };
                    var (html, _, error) = await ToHtmlAsync(dataSelectorRequestModel);
                    if (!String.IsNullOrWhiteSpace(error))
                    {
                        html = $"An error occurred while generating data selector with id '{dataSelectorId}' and template '{templateId}': {error}";
                    }

                    template = template.Replace(match.Value, $"<!-- Start data selector with id {dataSelectorId} and template {templateId} -->{html}<!-- End data selector with id {dataSelectorId} and template {templateId} -->");
                }
                catch (Exception exception)
                {
                    logger.LogError($"An error while generating data selector with id '{dataSelectorId}' and template '{templateId}': {exception}");
                    var errorOnPage = $"An error occurred while generating data selector with id '{dataSelectorId}' and template '{templateId}'";
                    if (gclSettings.Environment is Environments.Development or Environments.Test)
                    {
                        errorOnPage += $": {exception.Message}";
                    }

                    template = template.Replace(match.Value, errorOnPage);
                }
            }

            return template;
        }

        #region Helper functions

        private async Task ProcessScopesAsync(ItemsRequest itemsRequest, IEnumerable<Scope> scopes, string joinDetailOn, string detailTableAliasPrefix, bool optionalConnection = false)
        {
            if (scopes == null)
            {
                return;
            }

            var queryAdditionBuilder = new StringBuilder(itemsRequest.QueryAddition);
            foreach (var scope in scopes)
            {
                if (scope?.ScopeRows == null || scope.ScopeRows.Length == 0)
                {
                    continue;
                }

                var queryPart = new StringBuilder();
                foreach (var row in scope.ScopeRows)
                {
                    if (queryPart.Length > 0)
                    {
                        queryPart.Append(" OR ");
                    }

                    var op = "";
                    var finalValue = "";

                    if (row.Key.IsReservedFieldName)
                    {
                        op = row.Operator.ToLowerInvariant() switch
                        {
                            "is not equal to" => "<>",
                            "is less than" => "<",
                            "is less than or equal to" => "<=",
                            "is greater than" => ">",
                            "is greater than or equal to" => ">=",
                            "is not empty" => "<>",
                            _ => "="
                        };

                        if (row.Value is JArray array)
                        {
                            var valueArray = array.ToObject<string[]>() ?? Array.Empty<string>();

                            if (row.Operator.Equals("is equal to", StringComparison.OrdinalIgnoreCase))
                            {
                                finalValue = $" IN ({String.Join(", ", valueArray.Select(v => v.ToMySqlSafeValue(true)))})";
                                op = "";
                            }
                            else if (row.Operator.Equals("is not equal to", StringComparison.OrdinalIgnoreCase))
                            {
                                finalValue = $" NOT IN ({String.Join(", ", valueArray.Select(v => v.ToMySqlSafeValue(true)))})";
                                op = "";
                            }
                            else
                            {
                                finalValue = valueArray[0];
                            }
                        }
                        else
                        {
                            finalValue = row.Value.ToString();
                        }
                    }
                    else if (!itemsRequest.FieldsInternal.Any(f => f.FieldName.Equals(row.Key.FieldName, StringComparison.OrdinalIgnoreCase)))
                    {
                        itemsRequest.FieldsInternal.Add(row.Key);
                    }

                    switch (row.Key.FieldName)
                    {
                        case "id":
                            {
                                if (!Decimal.TryParse(finalValue, out _) && !row.Operator.InList("is equal to", "is not equal to"))
                                {
                                    finalValue = finalValue.ToMySqlSafeValue(true);
                                }

                                switch (row.Operator.ToLowerInvariant())
                                {
                                    case "is empty":
                                        queryPart.Append($"{GetFormattedField(row.Key, joinDetailOn)} IS NULL ");
                                        break;
                                    case "is not empty":
                                        queryPart.Append($"{GetFormattedField(row.Key, joinDetailOn)} IS NOT NULL ");
                                        break;
                                    default:
                                        queryPart.Append($"{GetFormattedField(row.Key, joinDetailOn)} {op} {finalValue}");
                                        break;
                                }

                                break;
                            }
                        case "idencrypted":
                            {
                                finalValue = finalValue.Trim('\'');
                                try
                                {
                                    finalValue = finalValue.DecryptWithAesWithSalt();
                                }
                                catch
                                {
                                    // ignored
                                }

                                if (!Decimal.TryParse(finalValue, out _))
                                {
                                    finalValue = finalValue.ToMySqlSafeValue(true);
                                }

                                queryPart.Append($"{GetFormattedField(row.Key, joinDetailOn)} {op} {finalValue}");

                                break;
                            }
                        case "itemtitle":
                        case "changed_on":
                        case "changed_by":
                        case "unique_uuid":
                        case "item_ordering":
                            {
                                var finalFieldName = row.Key.FieldName switch
                                {
                                    "itemtitle" => "title",
                                    "item_ordering" => "ordering",
                                    _ => row.Key.FieldName
                                };

                                var finalJoinDetailOn = joinDetailOn.Replace(".id", $".{finalFieldName}").Replace(".destination_item_id", $"_item.{finalFieldName}");
                                var formattedField = GetFormattedField(row.Key, finalJoinDetailOn);

                                switch (row.Operator.ToLowerInvariant())
                                {
                                    case "contains":
                                        queryPart.Append($"{formattedField} LIKE '%{finalValue.ToMySqlSafeValue(false)}%'");
                                        break;
                                    case "does not contain":
                                        queryPart.Append($"{formattedField} NOT LIKE '%{finalValue.ToMySqlSafeValue(false)}%'");
                                        break;
                                    case "begin with":
                                        queryPart.Append($"{formattedField} LIKE '{finalValue.ToMySqlSafeValue(false)}%'");
                                        break;
                                    case "does not begin with":
                                        queryPart.Append($"{formattedField} NOT LIKE '{finalValue.ToMySqlSafeValue(false)}%'");
                                        break;
                                    case "end with":
                                        queryPart.Append($"{formattedField} LIKE '%{finalValue.ToMySqlSafeValue(false)}'");
                                        break;
                                    case "does not end with":
                                        queryPart.Append($"{formattedField} NOT LIKE '%{finalValue.ToMySqlSafeValue(false)}'");
                                        break;
                                    default:
                                        var finalPart = String.IsNullOrWhiteSpace(op) ? finalValue : $"{op} {finalValue.ToMySqlSafeValue(true)}";
                                        queryPart.Append($"{formattedField} {finalPart}");
                                        break;
                                }

                                break;
                            }
                        case "parentitemtitle":
                            {
                                // TODO: This doesn't work yet because the parent item tables are not joined.
                                break;
                            }
                        case "moduleid":
                            {
                                if (!Decimal.TryParse(finalValue, out _))
                                {
                                    finalValue = finalValue.ToMySqlSafeValue(true);
                                }

                                var finalJoinDetailOn = joinDetailOn.Replace(".id", ".moduleid").Replace(".destination_item_id", "_item.moduleid");
                                var formattedField = GetFormattedField(row.Key, finalJoinDetailOn);

                                switch (row.Operator.ToLowerInvariant())
                                {
                                    case "is empty":
                                        queryPart.Append($"{formattedField} IS NULL ");
                                        break;
                                    case "is not empty":
                                        queryPart.Append($"{formattedField} IS NOT NULL ");
                                        break;
                                    default:
                                        queryPart.Append($"{formattedField} {op} {finalValue}");
                                        break;
                                }
                                break;
                            }
                        case "link_ordering":
                        {
                            var finalJoinDetailOn = joinDetailOn.Replace(".id", $".ordering").Replace(".destination_item_id", $".ordering");
                            var formattedField = GetFormattedField(row.Key, finalJoinDetailOn);

                            switch (row.Operator.ToLowerInvariant())
                            {
                                case "contains":
                                    queryPart.Append($"{formattedField} LIKE '%{finalValue.ToMySqlSafeValue(false)}%'");
                                    break;
                                case "does not contain":
                                    queryPart.Append($"{formattedField} NOT LIKE '%{finalValue.ToMySqlSafeValue(false)}%'");
                                    break;
                                case "begin with":
                                    queryPart.Append($"{formattedField} LIKE '{finalValue.ToMySqlSafeValue(false)}%'");
                                    break;
                                case "does not begin with":
                                    queryPart.Append($"{formattedField} NOT LIKE '{finalValue.ToMySqlSafeValue(false)}%'");
                                    break;
                                case "end with":
                                    queryPart.Append($"{formattedField} LIKE '%{finalValue.ToMySqlSafeValue(false)}'");
                                    break;
                                case "does not end with":
                                    queryPart.Append($"{formattedField} NOT LIKE '%{finalValue.ToMySqlSafeValue(false)}'");
                                    break;
                                default:
                                    var finalPart = String.IsNullOrWhiteSpace(op) ? finalValue : $"{op} {finalValue.ToMySqlSafeValue(true)}";
                                    queryPart.Append($"{formattedField} {finalPart}");
                                    break;
                            }

                            break;
                        }
                        default:
                            row.Key.JoinOn = joinDetailOn;
                            row.Key.TableAliasPrefix = detailTableAliasPrefix;

                            if (itemsRequest.JoinDetail.All(f => f.TableAlias != row.Key.TableAlias))
                            {
                                itemsRequest.JoinDetail.Add(row.Key);
                            }

                            queryPart.Append(await CreateScopeRowQueryPartAsync(row));
                            break;
                    }
                }

                queryAdditionBuilder.Append(optionalConnection ? $" AND (({joinDetailOn} IS NULL) OR ({queryPart}))" : $" AND ({queryPart})");
            }

            itemsRequest.QueryAddition = queryAdditionBuilder.ToString();
        }

        private async Task ProcessConnectionsAsync(ItemsRequest itemsRequest, IReadOnlyList<Connection> connections, string joinOn, string prefix = "con", string selectAliasPrefix = "", string previousLevelTableAlias = "ilc1")
        {
            if (connections == null || connections.Count == 0)
            {
                return;
            }

            var queryPartLink = new StringBuilder();
            var queryPartItem = new StringBuilder();
            var count = 1;

            itemsRequest.WhereLink.Add("(");
            foreach (var connection in connections)
            {
                if (connection?.ConnectionRows == null || connection.ConnectionRows.Length == 0)
                {
                    itemsRequest.WhereLink.Add(" TRUE ");
                    continue;
                }

                if (!connection.Equals(connections[0]))
                {
                    itemsRequest.WhereLink.Add(" AND ");
                }

                itemsRequest.WhereLink.Add("(");

                foreach (var connectionRow in connection.ConnectionRows)
                {
                    var tablePrefix = "";
                    if (!String.IsNullOrWhiteSpace(connectionRow.EntityName) && itemsRequest.DedicatedTables.ContainsKey(connectionRow.EntityName))
                    {
                        tablePrefix = itemsRequest.DedicatedTables[connectionRow.EntityName];
                    }

                    if (!connectionRow.Equals(connection.ConnectionRows[0]))
                    {
                        itemsRequest.WhereLink.Add(" OR ");
                    }

                    var tableName = $"{prefix.ToMySqlSafeValue(false)}_{count}";

                    if (connectionRow.ItemIds is { Length: > 0 })
                    {
                        if (connectionRow.Modes.Contains("up"))
                        {
                            queryPartLink.Append(connectionRow.ItemIds.Length > 1 ? $"`{tableName}`.destination_item_id IN ({String.Join(", ", connectionRow.ItemIds)})" : $"`{tableName}`.destination_item_id = {String.Join(", ", connectionRow.ItemIds.Single())}");
                        }
                        else
                        {
                            queryPartLink.Append(connectionRow.ItemIds.Length > 1 ? $"`{tableName}`.item_id IN ({String.Join(", ", connectionRow.ItemIds)})" : $"`{tableName}`.item_id = {String.Join(", ", connectionRow.ItemIds.Single())}");
                        }
                    }
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(connectionRow.EntityName))
                        {
                            queryPartItem.Append($"`{tableName}_item`.entity_type = {connectionRow.EntityName.ToMySqlSafeValue(true)}");
                        }

                        if (connectionRow.TypeNumber > 0)
                        {
                            queryPartLink.Append($"`{tableName}`.type = {connectionRow.TypeNumber}");
                        }
                    }

                    // Fields of connection row.
                    itemsRequest.FieldsInternal.Add(new Field
                    {
                        JoinOn = "",
                        FieldName = "id",
                        TableAliasPrefix = $"{tableName}_",
                        SelectAliasPrefix = GetConnectionRowSelectAlias(connectionRow, tableName, selectAliasPrefix)
                    });

                    // The id of the connected row.
                    if (connectionRow.Fields is { Length: > 0 })
                    {
                        itemsRequest.FieldsInternal.AddRange(
                            connectionRow.Modes.Contains("up")
                                ? UpdateFieldsWithInternals($"`{tableName}`.destination_item_id", $"idv_{tableName}_", connectionRow.Fields, GetConnectionRowSelectAlias(connectionRow, tableName, selectAliasPrefix))
                                : UpdateFieldsWithInternals($"`{tableName}`.item_id", $"idv_{tableName}_", connectionRow.Fields, GetConnectionRowSelectAlias(connectionRow, tableName, selectAliasPrefix))
                        );
                    }

                    var linkFields = connectionRow.LinkFields?.ToList();
                    if (linkFields is { Count: > 0 })
                    {
                        if (linkFields.Any(lf => lf.FieldName == "id"))
                        {
                            itemsRequest.FieldsInternal.Add(new Field
                            {
                                JoinOn = "",
                                FieldName = "id",
                                TableAliasPrefix = tableName,
                                SelectAliasPrefix = GetConnectionRowSelectAlias(connectionRow, tableName, selectAliasPrefix),
                                IsLinkField = true
                            });
                            // Remove item from the list by re-creating the list without the ID field.
                            linkFields = linkFields.Where(lf => lf.FieldName != "id").ToList();
                        }

                        // All the specific fields specified on the connection.
                        itemsRequest.FieldsInternal.AddRange(UpdateFieldsWithInternals($"`{tableName}`.id", $"ldv_{tableName}_", linkFields, GetConnectionRowSelectAlias(connectionRow, tableName, selectAliasPrefix), true));
                    }

                    // Process the scope of the connection (constraint on connection).
                    await ProcessScopesAsync(itemsRequest, connectionRow.Scopes, connectionRow.Modes.Contains("up") ? $"`{tableName}`.destination_item_id" : $"`{tableName}`.item_id", $"idv_{tableName}_", connectionRow.Modes.Contains("optional"));

                    // Add "AND" to query part if query part is not empty.
                    if (queryPartLink.Length > 0)
                    {
                        queryPartLink.Insert(0, "AND ");
                    }

                    if (queryPartItem.Length > 0)
                    {
                        queryPartItem.Insert(0, "AND ");
                    }

                    // Add necessary joins to list.
                    if (connectionRow.Modes.Contains("up"))
                    {
                        var settings = itemsRequest.LinkTypeSettings.FirstOrDefault(l => l.Type == connectionRow.TypeNumber && l.DestinationEntityType.Equals(connectionRow.EntityName, StringComparison.OrdinalIgnoreCase) && l.SourceEntityType.Equals(itemsRequest.EntityTypes?.Split(',').First(), StringComparison.OrdinalIgnoreCase));
                        if (settings is { UseParentItemId: true })
                        {
                            itemsRequest.JoinLink.Add($"LEFT JOIN `{tablePrefix}{WiserTableNames.WiserItem}` AS `{tableName}_item` ON `{tableName}_item`.id = {previousLevelTableAlias}.parent_item_id AND `{tableName}_item`.published_environment & {itemsRequest.Environment} = {itemsRequest.Environment} {queryPartItem}");
                        }
                        else
                        {
                            itemsRequest.JoinLink.Add($"LEFT JOIN `{settings?.DedicatedTablePrefix ?? ""}{WiserTableNames.WiserItemLink}` AS {tableName} ON {tableName}.item_id = {joinOn} {queryPartLink}");
                            itemsRequest.JoinLink.Add($"LEFT JOIN `{tablePrefix}{WiserTableNames.WiserItem}` AS `{tableName}_item` ON `{tableName}_item`.id = {tableName}.destination_item_id AND `{tableName}_item`.published_environment & {itemsRequest.Environment} = {itemsRequest.Environment} {queryPartItem}");
                        }
                    }
                    else
                    {
                        var settings = itemsRequest.LinkTypeSettings.FirstOrDefault(l => l.Type == connectionRow.TypeNumber && l.DestinationEntityType.Equals(itemsRequest.EntityTypes?.Split(',').First(), StringComparison.OrdinalIgnoreCase) && l.SourceEntityType.Equals(connectionRow.EntityName, StringComparison.OrdinalIgnoreCase));
                        if (settings is { UseParentItemId: true })
                        {
                            itemsRequest.JoinLink.Add($"LEFT JOIN `{tablePrefix}{WiserTableNames.WiserItem}` AS `{tableName}_item` ON `{tableName}_item`.parent_item_id = {previousLevelTableAlias}.id AND `{tableName}_item`.published_environment & {itemsRequest.Environment} = {itemsRequest.Environment} {queryPartItem}");
                        }
                        else
                        {
                            itemsRequest.JoinLink.Add($"LEFT JOIN `{settings?.DedicatedTablePrefix ?? ""}{WiserTableNames.WiserItemLink}` AS {tableName} ON {tableName}.destination_item_id = {joinOn} {queryPartLink}");
                            itemsRequest.JoinLink.Add($"LEFT JOIN `{tablePrefix}{WiserTableNames.WiserItem}` AS `{tableName}_item` ON `{tableName}_item`.id = {tableName}.item_id AND `{tableName}_item`.published_environment & {itemsRequest.Environment} = {itemsRequest.Environment} {queryPartItem}");
                        }
                    }

                    itemsRequest.LinkTables.Add(tableName);

                    // If link (connection) is not optional, then check the id of the left join table in the where statement.
                    if (!connectionRow.Modes.Contains("optional"))
                    {
                        itemsRequest.WhereLink.Add($"`{tableName}_item`.id IS NOT NULL AND");
                    }

                    // Only use the deepest level in the where part --> Waarom?? Tijdelijk uitgeschakeld, deuren configurator IG, wel variant verplicht, glas optioneel
                    if (connectionRow.Connections == null || connectionRow.Connections.Length == 0)
                    {
                        itemsRequest.WhereLink.Add("TRUE");
                    }

                    // Empty internal variables.
                    queryPartLink.Clear();
                    queryPartItem.Clear();

                    // Recursive part.
                    if (connectionRow.Connections is { Length: > 0 })
                    {
                        await ProcessConnectionsAsync(itemsRequest, connectionRow.Connections, connectionRow.Modes.Contains("up") ? $"{tableName}.destination_item_id" : $"{tableName}.item_id", tableName, GetConnectionRowSelectAlias(connectionRow, tableName, selectAliasPrefix), $"{tableName}_item");
                    }

                    count += 1;
                }

                itemsRequest.WhereLink.Add(")");
            }

            itemsRequest.WhereLink.Add(")");
        }

        private static List<Field> UpdateFieldsWithInternals(string joinOn, string tableAliasPrefix, IEnumerable<Field> fields, string selectAliasPrefix = "", bool isLinkField = false, bool fieldsFromField = false)
        {
            var output = new List<Field>();
            foreach (var item in fields)
            {
                item.FieldFromField = fieldsFromField;
                item.IsLinkField = isLinkField;
                item.JoinOn = joinOn;
                item.TableAliasPrefix = tableAliasPrefix;
                item.SelectAliasPrefix = selectAliasPrefix;
                output.Add(item);
            }

            return output;
        }

        private static string GetFormattedField(Field field, string value)
        {
            // Data type part.
            var valuePart = field.DataType switch
            {
                "decimal" => "CONVERT(REPLACE({value}, ',', '.'), DECIMAL(65,30))",
                "datetime" => "CONVERT({value}, DATETIME)",
                _ => "{value}"
            };

            // Aggregation function part.
            string function;
            if (!String.IsNullOrWhiteSpace(field.AggregationFunction) && !field.AggregationFunction.Equals("distinct"))
            {
                function = field.AggregationFunction switch
                {
                    "countdistinct" => $"COUNT(DISTINCT {valuePart})",
                    _ => $"{field.AggregationFunction.ToUpper()}({valuePart})"
                };
            }
            else
            {
                function = valuePart;
            }

            // Formatting part.
            var result = String.IsNullOrWhiteSpace(field.Formatting) ? function : field.Formatting.Replace("{value}", function);

            // Return the formatted field.
            return result.Replace("{value}", value);
        }

        private async Task<string> CreateScopeRowQueryPartAsync(ScopeRow scopeRow)
        {
            var formattedField = GetFormattedField(scopeRow.Key, $"`{scopeRow.Key.TableAlias.ToMySqlSafeValue(false)}`.`value`");
            if (scopeRow.Value is JArray array)
            {
                var valueArray = array.ToObject<string[]>() ?? Array.Empty<string>();
                var inPart = String.Join(", ", valueArray.Select(v => v.ToMySqlSafeValue(true)));
                switch (scopeRow.Operator)
                {
                    case "is equal to":
                        return $"{formattedField} IN ({inPart})";
                    case "is not equal to":
                        return $"{formattedField} NOT IN ({inPart})";
                }
            }
            else
            {
                var value = await stringReplacementsService.DoAllReplacementsAsync(scopeRow.Value.ToString(), handleRequest: false, evaluateLogicSnippets: false, removeUnknownVariables: false);
                switch (scopeRow.Operator.ToLowerInvariant())
                {
                    case "is equal to":
                        return $"{formattedField} = {value.ToMySqlSafeValue(true)}";
                    case "is not equal to":
                        return $"{formattedField} <> {value.ToMySqlSafeValue(true)}";
                    case "is less than":
                        return $"{formattedField} < {value.ToMySqlSafeValue(true)}";
                    case "is less than or equal to":
                        return $"{formattedField} <= {value.ToMySqlSafeValue(true)}";
                    case "is greater than":
                        return $"{formattedField} > {value.ToMySqlSafeValue(true)}";
                    case "is greater than or equal to":
                        return $"{formattedField} >= {value.ToMySqlSafeValue(true)}";
                    case "contains":
                        return $"{formattedField} LIKE '%{value.ToMySqlSafeValue(false)}%'";
                    case "does not contain":
                        return $"{formattedField} NOT LIKE '%{value.ToMySqlSafeValue(false)}%'";
                    case "begin with":
                        return $"{formattedField} LIKE '{value.ToMySqlSafeValue(false)}%'";
                    case "does not begin with":
                        return $"{formattedField} NOT LIKE '{value.ToMySqlSafeValue(false)}%'";
                    case "end with":
                        return $"{formattedField} LIKE '%{value.ToMySqlSafeValue(false)}'";
                    case "does not end with":
                        return $"{formattedField} NOT LIKE '%{value.ToMySqlSafeValue(false)}'";
                    case "is empty":
                        return $"{formattedField} IS NULL OR {formattedField} = ''";
                    case "is not empty":
                        return $"{formattedField} IS NOT NULL AND {formattedField} <> ''";
                }
            }

            throw new ArgumentOutOfRangeException(scopeRow.Operator, $"GCL DataSelector: Unknown operator in scope: {scopeRow.Operator}");
        }

        private async Task<string> CreateHavingRowQueryPart(HavingRow havingRow, string selectAlias)
        {
            var formattedField = GetFormattedField(havingRow.Key, $"`{selectAlias}");

            if (havingRow.Value is JArray array)
            {
                var valueArray = array.ToObject<string[]>() ?? Array.Empty<string>();
                var inPart = String.Join(", ", valueArray.Select(v => v.ToMySqlSafeValue(true)));
                switch (havingRow.Operator)
                {
                    case "is equal to":
                        return $"{formattedField} IN ({inPart})";
                    case "is not equal to":
                        return $"{formattedField} NOT IN ({inPart})";
                }
            }
            else
            {
                var value = await stringReplacementsService.DoAllReplacementsAsync(havingRow.Value.ToString(), handleRequest: false, evaluateLogicSnippets: false, removeUnknownVariables: false);
                switch (havingRow.Operator.ToLowerInvariant())
                {
                    case "is equal to":
                        return $"{formattedField} = {value.ToMySqlSafeValue(true)}";
                    case "is not equal to":
                        return $"{formattedField} <> {value.ToMySqlSafeValue(true)}";
                    case "is less than":
                        return $"{formattedField} < {value.ToMySqlSafeValue(true)}";
                    case "is less than or equal to":
                        return $"{formattedField} <= {value.ToMySqlSafeValue(true)}";
                    case "is greater than":
                        return $"{formattedField} > {value.ToMySqlSafeValue(true)}";
                    case "is greater than or equal to":
                        return $"{formattedField} >= {value.ToMySqlSafeValue(true)}";
                    case "contains":
                        return $"{formattedField} LIKE '%{value.ToMySqlSafeValue(false)}%'";
                    case "does not contain":
                        return $"{formattedField} NOT LIKE '%{value.ToMySqlSafeValue(false)}%'";
                    case "begin with":
                        return $"{formattedField} LIKE '{value.ToMySqlSafeValue(false)}%'";
                    case "does not begin with":
                        return $"{formattedField} NOT LIKE '{value.ToMySqlSafeValue(false)}%'";
                    case "end with":
                        return $"{formattedField} LIKE '%{value.ToMySqlSafeValue(false)}'";
                    case "does not end with":
                        return $"{formattedField} NOT LIKE '%{value.ToMySqlSafeValue(false)}'";
                    case "is empty":
                        return $"{formattedField} IS NULL OR {formattedField} = ''";
                    case "is not empty":
                        return $"{formattedField} IS NOT NULL AND {formattedField} <> ''";
                }
            }

            throw new ArgumentOutOfRangeException(havingRow.Operator, $"GCL DataSelector: Unknown operator in having: {havingRow.Operator}");
        }

        private static string GetConnectionRowSelectAlias(ConnectionRow connectionRow, string fallback, string prefix = "")
        {
            if (!String.IsNullOrWhiteSpace(connectionRow.Name))
            {
                return $"{prefix}{connectionRow.Name}~";
            }

            return !String.IsNullOrWhiteSpace(connectionRow.EntityName)
                ? $"{prefix}{connectionRow.EntityName}~"
                : $"{prefix}{fallback}~";
        }

        /// <summary>
        /// Get the full select alias, table prefixes included.
        /// </summary>
        /// <param name="itemsRequest"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetFullSelectAlias(ItemsRequest itemsRequest, string key)
        {
            var selectAlias = key;
            foreach (var f in itemsRequest.FieldsInternal)
            {
                if (String.IsNullOrWhiteSpace(f.FieldAlias))
                {
                    if (f.FieldName == key)
                    {
                        selectAlias = f.SelectAlias;
                        break;
                    }
                }
                else if (f.FieldAlias == key)
                {
                    selectAlias = f.SelectAlias;
                    break;
                }
            }

            selectAlias = $"`{selectAlias.ToMySqlSafeValue(false)}`";

            return selectAlias;
        }

        /// <summary>
        /// Validates the hash.
        /// </summary>
        /// <returns></returns>
        private string ValidateHash(DataSelectorRequestModel data)
        {
            if (String.IsNullOrWhiteSpace(gclSettings.ExpiringEncryptionKey))
            {
                return "No decryption key found";
            }

            var parameters = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in typeof(DataSelectorRequestModel).GetProperties())
            {
                if (!property.PropertyType.IsValueType && property.GetValue(data) == null)
                {
                    continue;
                }
                if (Nullable.GetUnderlyingType(property.PropertyType) != null && property.GetValue(data) == null)
                {
                    continue;
                }
                if (property.Name.Equals("Hash"))
                {
                    continue;
                }

                parameters.Add(property.Name, Convert.ToString(property.GetValue(data)));
            }

            if (!parameters.ContainsKey("DateTime"))
            {
                return "No datetime parameter found";
            }

            // Check if the request hasn't expired yet.
            var dateTime = DateTime.ParseExact(parameters["DateTime"], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var hoursValid = gclSettings.TemporaryEncryptionHoursValid;
            if (DateTime.Now.Subtract(dateTime).TotalHours > hoursValid)
            {
                return "Expired";
            }

            var stringToHash = new StringBuilder();
            foreach (var (key, value) in parameters)
            {
                stringToHash.Append(key.ToLowerInvariant()).Append(value);
            }

            stringToHash.Append("secret").Append(gclSettings.ExpiringEncryptionKey);

            return !stringToHash.ToString().VerifySha512(data.Hash)
                ? "Invalid hash"
                : String.Empty;
        }

        #endregion
    }
}
