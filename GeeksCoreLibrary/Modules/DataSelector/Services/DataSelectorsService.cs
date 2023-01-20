using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Helpers;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Models;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.DataSelector.Services;

public class DataSelectorsService : IDataSelectorsService
{
    private readonly IDatabaseConnection databaseConnection;
    private readonly ITemplatesService templatesService;

    public DataSelectorsService(IDatabaseConnection databaseConnection, ITemplatesService templatesService)
    {
        this.databaseConnection = databaseConnection;
        this.templatesService = templatesService;
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

        if (itemsRequest.Selector?.Main == null || (String.IsNullOrWhiteSpace(itemsRequest.Selector.Main.EntityName) && (itemsRequest.Selector.Main.Scopes == null || itemsRequest.Selector.Main.Scopes.Length == 0) && (itemsRequest.Selector.Having == null || itemsRequest.Selector.Having.Length == 0) && String.IsNullOrWhiteSpace(itemsRequest.ContainsPath) && String.IsNullOrWhiteSpace(itemsRequest.EntityTypes)))
        {
            return String.Empty;
        }

        var selectQueryBuilder = new StringBuilder();
        var joinQueryBuilder = new StringBuilder();
        var whereQueryBuilder = new StringBuilder();

        // Handle main connection.
        var mainConnectionRow = new ConnectionRow
        {
            EntityName = itemsRequest.Selector.Main.EntityName,
            Fields = itemsRequest.Selector.Main.Fields,
            Scopes = itemsRequest.Selector.Main.Scopes,
            Modes = Array.Empty<string>()
        };

        var mainFieldQueryParts = await CreateQueryJoinsForConnectionFieldsAsync(mainConnectionRow, null);
        foreach (var fieldQueryPart in mainFieldQueryParts)
        {
            if (selectQueryBuilder.Length > 0) selectQueryBuilder.Append(", ");

            selectQueryBuilder.Append(fieldQueryPart.SelectQueryPart);
            if (!String.IsNullOrWhiteSpace(fieldQueryPart.JoinQueryPart))
            {
                joinQueryBuilder.AppendLine(fieldQueryPart.JoinQueryPart);
            }

            if (!String.IsNullOrWhiteSpace(fieldQueryPart.ScopesQueryPart))
            {
                whereQueryBuilder.Append($" AND {fieldQueryPart.ScopesQueryPart}");
            }
        }

        var connectionsQueryParts = new List<ConnectionForQuery>();
        if (itemsRequest.Selector.Connections != null)
        {
            connectionsQueryParts.AddRange(await CreateQueryJoinsAsync(itemsRequest.Selector.Connections, mainConnectionRow, new[] { new ConnectionIterationModel { Count = 1, PreviousEntityName = "" } }));
        }

        foreach (var queryPart in connectionsQueryParts)
        {
            // Add JOIN parts.
            joinQueryBuilder.Append(queryPart.JoinQueryPart);

            // Add SELECT parts.
            foreach (var fieldQueryPart in queryPart.Fields)
            {
                if (selectQueryBuilder.Length > 0) selectQueryBuilder.Append(", ");

                selectQueryBuilder.Append(fieldQueryPart.SelectQueryPart);
                if (!String.IsNullOrWhiteSpace(fieldQueryPart.JoinQueryPart))
                {
                    joinQueryBuilder.AppendLine(fieldQueryPart.JoinQueryPart);
                }

                if (!String.IsNullOrWhiteSpace(fieldQueryPart.ScopesQueryPart))
                {
                    whereQueryBuilder.Append($" AND {fieldQueryPart.ScopesQueryPart}");
                }
            }
        }

        var fullQueryBuilder = new StringBuilder();
        fullQueryBuilder.AppendLine($"SELECT {selectQueryBuilder}");
        fullQueryBuilder.AppendLine($"FROM `{WiserTableNames.WiserItem}` AS item_main");
        fullQueryBuilder.Append(joinQueryBuilder);
        fullQueryBuilder.AppendLine($"WHERE item_main.entity_type = {itemsRequest.Selector.Main.EntityName.ToMySqlSafeValue(true)} {whereQueryBuilder}");
        
        if (!String.IsNullOrWhiteSpace(itemsRequest.Selector.Limit) && !itemsRequest.Selector.Limit.Equals("0"))
        {
            var limitRegex = new Regex("^\\d+(?:,\\s*\\d+)?$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
            if (limitRegex.IsMatch(itemsRequest.Selector.Limit.Trim()))
            {
                fullQueryBuilder.AppendLine($"LIMIT {itemsRequest.Selector.Limit.Trim()}");
            }
        }

        return fullQueryBuilder.ToString();
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
    public async Task<(FileContentResult Result, HttpStatusCode StatusCode, string Error)> ToExcelAsync(DataSelectorRequestModel data)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<(string Result, HttpStatusCode StatusCode, string Error)> ToHtmlAsync(DataSelectorRequestModel data)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<(FileContentResult Result, HttpStatusCode StatusCode, string Error)> ToPdfAsync(DataSelectorRequestModel data)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<(ItemsRequest Result, HttpStatusCode StatusCode, string Error)> InitializeItemsRequestAsync(DataSelectorRequestModel data, bool skipSecurity = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<string> ReplaceAllDataSelectorsAsync(string template)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connections"></param>
    /// <param name="previousConnectionRow">The connection that is adding the new connections.</param>
    /// <param name="iterations"></param>
    /// <returns></returns>
    private async Task<IList<ConnectionForQuery>> CreateQueryJoinsAsync(ICollection<Connection> connections, ConnectionRow previousConnectionRow, ConnectionIterationModel[] iterations)
    {
        if (connections == null || !connections.Any())
        {
            return null;
        }

        var result = new List<ConnectionForQuery>();

        foreach (var connection in connections)
        {
            if (connection?.ConnectionRows == null || connection.ConnectionRows.Length == 0) continue;

            foreach (var connectionRow in connection.ConnectionRows)
            {
                if (connectionRow == null) continue;

                var connectionForQuery = new ConnectionForQuery();
                var joinStringBuilder = new StringBuilder();

                var iterationCounts = iterations.Select(it => it.Count).ToArray();

                // Always create a wiser_item JOIN. Determine if parent_item_id is used or if wiser_itemlink is used.
                var connectionIsParent = connectionRow.Modes.Contains("up", StringComparer.OrdinalIgnoreCase);
                var sourceEntityType = connectionIsParent
                    ? connectionRow.EntityName
                    : previousConnectionRow.EntityName;
                var destinationEntityType = connectionIsParent
                    ? previousConnectionRow.EntityName
                    : connectionRow.EntityName;
                
                var tablePrefix = await GetTablePrefixForEntityAsync(connectionRow.EntityName);
                var linkSettings = await GetLinkTypeSettingsAsync(connectionRow.TypeNumber, sourceEntityType, destinationEntityType);
                var joinPrefix = connectionRow.Modes.Contains("optional", StringComparer.OrdinalIgnoreCase) ? "LEFT " : String.Empty;
                var tableAlias = $"item_{String.Join("_", iterationCounts)}";
                var linkTableAlias = $"itemlink_{String.Join("_", iterationCounts)}";

                string previousItemTableAlias;
                if (iterationCounts.Length == 1)
                {
                    previousItemTableAlias = "item_main";
                }
                else
                {
                    var iterationCountsPrevious = new int[iterationCounts.Length - 1];
                    Array.Copy(iterationCounts, iterationCountsPrevious, iterationCountsPrevious.Length);
                    previousItemTableAlias = $"item_{String.Join("_", iterationCountsPrevious)}";
                }

                if (linkSettings.UseParentItemId)
                {
                    var sourceColumnName = connectionIsParent ? "id" : "parent_item_id";
                    var destinationColumnName = connectionIsParent ? "parent_item_id" : "id";

                    joinStringBuilder.AppendLine($"{joinPrefix}JOIN `{tablePrefix}{WiserTableNames.WiserItem}` AS `{tableAlias}` ON `{tableAlias}`.`{destinationColumnName}` = `{previousItemTableAlias}`.`{sourceColumnName}`");
                }
                else
                {
                    var sourceColumnName = connectionIsParent ? "destination_item_id" : "item_id";
                    var destinationColumnName = connectionIsParent ? "item_id" : "destination_item_id";

                    joinStringBuilder.AppendLine($"{joinPrefix}JOIN `{linkSettings.DedicatedTablePrefix}{WiserTableNames.WiserItemLink}` AS `{linkTableAlias}` ON `{linkTableAlias}`.`{destinationColumnName}` = `{previousItemTableAlias}`.`id`");
                    joinStringBuilder.AppendLine($"{joinPrefix}JOIN `{tablePrefix}{WiserTableNames.WiserItem}` AS `{tableAlias}` ON `{tableAlias}`.`id` = `{linkTableAlias}`.`{sourceColumnName}`");
                }

                connectionForQuery.JoinQueryPart = joinStringBuilder.ToString();
                connectionForQuery.Fields = await CreateQueryJoinsForConnectionFieldsAsync(connectionRow, iterations);

                result.Add(connectionForQuery);

                if (connectionRow.Connections is { Length: > 0 })
                {
                    // Add connections of this connection.
                    var newIterationCounts = new List<ConnectionIterationModel>(iterations) { new() { Count = 1, PreviousEntityName = connectionRow.EntityName } };

                    result.AddRange(await CreateQueryJoinsAsync(connectionRow.Connections, connectionRow, newIterationCounts.ToArray()));
                }

                // Increment the iteration count of the last one by 1.
                iterations[^1].Count += 1;
            }
        }

        return result;
    }

    private async Task<List<FieldForQuery>> CreateQueryJoinsForConnectionFieldsAsync(ConnectionRow connectionRow, ConnectionIterationModel[] iterations)
    {
        var finalFields = new List<Field>();
        if (!connectionRow.Fields.Any(field => field.FieldName.Equals("id", StringComparison.OrdinalIgnoreCase)))
        {
            finalFields.Add(new Field
            {
                FieldName = "id"
            });
        }
        finalFields.AddRange(connectionRow.Fields);

        var result = new List<FieldForQuery>(finalFields.Count);

        var tablePrefix = await GetTablePrefixForEntityAsync(connectionRow.EntityName);
        var isOptional = connectionRow.Modes.Contains("optional", StringComparer.OrdinalIgnoreCase);
        var joinPrefix = isOptional ? "LEFT " : String.Empty;

        var fieldIteration = 1;
        foreach (var field in finalFields)
        {
            fieldIteration++;

            var fieldForQuery = new FieldForQuery
            {
                Field = field
            };

            // Check which table should be used, wiser_item or wiser_itemdetail.
            var tableName = field.FieldName.InList("id", "idencrypted", "unique_uuid", "itemtitle", "changed_on", "changed_by")
                ? WiserTableNames.WiserItem
                : WiserTableNames.WiserItemDetail;

            var languageCodes = new List<string>(field.LanguageCodes ?? Array.Empty<string>());
            if (languageCodes.Count == 0)
            {
                languageCodes.Add(String.Empty);
            }

            var iterationCounts = iterations?.Select(it => it.Count).ToArray();
            var iterationPrefix = new StringBuilder();

            if (iterations != null)
            {
                iterationPrefix.Append(String.Join("~", iterations.Where(it => !String.IsNullOrWhiteSpace(it.PreviousEntityName)).Select(it => it.PreviousEntityName)));
                if (iterationPrefix.Length > 0) iterationPrefix.Append('~');
                iterationPrefix.Append($"{connectionRow.EntityName}~");
            }

            if (tableName.Equals(WiserTableNames.WiserItem))
            {
                var tableAlias = iterationCounts == null ? "item_main" : $"item_{String.Join("_", iterationCounts)}";

                fieldForQuery.SelectQueryPart = field.FieldName switch
                {
                    "itemtitle" => $"`{tableAlias}`.`title` AS `{iterationPrefix}{field.FieldAlias}`",
                    "idencrypted" => $"`{tableAlias}`.`id` AS `{iterationPrefix}{field.FieldAlias}_encrypt_withdate`",
                    _ => $"`{tableAlias}`.`{field.FieldName}` AS `{iterationPrefix}{field.FieldAlias}`"
                };
            }
            else
            {
                foreach (var tableAlias in languageCodes.Select(languageCode => FieldHelpers.CreateTableJoinAlias(field, iterations, fieldIteration, languageCode)))
                {
                    fieldForQuery.SelectQueryPart = $"CONCAT_WS('', `{tableAlias}`.`value`, `{tableAlias}`.`long_value`) AS `{iterationPrefix}{field.FieldAlias}`";

                    var joinOn = iterationCounts == null ? "item_main" : $"item_{String.Join("_", iterationCounts)}";
                    fieldForQuery.JoinQueryPart = $"{joinPrefix}JOIN `{tablePrefix}{tableName}` AS `{tableAlias}` ON `{tableAlias}`.item_id = {joinOn}.id";
                }
            }

            // Check if there are scopes specifically for this field.
            var scopeQuery = new StringBuilder();
            if (connectionRow.Scopes != null)
            {
                foreach (var scope in connectionRow.Scopes)
                {
                    if (scope.ScopeRows == null || scope.ScopeRows.Length == 0) continue;

                    var scopeRows = scope.ScopeRows.Where(sr => sr.Key.FieldName.Equals(field.FieldName, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (scopeRows.Count == 0)
                    {
                        continue;
                    }

                    scopeQuery.Append(" AND (");

                    foreach (var scopeRow in scopeRows)
                    {
                        if (scopeQuery.Length > 0)
                        {
                            scopeQuery.Append(" OR ");
                        }

                        var op = ConvertOperator(scopeRow.Operator);
                        if (scopeRow.Value is JArray array)
                        {
                            var valueArray = array.ToObject<string[]>() ?? Array.Empty<string>();

                            switch (op)
                            {
                                case "=":
                                    scopeQuery.Append($"`{field.FieldAlias}` IN ({String.Join(", ", valueArray.Select(v => v.ToMySqlSafeValue(true)))})");
                                    break;
                                case "<>":
                                    scopeQuery.Append($"`{field.FieldAlias}` NOT IN ({String.Join(", ", valueArray.Select(v => v.ToMySqlSafeValue(true)))})");
                                    break;
                                default:
                                    scopeQuery.Append($"`{field.FieldAlias}` {op} {(valueArray.FirstOrDefault() ?? String.Empty).ToMySqlSafeValue(true)}");
                                    break;
                            }
                        }
                        else
                        {
                            scopeQuery.Append($"`{field.FieldAlias}` {op} {Convert.ToString(scopeRow.Value).ToMySqlSafeValue(true)}");
                        }
                    }

                    scopeQuery.Append(')');
                }
            }

            fieldForQuery.ScopesQueryPart = scopeQuery.ToString();

            result.Add(fieldForQuery);
        }

        // Handle scopes that reference fields that aren't in the selected fields.
        var queryPart = new StringBuilder();
        if (connectionRow.Scopes != null)
        {
            foreach (var scope in connectionRow.Scopes)
            {
                if (scope.ScopeRows == null || scope.ScopeRows.Length == 0) continue;

                var connectionRowFields = finalFields.Select(field => field.FieldName);

                var scopeRows = scope.ScopeRows.Where(sr => !connectionRowFields.Contains(sr.Key.FieldName)).ToList();
                if (scopeRows.Count == 0)
                {
                    continue;
                }

                foreach (var scopeRow in scope.ScopeRows)
                {
                    if (queryPart.Length > 0)
                    {
                        queryPart.Append(" OR ");
                    }

                    var op = ConvertOperator(scopeRow.Operator);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Converts the string version of an operator to a MySQL operator.
    /// </summary>
    /// <param name="operatorString">The operator of a scope.</param>
    /// <returns></returns>
    private static string ConvertOperator(string operatorString)
    {
        return operatorString.ToLowerInvariant() switch
        {
            "is not equal to" => "<>",
            "is less than" => "<",
            "is less than or equal to" => "<=",
            "is greater than" => ">",
            "is greater than or equal to" => ">=",
            "is not empty" => "<>",
            _ => "="
        };
    }

    private async Task<string> GetTablePrefixForEntityAsync(string entityName)
    {
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("entityName", entityName);
        var getEntitySettingsResult = await databaseConnection.GetAsync($"SELECT `name`, dedicated_table_prefix FROM `{WiserTableNames.WiserEntity}`");
        var dataRow = getEntitySettingsResult.Rows.Cast<DataRow>().FirstOrDefault(dataRow => dataRow.Field<string>("name").Equals(entityName, StringComparison.OrdinalIgnoreCase));

        if (dataRow == null) return String.Empty;

        var prefix = dataRow.Field<string>("dedicated_table_prefix");
        if (!String.IsNullOrWhiteSpace(prefix) && !prefix.EndsWith('_')) prefix = $"{prefix}_";

        return prefix ?? String.Empty;
    }

    private async Task<LinkTypeSettings> GetLinkTypeSettingsAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
    {
        var settings = new LinkTypeSettings
        {
            Type = linkType,
            DestinationEntityType = destinationEntityType,
            SourceEntityType = sourceEntityType
        };
        
        var getLinkTypesResult = await databaseConnection.GetAsync($"SELECT type, destination_entity_type, connected_entity_type, use_item_parent_id, use_dedicated_table FROM `{WiserTableNames.WiserLink}`");
        foreach (DataRow dataRow in getLinkTypesResult.Rows)
        {
            var typeNumber = dataRow.Field<int>("type");
            var linkDestinationEntityType = dataRow.Field<string>("destination_entity_type");
            var linkSourceEntityType = dataRow.Field<string>("connected_entity_type");

            if (!linkDestinationEntityType.Equals(settings.DestinationEntityType, StringComparison.OrdinalIgnoreCase) || !linkSourceEntityType.Equals(settings.SourceEntityType, StringComparison.OrdinalIgnoreCase))
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

        return settings;
    }
}