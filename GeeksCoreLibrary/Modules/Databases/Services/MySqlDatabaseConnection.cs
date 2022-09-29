using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.Databases.Services
{
    public class MySqlDatabaseConnection : IDatabaseConnection, IScopedService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<MySqlDatabaseConnection> logger;
        private readonly IBranchesService branchesService;
        private MySqlConnectionStringBuilder connectionStringForReading;
        private MySqlConnectionStringBuilder connectionStringForWriting;

        private MySqlConnection ConnectionForReading { get; set; }
        private MySqlConnection ConnectionForWriting { get; set; }
        private MySqlCommand CommandForReading { get; set; }
        private MySqlCommand CommandForWriting { get; set; }

        private readonly GclSettings gclSettings;

        private DbDataReader dataReader;

        private IDbTransaction transaction;
        private readonly Guid instanceId;

        private readonly ConcurrentDictionary<string, object> parameters = new();

        /// <summary>
        /// Creates a new instance of <see cref="MySqlDatabaseConnection"/>.
        /// </summary>
        public MySqlDatabaseConnection(IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, ILogger<MySqlDatabaseConnection> logger, IBranchesService branchesService)
        {
            this.httpContextAccessor = httpContextAccessor;
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

            logger.LogTrace($"Created new instance of MySqlDatabaseConnection with ID '{instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext)}");
        }

        /// <inheritdoc />
        public string ConnectedDatabase { get; protected set; }

        /// <inheritdoc />
        public string ConnectedDatabaseForWriting { get; protected set; }

        /// <inheritdoc />
        public async Task<DbDataReader> GetReaderAsync(string query)
        {
            logger.LogTrace($"Called GetReaderAsync of MySqlDatabaseConnection with ID '{instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext)}");
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
                await dataAdapter.FillAsync(result);

                logger.LogDebug("Query: {query}", query);

                return result;
            }
            catch (MySqlException mySqlException)
            {
                if (retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                    throw;
                }

                switch (mySqlException.Number)
                {
                    case (int)MySqlErrorCode.LockDeadlock:
                    case (int)MySqlErrorCode.LockWaitTimeout:
                        return await GetAsync(query, retryCount + 1);
                    case (int)MySqlErrorCode.UnableToConnectToHost:
                    case (int)MySqlErrorCode.TooManyUserConnections:
                    case (int)MySqlErrorCode.ConnectionCountError:
                        Thread.Sleep(1000);
                        return await GetAsync(query, retryCount + 1);
                    default:
                        logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                        throw;
                }
            }
            finally
            {
                // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets committed or rollbacked.
                if (transaction == null && cleanUp)
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
                if (retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                    throw;
                }

                switch (mySqlException.Number)
                {
                    case (int)MySqlErrorCode.LockDeadlock:
                    case (int)MySqlErrorCode.LockWaitTimeout:
                        return await ExecuteAsync(query, retryCount + 1);
                    case (int)MySqlErrorCode.UnableToConnectToHost:
                    case (int)MySqlErrorCode.TooManyUserConnections:
                    case (int)MySqlErrorCode.ConnectionCountError:
                        Thread.Sleep(1000);
                        return await ExecuteAsync(query, retryCount + 1);
                    default:
                        logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                        throw;
                }
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
                if (retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    throw;
                }

                switch (mySqlException.Number)
                {
                    case (int)MySqlErrorCode.LockDeadlock:
                    case (int)MySqlErrorCode.LockWaitTimeout:
                        return await InsertRecordAsync(query, retryCount + 1);
                    case (int)MySqlErrorCode.UnableToConnectToHost:
                    case (int)MySqlErrorCode.TooManyUserConnections:
                    case (int)MySqlErrorCode.ConnectionCountError:
                        Thread.Sleep(1000);
                        return await InsertRecordAsync(query, retryCount + 1);
                    default:
                        throw;
                }
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

            transaction?.Rollback();

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

            transaction.Commit();

            // Dispose and set to null, so that we know there is no more active transaction.
            transaction.Dispose();
            transaction = null;
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

            transaction.Rollback();

            // Dispose and set to null, so that we know there is no more active transaction.
            transaction.Dispose();
            transaction = null;
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
        public void Dispose()
        {
            logger.LogTrace($"Disposing instance of MySqlDatabaseConnection with ID '{instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext)}");
            dataReader?.Dispose();
            ConnectionForReading?.Dispose();
            CommandForReading?.Dispose();
            ConnectionForWriting?.Dispose();
            CommandForWriting?.Dispose();
        }

        /// <summary>
        /// If the connection is not open yet, open it.
        /// </summary>
        public async Task EnsureOpenConnectionForReadingAsync()
        {
            if (ConnectionForReading == null)
            {
                ConnectionForReading = new MySqlConnection { ConnectionString = connectionStringForReading.ConnectionString };
                CommandForReading = ConnectionForReading.CreateCommand();
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

            if (ConnectionForReading.State == ConnectionState.Closed)
            {
                await ConnectionForReading.OpenAsync();

                try
                {
                    // Make sure we always use the correct timezone.
                    if (!String.IsNullOrWhiteSpace(gclSettings.DatabaseTimeZone))
                    {
                        CommandForReading.CommandText =
                            $"SET @@time_zone = {gclSettings.DatabaseTimeZone.ToMySqlSafeValue(true)};";
                        await CommandForReading.ExecuteNonQueryAsync();
                    }
                }
                catch (MySqlException mySqlException)
                {
                    //Checks if the exception is about the timezone or something else related to MySQL. Not setting timezones on databases based at TransIP is okay.
                    if (mySqlException.Number == 1298)
                    {
                        logger.LogInformation($"The time zone is not set to '{gclSettings.DatabaseTimeZone}'"); 
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

            if (ConnectionForWriting == null)
            {
                ConnectionForWriting = new MySqlConnection { ConnectionString = connectionStringForWriting.ConnectionString };
                CommandForWriting = ConnectionForWriting.CreateCommand();
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

            if (ConnectionForWriting.State == ConnectionState.Closed)
            {
                await ConnectionForWriting.OpenAsync();

                try
                {
                    // Make sure we always use the correct timezone.
                    if (!String.IsNullOrWhiteSpace(gclSettings.DatabaseTimeZone))
                    {
                        CommandForWriting.CommandText = $"SET @@time_zone = {gclSettings.DatabaseTimeZone.ToMySqlSafeValue(true)};";
                        await CommandForWriting.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, $"An error occurred while trying to set the time zone to '{gclSettings.DatabaseTimeZone}'");
                }
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
    }
}
