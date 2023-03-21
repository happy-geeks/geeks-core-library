namespace GeeksCoreLibrary.Modules.Databases.Models;

public class Constants
{
    /// <summary>
    /// The table name that is used to log connections to the database are created and when they are closed again.
    /// This is meant for debugging when you are having problems with database connections that stay open too long.
    /// </summary>
    public const string DatabaseConnectionLogTableName = "gcl_database_connection_log";
}