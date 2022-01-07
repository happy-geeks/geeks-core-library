using System;

namespace GeeksCoreLibrary.Modules.Databases.Exceptions
{
    public class DatabaseColumnExistsException : Exception
    {
        public string TableName { get; set; }

        public string ColumnName { get; set; }
        
        public DatabaseColumnExistsException(string tableName, string columnName) : base($"The column '{columnName}' for table '{tableName}' already exists")
        {
            TableName = tableName;
            ColumnName = columnName;
        }
        
        public DatabaseColumnExistsException(string tableName, string columnName, Exception innerException) : base($"The column '{columnName}' for table '{tableName}' already exists", innerException)
        {
            TableName = tableName;
            ColumnName = columnName;
        }
    }
}
