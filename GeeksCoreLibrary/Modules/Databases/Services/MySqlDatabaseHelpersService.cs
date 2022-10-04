using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Exceptions;
using GeeksCoreLibrary.Modules.Databases.Extensions;
using GeeksCoreLibrary.Modules.Databases.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace GeeksCoreLibrary.Modules.Databases.Services
{
    /// <inheritdoc cref="IDatabaseHelpersService" />.
    public class MySqlDatabaseHelpersService : IDatabaseHelpersService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly ILogger<MySqlDatabaseHelpersService> logger;

        /// <summary>
        /// Creates a new instance of <see cref="MySqlDatabaseHelpersService"/>.
        /// </summary>
        public MySqlDatabaseHelpersService(IDatabaseConnection databaseConnection, ILogger<MySqlDatabaseHelpersService> logger)
        {
            this.databaseConnection = databaseConnection;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> ColumnExistsAsync(string tableName, string columnName, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            databaseConnection.AddParameter("columnName", columnName);
            
            var dataTable = await databaseConnection.GetAsync($"SHOW COLUMNS FROM `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}` LIKE ?columnName");
            return dataTable.Rows.Count > 0;
        }
        
        /// <inheritdoc />
        public async Task<List<string>> GetColumnNamesAsync(string tableName, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            var dataTable = await databaseConnection.GetAsync($"SHOW COLUMNS FROM `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}`");
            return dataTable.Rows.Cast<DataRow>().Select(dataRow => dataRow.Field<string>("Field")).ToList();
        }

        /// <inheritdoc />
        public async Task AddColumnToTableAsync(string tableName, ColumnSettingsModel settings, bool throwExceptionIfColumnAlreadyExists = true, string databaseName = null)
        {
            await AddColumnToTableAsync(this, tableName, settings, throwExceptionIfColumnAlreadyExists, databaseName);
        }

        /// <inheritdoc />
        public async Task AddColumnToTableAsync(IDatabaseHelpersService databaseHelpersService, string tableName, ColumnSettingsModel settings, bool throwExceptionIfColumnAlreadyExists = true, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(settings?.Name))
            {
                throw new ArgumentException("No column name given.");
            }
            
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            
            databaseConnection.AddParameter("columnName", settings.Name);
            databaseConnection.AddParameter("defaultValue", settings.DefaultValue);
            
            if (await databaseHelpersService.ColumnExistsAsync(tableName, settings.Name, databaseName))
            {
                if (throwExceptionIfColumnAlreadyExists)
                {
                    throw new DatabaseColumnExistsException(tableName, settings.Name);
                }

                return;
            }
            
            var queryBuilder = new StringBuilder($"ALTER TABLE `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}` ADD COLUMN ");
            queryBuilder.Append(GenerateColumnQueryPart(tableName, settings));
            queryBuilder.Append("; ");

            if (settings.AddIndex)
            {
                var indexTypeName = settings.IndexType.ToMySqlString();

                var name = settings.Name;
                if (name.Length > 60)
                {
                    name = name[..64];
                }

                queryBuilder.Append($"CREATE {indexTypeName} INDEX `idx_{name.ToMySqlSafeValue(false)}` ON `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}` (?columnName);");
            }

            await databaseConnection.ExecuteAsync(queryBuilder.ToString());
        }

        /// <inheritdoc />
        public async Task DropColumnAsync(string tableName, string columnName, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            databaseConnection.AddParameter("columnName", columnName);
            await databaseConnection.ExecuteAsync($"ALTER TABLE `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}` DROP COLUMN ?columnName");
        }

        /// <inheritdoc />
        public async Task CreateTableAsync(string tableName, IList<ColumnSettingsModel> primaryKeys, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            var queryBuilder = new StringBuilder($"CREATE TABLE IF NOT EXISTS `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}`");
            if (primaryKeys != null && primaryKeys.Any())
            {
                queryBuilder.AppendLine(" (");
                foreach (var primaryKey in primaryKeys)
                {
                    queryBuilder.AppendLine($"{GenerateColumnQueryPart(tableName, primaryKey)},");
                }

                queryBuilder.AppendLine($"PRIMARY KEY (`{String.Join("`,`", primaryKeys.Select(p => p.Name.ToMySqlSafeValue(false)))}`)");
                queryBuilder.AppendLine(")");
            }

            queryBuilder.AppendLine("ENGINE = INNODB");
            if (!String.IsNullOrWhiteSpace(characterSet))
            {
                databaseConnection.AddParameter("characterSet", characterSet);
                queryBuilder.Append("CHARACTER SET ?characterSet");

                if (!String.IsNullOrWhiteSpace(collation))
                {
                    databaseConnection.AddParameter("collation", collation);
                    queryBuilder.Append(" COLLATE ?collation");
                }

                queryBuilder.AppendLine();
            }

            await databaseConnection.ExecuteAsync(queryBuilder.ToString());
        }
        
        /// <inheritdoc />
        public async Task CreateOrUpdateTableAsync(string tableName, IList<ColumnSettingsModel> columns, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null)
        {
            await CreateOrUpdateTableAsync(this, tableName, columns, characterSet, collation, databaseName);
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateTableAsync(IDatabaseHelpersService databaseHelpersService, string tableName, IList<ColumnSettingsModel> columns, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null)
        {
            var primaryKeys = columns.Where(c => c.IsPrimaryKey).ToList();
            var isNewTable = false;
            List<ColumnSettingsModel> columnsToAdd;

            if (!await databaseHelpersService.TableExistsAsync(tableName, databaseName))
            {
                // Create the table if it doesn't exist yet.
                await databaseHelpersService.CreateTableAsync(tableName, primaryKeys, characterSet, collation, databaseName);
                isNewTable = true;
                columnsToAdd = columns.Where(c => !c.IsPrimaryKey).ToList();
            }
            else
            {
                var existingColumnNames = await databaseHelpersService.GetColumnNamesAsync(tableName, databaseName);
                columnsToAdd = columns.Where(c => !existingColumnNames.Any(x => x.Equals(c.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            // Add missing columns.
            foreach (var column in columnsToAdd)
            {
                await databaseHelpersService.AddColumnToTableAsync(tableName, column, false, databaseName);
            }

            // Update primary key if needed.
            if (isNewTable)
            {
                // If it's a new table, we already added the primary keys.
                return;
            }
            
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            var dataTable = await databaseConnection.GetAsync($"SHOW KEYS FROM `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}` WHERE KEY_NAME = 'PRIMARY'");
            var primaryKeyIsChanged = dataTable.Rows.Count != primaryKeys.Count || dataTable.Rows.Cast<DataRow>().Any(d => !primaryKeys.Any(p => p.Name.Equals(d.Field<string>("column_name"))));
            if (!primaryKeyIsChanged)
            {
                return;
            }

            await databaseConnection.ExecuteAsync($"ALTER TABLE `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}` DROP PRIMARY KEY, ADD PRIMARY KEY (`{String.Join("`,`", primaryKeys.Select(x => x.Name.ToMySqlSafeValue(false)))}`)");
        }

        /// <inheritdoc />
        public async Task<bool> TableExistsAsync(string tableName, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }

            var databaseClause = "";
            if (!String.IsNullOrWhiteSpace(databaseName))
            {
                databaseClause = "AND TABLE_SCHEMA = ?databaseName";
                databaseConnection.AddParameter("databaseName", databaseName);
            }

            databaseConnection.AddParameter("tableName", tableName);
            var dataTable = await databaseConnection.GetAsync($"SELECT TABLE_NAME FROM information_schema.`TABLES` WHERE TABLE_NAME = ?tableName {databaseClause}");
            return dataTable.Rows.Count > 0;
        }
        
        /// <inheritdoc />
        public async Task<bool> DatabaseExistsAsync(string databaseName)
        {
            var dataTable = await databaseConnection.GetAsync($"SELECT NULL FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = {databaseName.ToMySqlSafeValue(true)}");
            return dataTable.Rows.Count > 0;
        }
        
        /// <inheritdoc />
        public async Task DropTableAsync(string tableName, bool isTemporaryTable = false, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            await databaseConnection.ExecuteAsync($"DROP {(isTemporaryTable ? "TEMPORARY" : "")} TABLE IF EXISTS `{databaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}`");
        }

        /// <inheritdoc />
        public async Task DuplicateTableAsync(string tableToDuplicate, string newTableName, bool includeData = true, string sourceDatabaseName = null, string destinationDatabaseName = null)
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            
            if (String.IsNullOrWhiteSpace(sourceDatabaseName))
            {
                sourceDatabaseName = databaseConnection.ConnectedDatabase;
            }
            if (String.IsNullOrWhiteSpace(destinationDatabaseName))
            {
                destinationDatabaseName = databaseConnection.ConnectedDatabase;
            }
            await databaseConnection.ExecuteAsync($"CREATE TABLE `{destinationDatabaseName.ToMySqlSafeValue(false)}`.`{newTableName.ToMySqlSafeValue(false)}` LIKE `{sourceDatabaseName.ToMySqlSafeValue(false)}`.`{tableToDuplicate.ToMySqlSafeValue(false)}`");
            if (!includeData)
            {
                return;
            }

            await databaseConnection.ExecuteAsync($"INSERT INTO `{destinationDatabaseName.ToMySqlSafeValue(false)}`.`{newTableName.ToMySqlSafeValue(false)}` (SELECT * FROM `{sourceDatabaseName.ToMySqlSafeValue(false)}`.`{tableToDuplicate.ToMySqlSafeValue(false)}`)");
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateIndexesAsync(List<IndexSettingsModel> indexes, string databaseName = null)
        {
            if (indexes == null || !indexes.Any())
            {
                throw new ArgumentNullException(nameof(indexes));
            }

            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            var oldIndexes = new Dictionary<string, List<(string Name, List<string> Columns)>>();
            
            foreach (var index in indexes.Where(index => !String.IsNullOrWhiteSpace(index.Name) && index.Fields != null && index.Fields.Any()))
            {
                var createIndexQuery = $"ALTER TABLE `{databaseName.ToMySqlSafeValue(false)}`.`{index.TableName.ToMySqlSafeValue(false)}` ADD {index.Type.ToMySqlString()} INDEX `{index.Name.ToMySqlSafeValue(false)}` (`{String.Join("`,`", index.Fields.Select(f => f.ToMySqlSafeValue(false)))}`)";
                
                databaseConnection.AddParameter("indexName", index.Name);

                if (!oldIndexes.ContainsKey(index.TableName))
                {
                    oldIndexes.Add(index.TableName, new List<(string Name, List<string> Columns)>());
                    var dataTable = await databaseConnection.GetAsync($"SHOW INDEX FROM `{databaseName.ToMySqlSafeValue(false)}`.`{index.TableName.ToMySqlSafeValue(false)}`");
                    foreach (var dataRow in dataTable.Rows.Cast<DataRow>().OrderBy(row => row.Field<string>("key_name")).ThenBy(row => Convert.ToInt32(row["seq_in_index"])))
                    {
                        var indexName = dataRow.Field<string>("key_name");
                        var oldIndex = oldIndexes[index.TableName].FirstOrDefault(i => i.Name == indexName);
                        if (oldIndex.Name == null)
                        {
                            oldIndex = (indexName, new List<string>());
                            oldIndexes[index.TableName].Add(oldIndex);
                        }

                        var columnName = dataRow.Field<string>("column_name");
                        oldIndex.Columns.Add(columnName);
                    }
                }

                var existingIndex = oldIndexes[index.TableName].FirstOrDefault(i => String.Equals(i.Name, index.Name, StringComparison.OrdinalIgnoreCase) || String.Equals(String.Join(",", i.Columns.OrderBy(c => c)), String.Join(",", index.Fields.OrderBy(f => f)), StringComparison.OrdinalIgnoreCase));
                var recreateIndex = existingIndex.Name == null || String.Join(",", existingIndex.Columns) != String.Join(",", index.Fields);
                if (!recreateIndex)
                {
                    // Index has not been changed, so do nothing.
                    continue;
                }

                if (existingIndex.Name != null)
                {
                    // If an index with this name already exists, but the new one if different, drop the old index first.
                    await databaseConnection.ExecuteAsync($"ALTER TABLE `{databaseName.ToMySqlSafeValue(false)}`.`{index.TableName.ToMySqlSafeValue(false)}` DROP INDEX `{existingIndex.Name.ToMySqlSafeValue(false)}`");
                }

                // Create the new index.
                await databaseConnection.ExecuteAsync(createIndexQuery);
            }
        }

        /// <inheritdoc />
        public async Task CreateDatabaseAsync(string databaseName, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci")
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            
            databaseConnection.AddParameter("characterSet", characterSet);
            databaseConnection.AddParameter("collation", collation);
            await databaseConnection.ExecuteAsync($"CREATE DATABASE IF NOT EXISTS `{databaseName.ToMySqlSafeValue(false)}` DEFAULT CHARACTER SET = ?characterSet DEFAULT COLLATE = ?collation");
        }

        /// <inheritdoc />
        public async Task DropDatabaseAsync(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            
            await databaseConnection.ExecuteAsync($"DROP DATABASE IF EXISTS `{databaseName.ToMySqlSafeValue(false)}`");
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, DateTime>> GetLastTableUpdatesAsync(string databaseName = null)
        {
            return await GetLastTableUpdatesAsync(this, databaseName);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, DateTime>> GetLastTableUpdatesAsync(IDatabaseHelpersService databaseHelpersService, string databaseName = null)
        {
            var result = new Dictionary<string, DateTime>();

            if (!await databaseHelpersService.TableExistsAsync(WiserTableNames.WiserTableChanges, databaseName))
            {
                await databaseHelpersService.CreateOrUpdateTableAsync(WiserTableNames.WiserTableChanges,
                                                                     new List<ColumnSettingsModel>
                                                                     {
                                                                         new()
                                                                         {
                                                                             Name = "name",
                                                                             Type = MySqlDbType.VarChar,
                                                                             Length = 100,
                                                                             NotNull = true,
                                                                             IsPrimaryKey = true
                                                                         },
                                                                         new()
                                                                         {
                                                                             Name = "last_update",
                                                                             Type = MySqlDbType.DateTime,
                                                                             NotNull = true
                                                                         }
                                                                     }, 
                                                                     databaseName: databaseName);

                return result;
            }

            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }
            var dataTable = await databaseConnection.GetAsync($"SELECT name, last_update FROM `{databaseName.ToMySqlSafeValue(false)}`.`{WiserTableNames.WiserTableChanges}`");
            if (dataTable.Rows.Count == 0)
            {
                return result;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var tableName = dataRow.Field<string>("name")?.ToLowerInvariant();
                if (String.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                result.Add(tableName, dataRow.Field<DateTime>("last_update"));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task CheckAndUpdateTablesAsync(List<string> tablesToUpdate, string databaseName = null)
        {
            await CheckAndUpdateTablesAsync(this, tablesToUpdate, databaseName);
        }

        /// <inheritdoc />
        public async Task CheckAndUpdateTablesAsync(IDatabaseHelpersService databaseHelpersService, List<string> tablesToUpdate, string databaseName = null)
        {
            var tableChanges = await databaseHelpersService.GetLastTableUpdatesAsync(databaseName);
            
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }

            foreach (var tableName in tablesToUpdate)
            {
                var tableDefinition = WiserTableDefinitions.TablesToUpdate.FirstOrDefault(t => String.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));
                if (tableDefinition == null)
                {
                    // If we don't know 
                    logger.LogWarning($"Called CheckAndUpdateTablesAsync with a table that doesn't exist ('{tableName}').");
                    continue;
                }

                if (tableChanges.ContainsKey(tableName.ToLowerInvariant()) && tableChanges[tableName.ToLowerInvariant()] >= tableDefinition.LastUpdate)
                {
                    // The table is already up-to-date.
                    continue;
                }

                // Table is not up-to-date, so update it now.
                await CreateOrUpdateTableAsync(tableName, tableDefinition.Columns, tableDefinition.CharacterSet, tableDefinition.Collation, databaseName);
                if (tableDefinition.Indexes != null && tableDefinition.Indexes.Any())
                {
                    await CreateOrUpdateIndexesAsync(tableDefinition.Indexes, databaseName);
                }

                // Update archive table.
                if (WiserTableNames.TablesWithArchive.Contains(tableName))
                {
                    await CreateOrUpdateTableAsync($"{tableName}{WiserTableNames.ArchiveSuffix}", tableDefinition.Columns, tableDefinition.CharacterSet, tableDefinition.Collation, databaseName);
                    if (tableDefinition.Indexes != null && tableDefinition.Indexes.Any())
                    {
                        tableDefinition.Indexes.ForEach(index => index.TableName += WiserTableNames.ArchiveSuffix);
                        await CreateOrUpdateIndexesAsync(tableDefinition.Indexes, databaseName);
                        tableDefinition.Indexes.ForEach(index => index.TableName = tableName);
                    }
                }
                
                // Update dedicated entity tables.
                if (WiserTableNames.TablesThatCanHaveEntityPrefix.Contains(tableName))
                {
                    var query = $@"SELECT DISTINCT dedicated_table_prefix FROM {WiserTableNames.WiserEntity} WHERE dedicated_table_prefix IS NOT NULL AND dedicated_table_prefix != ''";
                    var dataTable = await databaseConnection.GetAsync(query);
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        var tablePrefix = dataRow.Field<string>("dedicated_table_prefix");
                        if (!tablePrefix!.EndsWith("_"))
                        {
                            tablePrefix += "_";
                        }
                        
                        // Normal tables.
                        await CreateOrUpdateTableAsync($"{tablePrefix}{tableName}", tableDefinition.Columns, tableDefinition.CharacterSet, tableDefinition.Collation, databaseName);
                        if (tableDefinition.Indexes != null && tableDefinition.Indexes.Any())
                        {
                            tableDefinition.Indexes.ForEach(index => index.TableName = $"{tablePrefix}{index.TableName}");
                            await CreateOrUpdateIndexesAsync(tableDefinition.Indexes, databaseName);
                            tableDefinition.Indexes.ForEach(index => index.TableName = tableName);
                        }

                        // Archive tables.
                        await CreateOrUpdateTableAsync($"{tablePrefix}{tableName}{WiserTableNames.ArchiveSuffix}", tableDefinition.Columns, tableDefinition.CharacterSet, tableDefinition.Collation, databaseName);
                        if (tableDefinition.Indexes != null && tableDefinition.Indexes.Any())
                        {
                            tableDefinition.Indexes.ForEach(index => index.TableName = $"{tablePrefix}{index.TableName}{WiserTableNames.ArchiveSuffix}");
                            await CreateOrUpdateIndexesAsync(tableDefinition.Indexes, databaseName);
                            tableDefinition.Indexes.ForEach(index => index.TableName = tableName);
                        }
                    }
                }
                
                // Update dedicated link tables.
                if (WiserTableNames.TablesThatCanHaveLinkPrefix.Contains(tableName))
                {
                    var query = $@"SELECT DISTINCT type FROM {WiserTableNames.WiserLink} WHERE use_dedicated_table = 1";
                    var dataTable = await databaseConnection.GetAsync(query);
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        var tablePrefix = $"{dataRow["type"]}_";
                        
                        // Normal tables.
                        await CreateOrUpdateTableAsync($"{tablePrefix}{tableName}", tableDefinition.Columns, tableDefinition.CharacterSet, tableDefinition.Collation, databaseName);
                        if (tableDefinition.Indexes != null && tableDefinition.Indexes.Any())
                        {
                            tableDefinition.Indexes.ForEach(index => index.TableName = $"{tablePrefix}{index.TableName}");
                            await CreateOrUpdateIndexesAsync(tableDefinition.Indexes, databaseName);
                            tableDefinition.Indexes.ForEach(index => index.TableName = tableName);
                        }

                        // Archive tables.
                        await CreateOrUpdateTableAsync($"{tablePrefix}{tableName}{WiserTableNames.ArchiveSuffix}", tableDefinition.Columns, tableDefinition.CharacterSet, tableDefinition.Collation, databaseName);
                        if (tableDefinition.Indexes != null && tableDefinition.Indexes.Any())
                        {
                            tableDefinition.Indexes.ForEach(index => index.TableName = $"{tablePrefix}{index.TableName}{WiserTableNames.ArchiveSuffix}");
                            await CreateOrUpdateIndexesAsync(tableDefinition.Indexes, databaseName);
                            tableDefinition.Indexes.ForEach(index => index.TableName = tableName);
                        }
                    }
                }

                // Update wiser_table_changes.
                databaseConnection.AddParameter("tableName", tableName);
                databaseConnection.AddParameter("lastUpdate", DateTime.Now);
                await databaseConnection.ExecuteAsync($@"INSERT INTO `{databaseName.ToMySqlSafeValue(false)}`.`{WiserTableNames.WiserTableChanges}` (name, last_update) 
                                                            VALUES (?tableName, ?lastUpdate) 
                                                            ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)");
            }
        }

        /// <summary>
        /// Generates part of a query for adding a column to a database table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="settings">The column settings.</param>
        /// <returns>A <see cref="StringBuilder"/> with the query part.</returns>
        private StringBuilder GenerateColumnQueryPart(string tableName, ColumnSettingsModel settings)
        {
            var parameterSuffix = $"_{settings.Name}";
            var (databaseType, unsigned) = MySqlDbTypeToDatabaseValue(settings.Type);
            var queryBuilder = new StringBuilder();
            switch (settings.Type)
            {
                case MySqlDbType.Double:
                case MySqlDbType.Decimal:
                case MySqlDbType.Float:
                    var columnLength = settings.Length == 0 ? "" : $"({settings.Length}, {settings.Decimals})";
                    queryBuilder.Append($"`{settings.Name.ToMySqlSafeValue(false)}` {databaseType}{columnLength}");
                    break;
                case MySqlDbType.Time:
                case MySqlDbType.Timestamp:
                case MySqlDbType.DateTime:
                case MySqlDbType.Year:
                case MySqlDbType.TinyBlob:
                case MySqlDbType.Blob:
                case MySqlDbType.MediumBlob:
                case MySqlDbType.TinyText:
                case MySqlDbType.MediumText:
                case MySqlDbType.Text:
                case MySqlDbType.LongText:
                    queryBuilder.Append($"`{settings.Name.ToMySqlSafeValue(false)}` {databaseType}");
                    break;
                case MySqlDbType.Enum:
                    if (settings.EnumValues == null || !settings.EnumValues.Any())
                    {
                        throw new DatabaseColumnMissingEnumValuesException(tableName, settings.Name);
                    }

                    queryBuilder.Append($"`{settings.Name.ToMySqlSafeValue(false)}` {databaseType}({String.Join(",", settings.EnumValues.Select(v => v.ToMySqlSafeValue(true)))})");

                    break;
                default:
                    queryBuilder.Append($"`{settings.Name.ToMySqlSafeValue(false)}` {databaseType}");
                    if (settings.Length > 0)
                    {
                        queryBuilder.Append($"({settings.Length})");
                    }

                    break;
            }

            if (settings.Type.InList(MySqlDbType.TinyText, MySqlDbType.Text, MySqlDbType.MediumText, MySqlDbType.LongText, MySqlDbType.VarChar, MySqlDbType.String, MySqlDbType.VarString))
            {
                if (!String.IsNullOrWhiteSpace(settings.CharacterSet))
                {
                    queryBuilder.Append($" CHARACTER SET {settings.CharacterSet.ToMySqlSafeValue(true)}");

                    if (!String.IsNullOrWhiteSpace(settings.Collation))
                    {
                        queryBuilder.Append($" COLLATE {settings.Collation.ToMySqlSafeValue(true)}");
                    }
                }
            }

            if (unsigned)
            {
                queryBuilder.Append(" UNSIGNED");
            }

            if (settings.NotNull)
            {
                queryBuilder.Append(" NOT NULL");
            }

            if (settings.DefaultValue != null)
            {
                if (settings.DefaultValue.Equals("CURRENT_TIMESTAMP"))
                {
                    queryBuilder.Append(" DEFAULT CURRENT_TIMESTAMP");
                }
                else if (settings.Type == MySqlDbType.Enum)
                {
                    if (settings.EnumValues == null || !settings.EnumValues.Any())
                    {
                        throw new DatabaseColumnMissingEnumValuesException(tableName, settings.Name);
                    }

                    if (!String.IsNullOrEmpty(settings.DefaultValue) && settings.EnumValues.Contains(settings.DefaultValue))
                    {
                        // Given default value exists in the list, so use it.
                        queryBuilder.Append($" DEFAULT {settings.DefaultValue.ToMySqlSafeValue(true)}");
                    }
                    else if (settings.NotNull)
                    {
                        // Given default value doesn't exist, and null values aren't allowed, so use first value from list.
                        queryBuilder.Append($" DEFAULT {settings.EnumValues.First().ToMySqlSafeValue(true)}");
                    }
                }
                else
                {
                    queryBuilder.Append($" DEFAULT {settings.DefaultValue.ToMySqlSafeValue(true)}");
                }
            }

            if (settings.UpdateTimeStampOnChange)
            {
                queryBuilder.Append(" ON UPDATE CURRENT_TIMESTAMP");
            }

            if (settings.AutoIncrement)
            {
                queryBuilder.Append(" AUTO_INCREMENT");
            }

            if (!String.IsNullOrWhiteSpace(settings.Comment))
            {
                databaseConnection.AddParameter($"comment{parameterSuffix}", settings.Comment);
                queryBuilder.Append($" COMMENT ?comment{parameterSuffix}");
            }

            if (!String.IsNullOrWhiteSpace(settings.AddAfterColumnName))
            {
                databaseConnection.AddParameter($"addAfterColumnName{parameterSuffix}", settings.AddAfterColumnName);
                queryBuilder.Append($" AFTER ?addAfterColumnName{parameterSuffix}");
            }

            return queryBuilder;
        }

        private static (string DatabaseValue, bool unsigned) MySqlDbTypeToDatabaseValue(MySqlDbType type)
        {
            var unsigned = false;
            string mysqlType;
            switch (type)
            {
                case MySqlDbType.Byte:
                    mysqlType = "BIT";
                    break;
                case MySqlDbType.Int16:
                    mysqlType = "TINYINT";
                    break;
                case MySqlDbType.Int24:
                    mysqlType = "MEDIUMINT";
                    break;
                case MySqlDbType.Int32:
                    mysqlType = "INT";
                    break;
                case MySqlDbType.Int64:
                    mysqlType = "BIGINT";
                    break;
                case MySqlDbType.VarChar:
                case MySqlDbType.String:
                case MySqlDbType.VarString:
                    mysqlType = "varchar";
                    break;
                case MySqlDbType.UByte:
                    mysqlType = "BIT";
                    unsigned = true;
                    break;
                case MySqlDbType.UInt16:
                    mysqlType = "TINYINT";
                    unsigned = true;
                    break;
                case MySqlDbType.UInt24:
                    mysqlType = "MEDIUMINT";
                    break;
                case MySqlDbType.UInt32:
                    mysqlType = "INT";
                    unsigned = true;
                    break;
                case MySqlDbType.UInt64:
                    mysqlType = "BIGINT";
                    unsigned = true;
                    break;
                default:
                    mysqlType = type.ToString().ToUpperInvariant();
                    break;
            }

            return (mysqlType, unsigned);
        }
    }
}
