using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Exceptions;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.Databases.Services
{
    public class MySqlDatabaseConnection : IDatabaseConnection, IScopedService
    {
        public static readonly List<int> MySqlErrorCodesToRetry = new()
        {
            (int) MySqlErrorCode.LockDeadlock,
            (int) MySqlErrorCode.LockWaitTimeout,
            (int) MySqlErrorCode.UnableToConnectToHost,
            (int) MySqlErrorCode.TooManyUserConnections,
            (int) MySqlErrorCode.ConnectionCountError,
            (int) MySqlErrorCode.TableDefinitionChanged
        };

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ILogger<MySqlDatabaseConnection> logger;
        private readonly IBranchesService branchesService;
        private MySqlConnectionStringBuilder connectionStringForReading;
        private MySqlConnectionStringBuilder connectionStringForWriting;

        private MySqlConnection ConnectionForReading { get; set; }
        private MySqlConnection ConnectionForWriting { get; set; }
        private MySqlCommand CommandForReading { get; set; }
        private MySqlCommand CommandForWriting { get; set; }

        private readonly GclSettings gclSettings;

        private MySqlDataReader dataReader;

        private MySqlTransaction transaction;
        private readonly Guid instanceId;
        private int readConnectionLogId;
        private int writeConnectionLogId;
        private bool? logTableExists;

        private readonly ConcurrentDictionary<string, object> parameters = new();

        /// <summary>
        /// Creates a new instance of <see cref="MySqlDatabaseConnection"/>.
        /// </summary>
        public MySqlDatabaseConnection(IOptions<GclSettings> gclSettings,
            ILogger<MySqlDatabaseConnection> logger,
            IBranchesService branchesService,
            IHttpContextAccessor httpContextAccessor = null,
            IWebHostEnvironment webHostEnvironment = null)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.webHostEnvironment = webHostEnvironment;
            this.logger = logger;
            this.branchesService = branchesService;
            this.gclSettings = gclSettings.Value;

            instanceId = Guid.NewGuid();
            connectionStringForReading = String.IsNullOrWhiteSpace(this.gclSettings.ConnectionString) ? null : new MySqlConnectionStringBuilder { ConnectionString = this.gclSettings.ConnectionString };
            connectionStringForWriting = String.IsNullOrWhiteSpace(this.gclSettings.ConnectionStringForWriting) ? null : new MySqlConnectionStringBuilder { ConnectionString = this.gclSettings.ConnectionStringForWriting };

            if (connectionStringForReading != null)
            {
                connectionStringForReading.Database = branchesService.GetDatabaseNameFromCookie() ?? connectionStringForReading.Database;
            }
            if (connectionStringForWriting != null)
            {
                connectionStringForWriting.Database = branchesService.GetDatabaseNameFromCookie() ?? connectionStringForWriting.Database;
            }

            logger.LogTrace($"Created new instance of MySqlDatabaseConnection with ID '{instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext)}");
        }

        /// <inheritdoc />
        public string ConnectedDatabase { get; protected set; }

        /// <inheritdoc />
        public string ConnectedDatabaseForWriting { get; protected set; }

        /// <inheritdoc />
        public async Task<DbDataReader> GetReaderAsync(string query)
        {
            logger.LogTrace($"Called GetReaderAsync of MySqlDatabaseConnection with ID '{instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext)}");
            await EnsureOpenConnectionForReadingAsync();
            CommandForReading.CommandText = query;

            dataReader = await CommandForReading.ExecuteReaderAsync();

            return dataReader;
        }

        /// <inheritdoc />
        public Task<DataTable> GetAsync(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
        {
            return GetAsync(query, 0, cleanUp, useWritingConnectionIfAvailable);
        }

        private async Task<DataTable> GetAsync(string query, int retryCount, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
        {
            try
            {
                MySqlCommand commandToUse;
                if (useWritingConnectionIfAvailable && !String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString))
                {
                    await EnsureOpenConnectionForWritingAsync();
                    commandToUse = CommandForWriting;
                }
                else
                {
                    await EnsureOpenConnectionForReadingAsync();
                    commandToUse = CommandForReading;
                }

                var result = new DataTable();
                commandToUse.CommandText = query;
                using var dataAdapter = new MySqlDataAdapter(commandToUse);
                dataAdapter.Fill(result);

                logger.LogDebug("Query: {query}", query);

                return result;
            }
            catch (MySqlException mySqlException)
            {
                // Never retry single queries if we're in a transaction, because transactions will get rolled back when a deadlock occurs,
                // so retrying a single query in a transaction is not very useful on most/all cases.
                // Also, if we've reached the maximum number of retries, don't retry anymore.
                if (HasActiveTransaction() || retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                    throw new GclQueryException("Error trying to run query", query, mySqlException);
                }

                // If we're not in a transaction, retry the query if it's a deadlock.
                if (MySqlErrorCodesToRetry.Contains(mySqlException.Number))
                {
                    Thread.Sleep(gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
                    return await GetAsync(query, retryCount + 1, cleanUp, useWritingConnectionIfAvailable);
                }

                // For any other errors, just throw the exception.
                logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, mySqlException);
            }
            finally
            {
                // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets committed or roll backed.
                if (!HasActiveTransaction() && cleanUp)
                {
                    await CleanUpAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task<string> GetAsJsonAsync(string query, bool formatResult = false, bool skipCache = false)
        {
            return JsonConvert.SerializeObject(await GetAsync(query), formatResult ? Formatting.Indented : Formatting.None);
        }

        /// <inheritdoc />
        public Task<int> ExecuteAsync(string query, bool useWritingConnectionIfAvailable = true, bool cleanUp = true)
        {
            return ExecuteAsync(query, 0, useWritingConnectionIfAvailable, cleanUp);
        }

        /// <summary>
        /// Executes a query and returns the amount of rows affected.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="retryCount">How many times the query has been attempted.</param>
        /// <param name="useWritingConnectionIfAvailable"></param>
        /// <param name="cleanUp">Clean up after the query has been completed.</param>
        /// <returns></returns>
        private async Task<int> ExecuteAsync(string query, int retryCount, bool useWritingConnectionIfAvailable = true, bool cleanUp = true)
        {
            try
            {
                MySqlCommand commandToUse;
                if (useWritingConnectionIfAvailable && !String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString))
                {
                    await EnsureOpenConnectionForWritingAsync();
                    commandToUse = CommandForWriting;
                }
                else
                {
                    await EnsureOpenConnectionForReadingAsync();
                    commandToUse = CommandForReading;
                }

                commandToUse.CommandText = query;
                logger.LogDebug("Query: {query}", query);
                return await commandToUse.ExecuteNonQueryAsync();
            }
            catch (MySqlException mySqlException)
            {
                // Never retry single queries if we're in a transaction, because transactions will get rolled back when a deadlock occurs,
                // so retrying a single query in a transaction is not very useful on most/all cases.
                // Also, if we've reached the maximum number of retries, don't retry anymore.
                if (HasActiveTransaction() || retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                    throw new GclQueryException("Error trying to run query", query, mySqlException);
                }

                // If we're not in a transaction, retry the query if it's a deadlock.
                if (MySqlErrorCodesToRetry.Contains(mySqlException.Number))
                {
                    Thread.Sleep(gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
                    return await ExecuteAsync(query, retryCount + 1, useWritingConnectionIfAvailable, cleanUp);
                }

                // For any other errors, just throw the exception.
                logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, mySqlException);
            }
            finally
            {
                // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets comitted or rollbacked.
                if (transaction == null && cleanUp)
                {
                    await CleanUpAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task<T> InsertOrUpdateRecordBasedOnParametersAsync<T>(string tableName, T id = default, string idColumnName = "id", bool ignoreErrors = false, bool useWritingConnectionIfAvailable = true)
        {
            if (parameters.Count == 0)
            {
                return id;
            }

            AddParameter("InsertOrUpdateRecord_Id", id);
            var query = new StringBuilder();
            var idIsDefaultValue = id.Equals(default(T));
            if (idIsDefaultValue)
            {
                query.Append($"INSERT {(ignoreErrors ? "IGNORE" : "")} INTO `{tableName}`");
            }
            else
            {
                query.Append($"UPDATE {(ignoreErrors ? "IGNORE" : "")} `{tableName}` SET ");
            }

            if (idIsDefaultValue)
            {
                query.Append($"({String.Join(",", parameters.Select(p => $"`{(p.Key == "InsertOrUpdateRecord_Id" ? idColumnName : p.Key)}`"))}) VALUES ({String.Join(",", parameters.Select(p => $"?{p.Key}"))})");
            }
            else
            {
                query.Append($"{String.Join(",", parameters.Where(p => p.Key != "InsertOrUpdateRecord_Id").Select(p => $"`{p.Key}` = ?{p.Key}"))} WHERE `{idColumnName}` = ?InsertOrUpdateRecord_Id");
            }

            await ExecuteAsync(query.ToString(), useWritingConnectionIfAvailable, false);

            if (!idIsDefaultValue)
            {
                return id;
            }

            var result = await GetAsync("SELECT LAST_INSERT_ID()", useWritingConnectionIfAvailable: useWritingConnectionIfAvailable);
            return (T)Convert.ChangeType(result.Rows[0][0], typeof(T));
        }

        /// <inheritdoc />
        public Task<long> InsertRecordAsync(string query, bool useWritingConnectionIfAvailable = true)
        {
            return InsertRecordAsync(query, 0, useWritingConnectionIfAvailable);
        }

        private async Task<long> InsertRecordAsync(string query, int retryCount, bool useWritingConnectionIfAvailable = true)
        {
            if (String.IsNullOrWhiteSpace(query))
            {
                return 0L;
            }

            try
            {
                MySqlCommand commandToUse;
                if (useWritingConnectionIfAvailable && !String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString))
                {
                    await EnsureOpenConnectionForWritingAsync();
                    commandToUse = CommandForWriting;
                }
                else
                {
                    await EnsureOpenConnectionForReadingAsync();
                    commandToUse = CommandForReading;
                }

                var finalQuery = new StringBuilder(query.TrimEnd());
                if (finalQuery[^1] != ';')
                {
                    finalQuery.Append(';');
                }

                // Add the query to retrieve the last inserted ID to the query that was passed to the function.
                finalQuery.Append("SELECT LAST_INSERT_ID();");

                commandToUse.CommandText = finalQuery.ToString();

                await using var reader = await commandToUse.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return 0L;
                }

                return Int64.TryParse(Convert.ToString(reader.GetValue(0)), out var tempId) ? tempId : 0L;
            }
            catch (MySqlException mySqlException)
            {
                // Never retry single queries if we're in a transaction, because transactions will get rolled back when a deadlock occurs,
                // so retrying a single query in a transaction is not very useful on most/all cases.
                // Also, if we've reached the maximum number of retries, don't retry anymore.
                if (HasActiveTransaction() || retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                    throw new GclQueryException("Error trying to run query", query, mySqlException);
                }

                // If we're not in a transaction, retry the query if it's a deadlock.
                if (MySqlErrorCodesToRetry.Contains(mySqlException.Number))
                {
                    Thread.Sleep(gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
                    return await InsertRecordAsync(query, retryCount + 1, useWritingConnectionIfAvailable);
                }

                // For any other errors, just throw the exception.
                logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, mySqlException);
            }
            finally
            {
                // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets comitted or rollbacked.
                if (transaction == null)
                {
                    await CleanUpAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task<IDbTransaction> BeginTransactionAsync(bool forceNewTransaction = false)
        {
            if (!forceNewTransaction && transaction != null)
            {
                throw new InvalidOperationException("Called BeginTransaction, but there already is an active transaction.");
            }

            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }

            // If we're using transactions, make sure to use it on the write connection, if we have one.
            MySqlConnection connectionToUse;
            if (!String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString))
            {
                await EnsureOpenConnectionForWritingAsync();
                connectionToUse = ConnectionForWriting;
            }
            else
            {
                await EnsureOpenConnectionForReadingAsync();
                connectionToUse = ConnectionForReading;
            }

            transaction = await connectionToUse.BeginTransactionAsync();

            // MySqlConnector wants us to set the transaction on the command, so that it knows which transaction to use.
            if (CommandForReading != null)
            {
                CommandForReading.Transaction = transaction;
            }
            if (CommandForWriting != null)
            {
                CommandForWriting.Transaction = transaction;
            }

            return transaction;
        }

        /// <inheritdoc />
        public async Task CommitTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            if (transaction == null)
            {
                if (throwErrorIfNoActiveTransaction)
                {
                    throw new InvalidOperationException("Called CommitTransactionAsync, but there is no active transaction.");
                }

                return;
            }

            await transaction.CommitAsync();

            // Dispose and set to null, so that we know there is no more active transaction.
            await transaction.DisposeAsync();
            transaction = null;

            // Also reset the Transaction property on the commands, so that they don't use the transaction anymore.
            if (CommandForReading != null)
            {
                CommandForReading.Transaction = null;
            }
            if (CommandForWriting != null)
            {
                CommandForWriting.Transaction = null;
            }

            await CleanUpAsync();
        }

        /// <inheritdoc />
        public async Task RollbackTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            if (transaction == null)
            {
                if (throwErrorIfNoActiveTransaction)
                {
                    throw new InvalidOperationException("Called RollbackTransactionAsync, but there is no active transaction.");
                }

                return;
            }

            await transaction.RollbackAsync();

            // Dispose and set to null, so that we know there is no more active transaction.
            await transaction.DisposeAsync();
            transaction = null;

            // Also reset the Transaction property on the commands, so that they don't use the transaction anymore.
            if (CommandForReading != null)
            {
                CommandForReading.Transaction = null;
            }
            if (CommandForWriting != null)
            {
                CommandForWriting.Transaction = null;
            }

            await CleanUpAsync();
        }

        /// <inheritdoc />
        public void ClearParameters()
        {
            parameters.Clear();
        }

        /// <inheritdoc />
        public string GetDatabaseNameForCaching(bool writeDatabase = false)
        {
            var connectionStringBuilder = writeDatabase && !String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString) ? connectionStringForWriting : connectionStringForReading;
            return $"{connectionStringBuilder["server"]}_{connectionStringBuilder["database"]}";
        }

        /// <inheritdoc />
        public void AddParameter(string key, object value)
        {
            if (parameters.ContainsKey(key))
            {
                parameters.TryRemove(key, out _);
            }

            parameters.TryAdd(key, value);
        }

        private async Task CleanUpAsync()
        {
            if (dataReader != null) await dataReader.DisposeAsync();
            if (CommandForReading != null) await CommandForReading.DisposeAsync();
            if (CommandForWriting != null) await CommandForWriting.DisposeAsync();
            CommandForReading = null;
            CommandForWriting = null;
            dataReader = null;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            logger.LogTrace($"Disposing instance of MySqlDatabaseConnection with ID '{instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext)}");
            if (dataReader != null)
            {
                await dataReader.DisposeAsync();
            }

            if (ConnectionForReading != null)
            {
                await AddConnectionCloseLogAsync(false, true);
            }

            if (ConnectionForWriting != null)
            {
                await AddConnectionCloseLogAsync(true, true);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            logger.LogTrace($"Disposing instance of MySqlDatabaseConnection with ID '{instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext)}");
            dataReader?.Dispose();
            AddConnectionCloseLogAsync(false, true);
            AddConnectionCloseLogAsync(true, true);
        }

        /// <summary>
        /// If the connection is not open yet, open it.
        /// </summary>
        public async Task EnsureOpenConnectionForReadingAsync()
        {
            var createdNewConnection = false;
            if (ConnectionForReading == null)
            {
                ConnectionForReading = new MySqlConnection { ConnectionString = connectionStringForReading.ConnectionString };
                CommandForReading = ConnectionForReading.CreateCommand();
                createdNewConnection = true;
            }

            CommandForReading ??= ConnectionForReading.CreateCommand();

            // Remember the database name that was connected to.
            ConnectedDatabase = ConnectionForReading.Database;

            // Copy parameters.
            foreach (var parameter in parameters)
            {
                if (CommandForReading.Parameters.Contains(parameter.Key))
                {
                    CommandForReading.Parameters.RemoveAt(parameter.Key);
                }

                CommandForReading.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }

            if (ConnectionForReading.State != ConnectionState.Closed)
            {
                return;
            }

            await ConnectionForReading.OpenAsync();

            await SetTimezone(CommandForReading);
            await SetCharacterSetAndCollationAsync(CommandForReading);

            if (createdNewConnection)
            {
                // Log the opening of the connection.
                await AddConnectionOpenLogAsync(false);
            }
        }

        /// <summary>
        /// If the connection is not open yet, open it.
        /// </summary>
        /// <returns></returns>
        public async Task EnsureOpenConnectionForWritingAsync()
        {
            if (String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString))
            {
                ConnectedDatabaseForWriting = null;
                return;
            }

            var createdNewConnection = false;
            if (ConnectionForWriting == null)
            {
                ConnectionForWriting = new MySqlConnection { ConnectionString = connectionStringForWriting.ConnectionString };
                CommandForWriting = ConnectionForWriting.CreateCommand();
                createdNewConnection = true;
            }

            CommandForWriting ??= ConnectionForWriting.CreateCommand();

            // Remember the database name that was connected to.
            ConnectedDatabaseForWriting = ConnectionForWriting.Database;

            // Copy parameters.
            foreach (var parameter in parameters)
            {
                if (CommandForWriting.Parameters.Contains(parameter.Key))
                {
                    CommandForWriting.Parameters.RemoveAt(parameter.Key);
                }

                CommandForWriting.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }

            if (ConnectionForWriting.State != ConnectionState.Closed)
            {
                return;
            }

            await ConnectionForWriting.OpenAsync();

            await SetTimezone(CommandForWriting);
            await SetCharacterSetAndCollationAsync(CommandForWriting);

            if (createdNewConnection)
            {
                // Log the opening of the connection.
                await AddConnectionOpenLogAsync(true);
            }
        }

        /// <inheritdoc />
        public async Task ChangeConnectionStringsAsync(string newConnectionStringForReading, string newConnectionStringForWriting = null)
        {
            connectionStringForReading ??= new MySqlConnectionStringBuilder();
            connectionStringForWriting ??= new MySqlConnectionStringBuilder();

            connectionStringForReading.ConnectionString = newConnectionStringForReading;
            connectionStringForWriting.ConnectionString = String.IsNullOrWhiteSpace(newConnectionStringForWriting) ? newConnectionStringForReading : newConnectionStringForWriting;
            await CleanUpAsync();
            if (ConnectionForReading != null)
            {
                await AddConnectionCloseLogAsync(false);
                await ConnectionForReading.CloseAsync();
            }

            if (ConnectionForWriting != null)
            {
                await AddConnectionCloseLogAsync(true);
                await ConnectionForWriting.CloseAsync();
            }

            ConnectionForReading = null;
            ConnectionForWriting = null;
        }

        /// <inheritdoc />
        public void SetCommandTimeout(int value)
        {
            if (CommandForReading != null)
            {
                CommandForReading.CommandTimeout = value;
            }

            if (CommandForWriting != null)
            {
                CommandForWriting.CommandTimeout = value;
            }
        }

        /// <inheritdoc />
        public bool HasActiveTransaction()
        {
            return transaction != null;
        }

        /// <summary>
        /// Checks whether or not the log table (for logging the opening and closing of database connections) exists.
        /// </summary>
        /// <param name="command">The MySqlCommand to execute the query on to check if the table exists.</param>
        /// <returns>A boolean indicating whether the log table exists or not.</returns>
        private async Task<bool> LogTableExistsAsync(MySqlCommand command)
        {
            // Simple text file that indicates whether or not the log table exists, so that we don't have to execute an extra query every time.
            var cacheDirectory = webHostEnvironment == null ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data") : FileSystemHelpers.GetContentCacheFolderPath(webHostEnvironment);
            var filePath = cacheDirectory == null ? null : Path.Combine(cacheDirectory, String.Format(Constants.LogTableExistsCacheFileName, (ConnectionForWriting ?? ConnectionForReading).Database));
            if (filePath != null && File.Exists(filePath))
            {
                return true;
            }

            if (webHostEnvironment == null && cacheDirectory != null && !Directory.Exists(cacheDirectory))
            {
                try
                {
                    Directory.CreateDirectory(cacheDirectory);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, $"An error occurred while trying to create the directory '{cacheDirectory}'.");
                    filePath = null;
                }
            }

            var dataTable = new DataTable();
            command.CommandText = $"SELECT TABLE_NAME FROM information_schema.`TABLES` WHERE TABLE_NAME = '{Constants.DatabaseConnectionLogTableName}' AND TABLE_SCHEMA = '{(ConnectionForWriting ?? ConnectionForReading).Database.ToMySqlSafeValue(false)}'";
            using var dataAdapter = new MySqlDataAdapter(command);
            dataAdapter.Fill(dataTable);

            if (dataTable.Rows.Count == 0)
            {
                return false;
            }

            if (filePath == null)
            {
                return true;
            }

            try
            {
                // Create the file to indicate that the table exists.
                await File.WriteAllTextAsync(filePath, "");
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, $"An error occurred while trying to create the file '{filePath}'.");
            }

            return true;
        }

        private async Task SetTimezone(MySqlCommand command)
        {
            try
            {
                // Make sure we always use the correct timezone.
                if (!String.IsNullOrWhiteSpace(gclSettings.DatabaseTimeZone))
                {
                    command.CommandText = $"SET @@time_zone = {gclSettings.DatabaseTimeZone.ToMySqlSafeValue(true)};";
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (MySqlException mySqlException)
            {
                // Checks if the exception is about the timezone or something else related to MySQL.
                // Not setting timezones when they are not available should not be logged as en error.
                if (mySqlException.Number == 1298)
                {
                    logger.LogInformation($"The time zone is not set to '{gclSettings.DatabaseTimeZone}', because that timezone is not available in the database.");
                }
                else
                {
                    logger.LogWarning(mySqlException, $"An error occurred while trying to set the time zone to '{gclSettings.DatabaseTimeZone}'");
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, $"An error occurred while trying to set the time zone to '{gclSettings.DatabaseTimeZone}'");
            }
        }

        /// <summary>
        /// Sets the correct character set and collation for the database connection.
        /// </summary>
        /// <param name="command">The <see cref="MySqlCommand"/> object that will execute the query.</param>
        private async Task SetCharacterSetAndCollationAsync(MySqlCommand command)
        {
            try
            {
                var characterSet = !String.IsNullOrWhiteSpace(gclSettings.DatabaseCharacterSet) ? gclSettings.DatabaseCharacterSet : "utf8mb4";
                var collation = !String.IsNullOrWhiteSpace(gclSettings.DatabaseCollation) ? gclSettings.DatabaseCollation : "utf8mb4_general_ci";

                // Make sure we always use the correct timezone.
                if (!String.IsNullOrWhiteSpace(gclSettings.DatabaseTimeZone))
                {
                    command.CommandText = $"SET NAMES {characterSet} COLLATE {collation};";
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (MySqlException mySqlException)
            {
                logger.LogWarning(mySqlException, $"An error occurred while trying to set the character set to '{gclSettings.DatabaseCharacterSet}' and the collation to '{gclSettings.DatabaseCollation}'");
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, $"An error occurred while trying to set the character set to '{gclSettings.DatabaseCharacterSet}' and the collation to '{gclSettings.DatabaseCollation}'");
            }
        }

        /// <summary>
        /// Add a mention to the log table that a connection to the database has been opened.
        /// </summary>
        /// <param name="isWriteConnection">Is this a write connection (true) or a read connection (false)?</param>
        private async Task AddConnectionOpenLogAsync(bool isWriteConnection)
        {
            try
            {
                if (!gclSettings.LogOpeningAndClosingOfConnections)
                {
                    return;
                }

                var commandToUse = isWriteConnection && !String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString) ? CommandForWriting : CommandForReading;

                logTableExists ??= await LogTableExistsAsync(commandToUse);

                if (!logTableExists.Value)
                {
                    // Table for logging doesn't exist yet, don't do anything. The table gets created during startup, but that also uses this service for doing that.
                    // So the table obviously won't exist yet during startup and we don't want an error from that.
                    return;
                }

                var url = "";
                var httpMethod = "";
                if (httpContextAccessor?.HttpContext != null)
                {
                    url = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).ToString();
                    httpMethod = httpContextAccessor.HttpContext.Request.Method;
                }

                if(commandToUse.Parameters.Contains("gclConnectionOpened")) commandToUse.Parameters.Remove("gclConnectionOpened");
                if(commandToUse.Parameters.Contains("gclConnectionUrl")) commandToUse.Parameters.Remove("gclConnectionUrl");
                if(commandToUse.Parameters.Contains("gclConnectionHttpMethod")) commandToUse.Parameters.Remove("gclConnectionHttpMethod");
                if(commandToUse.Parameters.Contains("gclConnectionInstanceId")) commandToUse.Parameters.Remove("gclConnectionInstanceId");
                if(commandToUse.Parameters.Contains("gclConnectionType")) commandToUse.Parameters.Remove("gclConnectionType");
                commandToUse.Parameters.AddWithValue("gclConnectionOpened", DateTime.Now);
                commandToUse.Parameters.AddWithValue("gclConnectionUrl", url);
                commandToUse.Parameters.AddWithValue("gclConnectionHttpMethod", httpMethod);
                commandToUse.Parameters.AddWithValue("gclConnectionInstanceId", instanceId);
                commandToUse.Parameters.AddWithValue("gclConnectionType", isWriteConnection ? "write" : "read");

                commandToUse.CommandText = $@"INSERT INTO {Constants.DatabaseConnectionLogTableName} (opened, url, http_method, database_service_instance_id, type)
VALUES (?gclConnectionOpened, ?gclConnectionUrl, ?gclConnectionHttpMethod, ?gclConnectionInstanceId, ?gclConnectionType);
SELECT LAST_INSERT_ID();";
                await using var reader = await commandToUse.ExecuteReaderAsync();
                var id = !await reader.ReadAsync() ? 0 : (Int32.TryParse(Convert.ToString(reader.GetValue(0)), out var tempId) ? tempId : 0);

                if (isWriteConnection)
                {
                    writeConnectionLogId = id;
                }
                else
                {
                    readConnectionLogId = id;
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error while trying to add connection open log.");
            }
        }

        /// <summary>
        /// Add a mention to the log table that a connection to the database has been closed.
        /// </summary>
        /// <param name="isWriteConnection">Is this a write connection (true) or a read connection (false)?</param>
        /// <param name="disposeConnection">Set to true to dispose the connection at the end.</param>
        private async Task AddConnectionCloseLogAsync(bool isWriteConnection, bool disposeConnection = false)
        {
            var commandToUse = isWriteConnection && !String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString) ? CommandForWriting : CommandForReading;

            try
            {
                if (!gclSettings.LogOpeningAndClosingOfConnections && ((isWriteConnection && writeConnectionLogId == 0) || (!isWriteConnection && readConnectionLogId == 0)))
                {
                    return;
                }

                if (!logTableExists.HasValue || !logTableExists.Value)
                {
                    // Table for logging doesn't exist yet, don't do anything. The table gets created during startup, but that also uses this service for doing that.
                    // So the table obviously won't exist yet during startup and we don't want an error from that.
                    return;
                }

                if (commandToUse == null)
                {
                    if (isWriteConnection && !String.IsNullOrWhiteSpace(connectionStringForWriting?.ConnectionString))
                    {
                        commandToUse = CommandForWriting ??= ConnectionForWriting.CreateCommand();
                    }
                    else
                    {
                        commandToUse = CommandForReading ??= ConnectionForReading.CreateCommand();
                    }
                }

                if (commandToUse.Connection is { State: ConnectionState.Closed })
                {
                    await commandToUse.Connection.OpenAsync();
                }

                commandToUse.Parameters.Clear();
                commandToUse.Parameters.AddWithValue("gclConnectionClosed", DateTime.Now);
                commandToUse.Parameters.AddWithValue("gclConnectionId", isWriteConnection ? writeConnectionLogId : readConnectionLogId);
                commandToUse.CommandText = $"UPDATE {Constants.DatabaseConnectionLogTableName} SET closed = ?gclConnectionClosed WHERE id = ?gclConnectionId";
                await commandToUse.ExecuteNonQueryAsync();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error while trying to add connection close log.");
            }
            finally
            {
                if (disposeConnection)
                {
                    if (commandToUse != null)
                    {
                        await commandToUse.DisposeAsync();
                    }

                    var connection = (isWriteConnection ? ConnectionForWriting : ConnectionForReading);
                    if (connection != null)
                    {
                        await connection.DisposeAsync();
                    }
                }
            }
        }
    }
}