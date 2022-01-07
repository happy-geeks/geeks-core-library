using System;

namespace GeeksCoreLibrary.Modules.Databases.Exceptions
{
    public class DatabaseColumnMissingEnumValuesException : Exception
    {
        public string TableName { get; set; }

        public string ColumnName { get; set; }
        
        public DatabaseColumnMissingEnumValuesException(string tableName, string columnName) : base($"No enum values given for column '{columnName}' for table '{tableName}'.")
        {
            TableName = tableName;
            ColumnName = columnName;
        }
        
        public DatabaseColumnMissingEnumValuesException(string tableName, string columnName, Exception innerException) : base($"No enum values given for column '{columnName}' for table '{tableName}'.", innerException)
        {
            TableName = tableName;
            ColumnName = columnName;
        }
    }
}
