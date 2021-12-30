using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace GeeksCoreLibrary.Modules.Databases.Services
{
    public class MsSqlDatabaseConnection : IDatabaseConnection, IScopedService
    {
        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public string ConnectedDatabase { get; protected set; }

        /// <inheritdoc />
        public string ConnectedDatabaseForWriting { get; protected set; }

        /// <inheritdoc />
        public Task<DbDataReader> GetReaderAsync(string query)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task<DataTable> GetAsync(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task<string> GetAsJsonAsync(string query, bool formatResult = false, bool skipCache = false)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task<int> ExecuteAsync(string query, bool useWritingConnectionIfAvailable = true, bool cleanUp = true)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task<T> InsertOrUpdateRecordBasedOnParametersAsync<T>(string tableName, T id = default, string idColumnName = "id", bool ignoreErrors = false, bool useWritingConnectionIfAvailable = true)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> InsertRecordAsync(string query, bool useWritingConnectionIfAvailable = true)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task<IDbTransaction> BeginTransactionAsync(bool forceNewTransaction = false)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task CommitTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task RollbackTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void AddParameter(string key, object value)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void ClearParameters()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public string GetDatabaseNameForCaching(bool writeDatabase = false)
        {
            throw new System.NotImplementedException();
        }
        
        /// <inheritdoc />
        public async Task EnsureOpenConnectionForReadingAsync()
        {
            throw new System.NotImplementedException();
        }
        
        /// <inheritdoc />
        public async Task EnsureOpenConnectionForWritingAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void SetCommandTimeout(int value)
        {
            throw new System.NotImplementedException();
        }
    }
}
