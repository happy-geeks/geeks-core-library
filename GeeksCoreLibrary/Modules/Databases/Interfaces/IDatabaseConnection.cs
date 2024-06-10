using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using MySqlConnector;

namespace GeeksCoreLibrary.Modules.Databases.Interfaces
{
    public interface IDatabaseConnection : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Gets the name of the database that the connection is currently connected to.
        /// </summary>
        string ConnectedDatabase { get; }

        /// <summary>
        /// Gets the name of the database that the connection is currently connected to that will be used for write commands.
        /// </summary>
        string ConnectedDatabaseForWriting { get; }

        /// <summary>
        /// Execute a query and get the <see cref="MySqlDataReader"/> with the results.
        /// </summary>
        /// <param name="query">The query to execute and get the results of.</param>
        Task<DbDataReader> GetReaderAsync(string query);

        /// <summary>
        /// Gets results from a query as a DataTable.
        /// </summary>
        /// <param name="query">The query to execute and get the results of.</param>
        /// <param name="skipCache">Optional: Set to true to skip the query cache. Queries that get data, will get cached by default, based on a hash of the query and all parameters.</param>
        /// <param name="cleanUp">Optional: Clean up after the query has been completed.</param>
        /// <param name="useWritingConnectionIfAvailable">Optional: Use the writing connection to get information, if there is one available. If we detect that your query contains a database modification, then we will always use the write connection string, no matter what you enter here.</param>
        Task<DataTable> GetAsync(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false);

        /// <summary>
        /// Gets results from a query as a JSON string.
        /// </summary>
        /// <param name="query">The query to execute and get the results of.</param>
        /// <param name="formatResult">Optional: Set to true to format the JSON to make is easy readable.</param>
        /// <param name="skipCache">Optional: Set to true to skip the query cache. Queries that get data, will get cached by default, based on a hash of the query and all parameters.</param>
        Task<string> GetAsJsonAsync(string query, bool formatResult = false, bool skipCache = false);

        /// <summary>
        /// Executes a query and returns the amount of rows affected.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="useWritingConnectionIfAvailable">Optional: Use the writing connection to get information, if there is one available. If we detect that your query contains a database modification, then we will always use the write connection string, no matter what you enter here.</param>
        /// <param name="cleanUp">Optional: Clean up the connection and command after the query has been completed.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> ExecuteAsync(string query, bool useWritingConnectionIfAvailable = true, bool cleanUp = true);

        /// <summary>
        /// Inserts or updates a record into the specified table, based on the parameters that have been added to the command/connection.
        /// </summary>
        /// <typeparam name="T">The type of the ID. Use ulong for IDs from Wiser 2+.</typeparam>
        /// <param name="tableName">The table name to insert or update a record in.</param>
        /// <param name="id">Optional: The ID of the item to update. If this contains the <see langword="default"/> value of <see langword="T"/>, it will insert a new record.</param>
        /// <param name="idColumnName">Optional: The name of the column in the table that contains the ID / primary key. Default value is "id".</param>
        /// <param name="ignoreErrors">Optional: Whether to ignore certain query errors, such as duplicate keys. Default is <see langword="false"/>.</param>
        /// <param name="useWritingConnectionIfAvailable">Optional: If there is a separate connection for writing data, use that connection. Default is true.</param>
        /// <returns>The new ID of the item if a record was inserted, or the existing ID if it was updated.</returns>
        Task<T> InsertOrUpdateRecordBasedOnParametersAsync<T>(string tableName, T id = default, string idColumnName = "id", bool ignoreErrors = false, bool useWritingConnectionIfAvailable = true);

        /// <summary>
        /// Inserts a record using a query and returns the newly inserted ID.
        /// </summary>
        /// <param name="query">The query that should be executed, which should be an INSERT query.</param>
        /// <param name="useWritingConnectionIfAvailable">Optional: Use the writing connection to get information, if there is one available. If we detect that your query contains a database modification, then we will always use the write connection string, no matter what you enter here.</param>
        /// <returns>The ID of the newly inserted record.</returns>
        Task<long> InsertRecordAsync(string query, bool useWritingConnectionIfAvailable = true);

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <param name="forceNewTransaction">
        ///     Optional: Set to true to force the start of a new transaction. This will rollback any previous transaction.
        ///     If set to false and there is already an active transaction, then an <see cref="InvalidOperationException"/> will be thrown.
        ///     Default value is true.
        /// </param>
        /// <exception cref="InvalidOperationException">If forceNewTransaction is False and there already is a transaction.</exception>
        /// <returns>The transaction as <see cref="IDbTransaction"/>.</returns>
        Task<IDbTransaction> BeginTransactionAsync(bool forceNewTransaction = false);

        /// <summary>
        /// Commits an active transaction.
        /// </summary>
        /// <param name="throwErrorIfNoActiveTransaction">
        ///     Optional: Set to false to do nothing if there is no active transaction.
        ///     If set to true and there is no active transaction, then an <see cref="InvalidOperationException"/> will be thrown.
        ///     Default value is true.
        /// </param>
        Task CommitTransactionAsync(bool throwErrorIfNoActiveTransaction = true);

        /// <summary>
        /// Roll backs an active transaction.
        /// </summary>
        /// <param name="throwErrorIfNoActiveTransaction">
        ///     Optional: Set to false to do nothing if there is no active transaction.
        ///     If set to true and there is no active transaction, then an <see cref="InvalidOperationException"/> will be thrown.
        ///     Default value is true.
        /// </param>
        Task RollbackTransactionAsync(bool throwErrorIfNoActiveTransaction = true);

        /// <summary>
        /// Add a parameter to the <see cref="MySqlCommand"/> to safely use user input in a query.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void AddParameter(string key, object value);

        /// <summary>
        /// Clear all previously added parameters from the <see cref="MySqlCommand"/>.
        /// </summary>
        void ClearParameters();

        /// <summary>
        /// Gets a value with the server and database that can be used if you need to cache something per database, for projects that connect to multiple databases.
        /// </summary>
        /// <param name="writeDatabase">Optional: Set to <see langword="true"/> to get the database of the write connection (if applicable), otherwise get the database of the read connection. Default is <see langword="false"/>.</param>
        /// <returns>The key that you can use in caching.</returns>
        string GetDatabaseNameForCaching(bool writeDatabase = false);

        /// <summary>
        /// If the connection is not open yet, open it.
        /// </summary>
        Task EnsureOpenConnectionForReadingAsync();

        /// <summary>
        /// If the connection is not open yet, open it.
        /// </summary>
        Task EnsureOpenConnectionForWritingAsync();

        /// <summary>
        /// Change the connection strings that are used by the connections.
        /// </summary>
        /// <param name="newConnectionStringForReading">The new connection string to use for reading.</param>
        /// <param name="newConnectionStringForWriting">The new connection string to use for writing.</param>
        /// <param name="sshSettingsForReading">Optional: If the new connection for reading requires SSH, enter the SSH details here.</param>
        /// <param name="sshSettingsForWriting">Optional: If the new connection for writing requires SSH, enter the SSH details here.</param>
        /// <returns></returns>
        Task ChangeConnectionStringsAsync(string newConnectionStringForReading, string newConnectionStringForWriting = null, SshSettings sshSettingsForReading = null, SshSettings sshSettingsForWriting = null);

        /// <summary>
        /// Sets the command timeout in seconds for the connection.
        /// </summary>
        /// <param name="value"></param>
        void SetCommandTimeout(int value);

        /// <summary>
        /// Check whether the connection currently has an active transaction.
        /// </summary>
        bool HasActiveTransaction();

        /// <summary>
        /// Sometimes you might need the underlying DbConnection, for example when using the SqlBulkCopy class.
        /// This function can be used for that.
        /// </summary>
        /// <returns>The <see cref="DbConnection"/> for the reading connection.</returns>
        DbConnection GetConnectionForReading();

        /// <summary>
        /// Sometimes you might need the underlying DbConnection, for example when using the SqlBulkCopy class.
        /// This function can be used for that.
        /// </summary>
        /// <returns>The <see cref="DbConnection"/> for the writing connection.</returns>
        DbConnection GetConnectionForWriting();

        /// <summary>
        /// Bulk insert a <see cref="DataTable"/> into a table in the database.
        /// </summary>
        /// <param name="dataTable">The <see cref="DataTable"/> that contains the data to insert.</param>
        /// <param name="tableName">The name of the table to insert the data into.</param>
        /// <param name="useWritingConnectionIfAvailable">Optional: Use the writing connection to get information, if there is one available. If we detect that your query contains a database modification, then we will always use the write connection string, no matter what you enter here.</param>
        /// <param name="useInsertIgnore">Optional: Whether to use INSERT IGNORE instead of INSERT, to ignore errors such as duplicate keys.</param>
        Task<int> BulkInsertAsync(DataTable dataTable, string tableName, bool useWritingConnectionIfAvailable = true, bool useInsertIgnore = false);
    }
}