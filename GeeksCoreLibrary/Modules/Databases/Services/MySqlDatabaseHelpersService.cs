using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Databases.Exceptions;
using GeeksCoreLibrary.Modules.Databases.Extensions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using MySql.Data.MySqlClient;

namespace GeeksCoreLibrary.Modules.Databases.Services
{
    /// <inheritdoc cref="IDatabaseHelpersService" />.
    public class MySqlDatabaseHelpersService : IDatabaseHelpersService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="MySqlDatabaseHelpersService"/>.
        /// </summary>
        public MySqlDatabaseHelpersService(IDatabaseConnection databaseConnection)
        {
            this.databaseConnection = databaseConnection;
        }

        /// <inheritdoc />
        public async Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("tableName", tableName);
            databaseConnection.AddParameter("columnName", columnName);
            
            var dataTable = await databaseConnection.GetAsync("SHOW COLUMNS FROM ?tableName LIKE ?columnName");
            return dataTable.Rows.Count > 0;
        }
        
        /// <inheritdoc />
        public async Task<List<string>> GetColumnNamesAsync(string tableName)
        {
            databaseConnection.AddParameter("tableName", tableName);
            var dataTable = await databaseConnection.GetAsync("SHOW COLUMNS FROM ?tableName");
            return dataTable.Rows.Cast<DataRow>().Select(dataRow => dataRow.Field<string>("Field")).ToList();
        }

        /// <inheritdoc />
        public async Task AddColumnToTableAsync(string tableName, ColumnSettingsModel settings, bool throwExceptionIfColumnAlreadyExists = true)
        {
            if (String.IsNullOrWhiteSpace(settings?.Name))
            {
                throw new ArgumentException("No column name given.");
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("tableName", tableName);
            databaseConnection.AddParameter("columnName", settings.Name);
            databaseConnection.AddParameter("defaultValue", settings.DefaultValue);
            
            if (!await ColumnExistsAsync(tableName, settings.Name))
            {
                if (throwExceptionIfColumnAlreadyExists)
                {
                    throw new DatabaseColumnExistsException(tableName, settings.Name);
                }

                return;
            }
            
            var queryBuilder = new StringBuilder("ALTER TABLE ?tableName ADD COLUMN ");
            queryBuilder.Append(GenerateColumnQueryPart(tableName, settings));
            queryBuilder.Append("; ");

            if (settings.AddIndex)
            {
                var indexTypeName = settings.IndexType.ToMySqlString();

                queryBuilder.Append($"CREATE {indexTypeName} INDEX ?columnName ON ?tableName (?columnName);");
            }

            await databaseConnection.ExecuteAsync(queryBuilder.ToString());
        }

        /// <inheritdoc />
        public async Task DropColumnAsync(string tableName, string columnName)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("tableName", tableName);
            databaseConnection.AddParameter("columnName", columnName);
            await databaseConnection.ExecuteAsync("ALTER TABLE ?tableName DROP COLUMN ?columnName");
        }

        /// <inheritdoc />
        public async Task CreateTableAsync(string tableName, IList<ColumnSettingsModel> primaryKeys, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci")
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("tableName", tableName);

            var queryBuilder = new StringBuilder("CREATE TABLE IS NOT EXISTS ?tableName");
            if (primaryKeys != null && primaryKeys.Any())
            {
                queryBuilder.AppendLine(" (");
                foreach (var primaryKey in primaryKeys)
                {
                    queryBuilder.AppendLine($"{GenerateColumnQueryPart(tableName, primaryKey)},");
                }

                queryBuilder.AppendLine($"PRIMARY KEY (`{String.Join("`,`", primaryKeys.Select(p => p.Name.ToMySqlSafeValue()))}`)");
                queryBuilder.AppendLine(")");
            }

            queryBuilder.AppendLine("ENGINE = AUTO");
            if (!String.IsNullOrWhiteSpace(characterSet))
            {
                databaseConnection.AddParameter("characterSet", characterSet);
                queryBuilder.Append("DEFAULT CHARACTER SET = ?characterSet");

                if (!String.IsNullOrWhiteSpace(collation))
                {
                    databaseConnection.AddParameter("collation", collation);
                    queryBuilder.Append(" COLLATE = ?collation");
                }

                queryBuilder.AppendLine();
            }

            await databaseConnection.ExecuteAsync(queryBuilder.ToString());
        }
        
        /// <inheritdoc />
        public async Task CreateOrUpdateTableAsync(string tableName, IList<ColumnSettingsModel> columns, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci")
        {
            var primaryKeys = columns.Where(c => c.IsPrimaryKey).ToList();
            var isNewTable = false;
            List<ColumnSettingsModel> columnsToAdd;

            if (!await TableExistsAsync(tableName))
            {
                // Create the table if it doesn't exist yet.
                await CreateTableAsync(tableName, primaryKeys, characterSet, collation);
                isNewTable = true;
                columnsToAdd = columns.Where(c => !c.IsPrimaryKey).ToList();
            }
            else
            {
                var existingColumnNames = await GetColumnNamesAsync(tableName);
                columnsToAdd = columns.Where(c => !existingColumnNames.Any(x => x.Equals(c.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            // Add missing columns.
            foreach (var column in columnsToAdd)
            {
                await AddColumnToTableAsync(tableName, column, false);
            }

            // Update primary key if needed.
            if (isNewTable)
            {
                // If it's a new table, we already added the primary keys.
                return;
            }

            databaseConnection.AddParameter("tableName", tableName);
            var dataTable = await databaseConnection.GetAsync("SHOW KEYS FROM ?tableName WHERE KEY_NAME = 'PRIMARY'");
            var primaryKeyIsChanged = dataTable.Rows.Count != primaryKeys.Count || dataTable.Rows.Cast<DataRow>().Any(d => !primaryKeys.Any(p => p.Name.Equals(d.Field<string>("column_name"))));
            if (!primaryKeyIsChanged)
            {
                return;
            }

            await databaseConnection.ExecuteAsync($"ALTER TABLE ?tableName DROP PRIMARY KEY, ADD PRIMARY KEY (`{String.Join("`,`", primaryKeys.Select(x => x.Name.ToMySqlSafeValue()))}`)");
        }

        /// <inheritdoc />
        public async Task<bool> TableExistsAsync(string tableName, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = databaseConnection.ConnectedDatabase;
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("databaseName", databaseName);
            databaseConnection.AddParameter("tableName", tableName);

            var dataTable = await databaseConnection.GetAsync("SELECT TABLE_NAME FROM information_schema.`TABLES` WHERE TABLE_SCHEMA = ?databaseName AND TABLE_NAME = ?tableName");
            return dataTable.Rows.Count > 0;
        }
        
        /// <inheritdoc />
        public async Task<bool> DatabaseExistsAsync(string databaseName)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("databaseName", databaseName);

            var dataTable = await databaseConnection.GetAsync("SELECT CATALOG_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE CATALOG_NAME = ?databaseName");
            return dataTable.Rows.Count > 0;
        }
        
        /// <inheritdoc />
        public async Task DropTableAsync(string tableName, bool isTemporaryTable = false)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("tableName", tableName);

            await databaseConnection.ExecuteAsync($"DROP {(isTemporaryTable ? "TEMPORARY" : "")} TABLE IF EXISTS ?tableName");
        }

        /// <inheritdoc />
        public async Task DuplicateTableAsync(string tableToDuplicate, string newTableName, bool includeData = true)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("tableToDuplicate", tableToDuplicate);
            databaseConnection.AddParameter("newTableName", newTableName);
            await databaseConnection.ExecuteAsync("CREATE TABLE ?newTableName LIKE ?tableToDuplicate");
            if (!includeData)
            {
                return;
            }

            await databaseConnection.ExecuteAsync("INSERT INTO ?newTableName (SELECT * FROM ?tableToDuplicate)");
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateIndexesAsync(List<IndexSettingsModel> indexes)
        {
            if (indexes == null || !indexes.Any())
            {
                throw new ArgumentNullException(nameof(indexes));
            }

            databaseConnection.ClearParameters();
            foreach (var index in indexes.Where(index => !String.IsNullOrWhiteSpace(index.Name) && index.Fields != null && index.Fields.Any()))
            {
                var createIndexQuery = $"ALTER TABLE ?tableName ADD {index.Type.ToMySqlString()} INDEX ?indexName (`{String.Join("`,`", index.Fields.Select(f => f.ToMySqlSafeValue()))}`)";

                databaseConnection.AddParameter("tableName", index.TableName);
                databaseConnection.AddParameter("indexName", index.Name);
                var dataTable = await databaseConnection.GetAsync("SHOW INDEX FROM ?tableName WHERE key_name = ?indexName");
                var recreateIndex = index.Fields.Count != dataTable.Rows.Count || dataTable.Rows.Cast<DataRow>().Any(dataRow => !index.Fields.Any(f => String.Equals(f, dataRow.Field<string>("column_name"), StringComparison.OrdinalIgnoreCase)));
                if (!recreateIndex)
                {
                    // Index has not been changed, so do nothing.
                    continue;
                }

                if (dataTable.Rows.Count > 0)
                {
                    // If an index with this name already exists, but the new one if different, drop the old index first.
                    await databaseConnection.ExecuteAsync("ALTER TABLE ?tableName DROP INDEX ?indexName");
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

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("databaseName", databaseName);
            databaseConnection.AddParameter("characterSet", characterSet);
            databaseConnection.AddParameter("collation", collation);
            await databaseConnection.ExecuteAsync("CREATE DATABASE IF NOT EXISTS ?databaseName DEFAULT CHARACTER SET = ?characterSet DEFAULT COLLATE = ?collation");
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
            databaseConnection.AddParameter($"columnName{parameterSuffix}", settings.CharacterSet);
            var (databaseType, unsigned) = MySqlDbTypeToDatabaseValue(settings.Type);
            var queryBuilder = new StringBuilder();
            switch (settings.Type)
            {
                case MySqlDbType.Double:
                case MySqlDbType.Decimal:
                case MySqlDbType.Float:
                    var columnLength = settings.Length == 0 ? "" : $"({settings.Length}, {settings.Decimals})";
                    queryBuilder.Append($"?columnName{parameterSuffix} {databaseType}{columnLength}");
                    break;
                case MySqlDbType.Time:
                case MySqlDbType.Timestamp:
                case MySqlDbType.DateTime:
                case MySqlDbType.Year:
                case MySqlDbType.TinyBlob:
                case MySqlDbType.Blob:
                case MySqlDbType.MediumBlob:
                    queryBuilder.Append($"?columnName{parameterSuffix} {databaseType}");
                    break;
                case MySqlDbType.Enum:
                    if (settings.EnumValues == null || !settings.EnumValues.Any())
                    {
                        throw new DatabaseColumnMissingEnumValuesException(tableName, settings.Name);
                    }

                    queryBuilder.Append($"?columnName{parameterSuffix} {databaseType}({String.Join(",", settings.EnumValues.Select(v => v.ToMySqlSafeValue(true)))})");

                    break;
                default:
                    queryBuilder.Append($"?columnName{parameterSuffix} {databaseType}({settings.Length})");
                    break;
            }

            if (settings.Type.InList(MySqlDbType.TinyText, MySqlDbType.Text, MySqlDbType.MediumText, MySqlDbType.LongText, MySqlDbType.VarChar, MySqlDbType.String, MySqlDbType.VarString))
            {
                if (!String.IsNullOrWhiteSpace(settings.CharacterSet))
                {
                    databaseConnection.AddParameter($"characterSet{parameterSuffix}", settings.CharacterSet);
                    queryBuilder.Append($" DEFAULT CHARACTER SET = ?characterSet{parameterSuffix}");

                    if (!String.IsNullOrWhiteSpace(settings.Collation))
                    {
                        databaseConnection.AddParameter($"collation{parameterSuffix}", settings.Collation);
                        queryBuilder.Append($" COLLATE = ?collation{parameterSuffix}");
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
                        queryBuilder.Append(" DEFAULT ?defaultValue");
                    }
                    else if (settings.NotNull)
                    {
                        // Given default value doesn't exist, and null values aren't allowed, so use first value from list.
                        queryBuilder.Append($" DEFAULT {settings.EnumValues.First().ToMySqlSafeValue(true)}");
                    }
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
