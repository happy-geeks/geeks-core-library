﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Databases.Models;

namespace GeeksCoreLibrary.Modules.Databases.Interfaces;

/// <summary>
/// A service with helper functions for doing generic things in a database, such as creating or updating tables.
/// </summary>
public interface IDatabaseHelpersService
{
    /// <summary>
    /// If your project has extra tables, you can add them here and use this class to automatically keep them updated.
    /// </summary>
    List<WiserTableDefinitionModel> ExtraWiserTableDefinitions { get; set; }

    /// <summary>
    /// Get all columns for specific tables.
    /// </summary>
    /// <param name="tableNames">The tables to get the columns for.</param>
    /// <param name="databaseName">Optional: The name of the database schema that the tables belong to. Leave empty to use the database schema from the connection string.</param>
    /// <returns>A <see cref="Dictionary{T,T}"/> where the key is the name of the table and the value is a <see cref="List{t}"/> of <see cref="ColumnSettingsModel"/> with all columns of that table.</returns>
    Task<Dictionary<string, List<ColumnSettingsModel>>> GetColumnsAsync(List<string> tableNames, string databaseName = null);

    /// <summary>
    /// Get all columns for a specific table.
    /// </summary>
    /// <param name="tableName">The name of the table to get the columns for.</param>
    /// <param name="databaseName">Optional: The name of the database schema that the tables belong to. Leave empty to use the database schema from the connection string.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="ColumnSettingsModel"/> with all columns of that table.</returns>
    Task<List<ColumnSettingsModel>> GetColumnsAsync(string tableName, string databaseName = null);

    /// <summary>
    /// Get all columns for a specific table.
    /// </summary>
    /// <param name="databaseHelpersService">The <see cref="IDatabaseHelpersService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods of the same service.</param>
    /// <param name="tableName">The name of the table to get the columns for.</param>
    /// <param name="databaseName">Optional: The name of the database schema that the tables belong to. Leave empty to use the database schema from the connection string.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="ColumnSettingsModel"/> with all columns of that table.</returns>
    Task<List<ColumnSettingsModel>> GetColumnsAsync(IDatabaseHelpersService databaseHelpersService, string tableName, string databaseName = null);

    /// <summary>
    /// Check whether or not a column exists in a specific table.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task<bool> ColumnExistsAsync(string tableName, string columnName, string databaseName = null);

    /// <summary>
    /// Gets a list of all column names in a table.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task<List<string>> GetColumnNamesAsync(string tableName, string databaseName = null);

    /// <summary>
    /// Add a new column to the table, if it doesn't exist yet.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="settings">The column settings.</param>
    /// <param name="throwExceptionIfColumnAlreadyExists">Optional: Whether or not to throw an exception if the column already exists, default is <see langword="true"/>.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task AddColumnToTableAsync(string tableName, ColumnSettingsModel settings, bool throwExceptionIfColumnAlreadyExists = true, string databaseName = null);

    /// <summary>
    /// Add a new column to the table, if it doesn't exist yet.
    /// </summary>
    /// <param name="databaseHelpersService">The <see cref="IDatabaseHelpersService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods of the same service.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="settings">The column settings.</param>
    /// <param name="throwExceptionIfColumnAlreadyExists">Optional: Whether or not to throw an exception if the column already exists, default is <see langword="true"/>.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task AddColumnToTableAsync(IDatabaseHelpersService databaseHelpersService, string tableName, ColumnSettingsModel settings, bool throwExceptionIfColumnAlreadyExists = true, string databaseName = null);

    /// <summary>
    /// Delete a column from a table.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task DropColumnAsync(string tableName, string columnName, string databaseName = null);

    /// <summary>
    /// Creates a new table, if it doesn't exist yet.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="primaryKeys">The primary keys for the table.</param>
    /// <param name="characterSet">Optional: The default character set for the table. Default value is 'utf8mb4'.</param>
    /// <param name="collation">Optional: The default collation for the table. Default value is 'utf8mb4_general_ci'.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task CreateTableAsync(string tableName, IList<ColumnSettingsModel> primaryKeys, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null);

    /// <summary>
    /// Creates a new table, or update is if it already exists.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columns">The columns for the table.</param>
    /// <param name="characterSet">Optional: The default character set for the table. Default value is 'utf8mb4'.</param>
    /// <param name="collation">Optional: The default collation for the table. Default value is 'utf8mb4_general_ci'.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task CreateOrUpdateTableAsync(string tableName, IList<ColumnSettingsModel> columns, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null);

    /// <summary>
    /// Creates a new table, or update is if it already exists.
    /// </summary>
    /// <param name="databaseHelpersService">The <see cref="IDatabaseHelpersService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods of the same service.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columns">The columns for the table.</param>
    /// <param name="characterSet">Optional: The default character set for the table. Default value is 'utf8mb4'.</param>
    /// <param name="collation">Optional: The default collation for the table. Default value is 'utf8mb4_general_ci'.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task CreateOrUpdateTableAsync(IDatabaseHelpersService databaseHelpersService, string tableName, IList<ColumnSettingsModel> columns, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null);

    /// <summary>
    /// Checks whether or not a table exists.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="databaseName">Optional: The name of the database. Default value is the database from the connection string.</param>
    Task<bool> TableExistsAsync(string tableName, string databaseName = null);

    /// <summary>
    /// Checks whether a database schema exists.
    /// </summary>
    /// <param name="databaseName">The name of the database.</param>
    Task<bool> DatabaseExistsAsync(string databaseName);

    /// <summary>
    /// Delete a table from the database.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="isTemporaryTable">Optional: Whether or not this is a temporary table. Default is <see langword="false" />.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task DropTableAsync(string tableName, bool isTemporaryTable = false, string databaseName = null);

    /// <summary>
    /// Create a duplicate of an existing table.
    /// </summary>
    /// <param name="tableToDuplicate">The name of the table to duplicate.</param>
    /// <param name="newTableName">The name for the new duplicated table.</param>
    /// <param name="includeData">Optional: Whether or not to also duplicate the data of the table. Default value is <see langword="true" />.</param>
    /// <param name="sourceDatabaseName">Optional: The name of the database schema to copy the table from. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    /// <param name="destinationDatabaseName">Optional: The name of the database schema to copy the table to. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task DuplicateTableAsync(string tableToDuplicate, string newTableName, bool includeData = true, string sourceDatabaseName = null, string destinationDatabaseName = null);

    /// <summary>
    /// Get all indexes for specific tables.
    /// </summary>
    /// <param name="tableNames">The tables to get the indexes for.</param>
    /// <param name="databaseName">Optional: The name of the database schema that the tables belong to. Leave empty to use the database schema from the connection string.</param>
    /// <returns>A <see cref="Dictionary{T,T}"/> where the key is the name of the table and the value is a <see cref="List{t}"/> of <see cref="IndexSettingsModel"/> with all indexes of that table.</returns>
    Task<Dictionary<string, List<IndexSettingsModel>>> GetIndexesAsync(List<string> tableNames, string databaseName = null);

    /// <summary>
    /// Get all indexes for a specific table.
    /// </summary>
    /// <param name="tableName">The name of the table to get the indexes for.</param>
    /// <param name="databaseName">Optional: The name of the database schema that the tables belong to. Leave empty to use the database schema from the connection string.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="IndexSettingsModel"/> with all indexes of that table.</returns>
    Task<List<IndexSettingsModel>> GetIndexesAsync(string tableName, string databaseName = null);

    /// <summary>
    /// Get all indexes for a specific table.
    /// </summary>
    /// <param name="databaseHelpersService">The <see cref="IDatabaseHelpersService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods of the same service.</param>
    /// <param name="tableName">The name of the table to get the indexes for.</param>
    /// <param name="databaseName">Optional: The name of the database schema that the tables belong to. Leave empty to use the database schema from the connection string.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="IndexSettingsModel"/> with all indexes of that table.</returns>
    Task<List<IndexSettingsModel>> GetIndexesAsync(IDatabaseHelpersService databaseHelpersService, string tableName, string databaseName = null);

    /// <summary>
    /// Creates or updates one or more indexes. This method will check if an index with the given name already exists,
    /// if it doesn't it will create that index, otherwise it will check if the index has been changed.
    /// If it has been changed, then the index will be dropped and recreated.
    /// </summary>
    /// <param name="indexes">A list with one or more <see cref="IndexSettingsModel"/>.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task CreateOrUpdateIndexesAsync(List<IndexSettingsModel> indexes, string databaseName = null);

    /// <summary>
    /// Creates or updates one or more indexes. This method will check if an index with the given name already exists,
    /// if it doesn't it will create that index, otherwise it will check if the index has been changed.
    /// If it has been changed, then the index will be dropped and recreated.
    /// </summary>
    /// <param name="databaseHelpersService">The <see cref="IDatabaseHelpersService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods of the same service.</param>
    /// <param name="indexes">A list with one or more <see cref="IndexSettingsModel"/>.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task CreateOrUpdateIndexesAsync(IDatabaseHelpersService databaseHelpersService, List<IndexSettingsModel> indexes, string databaseName = null);

    /// <summary>
    /// Create a new database if it doesn't exist yet.
    /// </summary>
    /// <param name="databaseName">The name of the database to create.</param>
    /// <param name="characterSet">Optional: The default character set for the new database. Default value is 'utf8mb4'.</param>
    /// <param name="collation">Optional: The default collation for the new database. Default value is 'utf8mb4_general_ci'.</param>
    Task CreateDatabaseAsync(string databaseName, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci");

    /// <summary>
    /// Deletes a database scheme from the server.
    /// WARNING: This cannot be undone, even with transactions. Only use this method if you're 100% sure of doing this!
    /// </summary>
    /// <param name="databaseName">The name of the database scheme to delete.</param>
    Task DropDatabaseAsync(string databaseName);

    /// <summary>
    /// Gets the list of all migrations that have been done on the database and the dates and times when they have been done.
    /// </summary>
    /// <returns>A Dictionary with table name and last update datetime.</returns>
    Task<Dictionary<string, DateTime>> GetMigrationsStatusAsync(string databaseSchema = null);

    /// <summary>
    /// Gets the list of all migrations that have been done on the database and the dates and times when they have been done.
    /// </summary>
    /// <param name="databaseHelpersService">The <see cref="IDatabaseHelpersService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods of the same service.</param>
    /// <returns>A Dictionary with table name and last update datetime.</returns>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    Task<Dictionary<string, DateTime>> GetMigrationsStatusAsync(IDatabaseHelpersService databaseHelpersService, string databaseName = null);

    /// <summary>
    /// Check if certain Wiser tables are up-to-date and update them if they're not.
    /// Returns a boolean to indicate whether any changes have been made to any table.
    /// </summary>
    /// <param name="tablesToUpdate">A list of one or more tables to check and update when needed.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    /// <returns>A boolean, indicating whether any changes have been made.</returns>
    Task<bool> CheckAndUpdateTablesAsync(List<string> tablesToUpdate, string databaseName = null);

    /// <summary>
    /// Check if certain Wiser tables are up-to-date and update them if they're not.
    /// Returns a boolean to indicate whether any changes have been made to any table.
    /// </summary>
    /// <param name="databaseHelpersService">The <see cref="IDatabaseHelpersService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods of the same service.</param>
    /// <param name="tablesToUpdate">A list of one or more tables to check and update when needed.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    /// <returns>A boolean, indicating whether any changes have been made.</returns>
    Task<bool> CheckAndUpdateTablesAsync(IDatabaseHelpersService databaseHelpersService, List<string> tablesToUpdate, string databaseName = null);

    /// <summary>
    /// Retrieves the names of all tables within the current database, or in the database specified by <paramref name="databaseName"/>.
    /// </summary>
    /// <param name="includeViews">Optional: Whether views should also be returned. Default value is <see langword="false"/>.</param>
    /// <param name="databaseName">Optional: The name of the database schema. Leave empty to use the database from the connection string. Default value is <see langword="null"/>.</param>
    /// <returns>A list of strings.</returns>
    Task<IList<string>> GetAllTableNamesAsync(bool includeViews = false, string databaseName = null);

    /// <summary>
    /// Change the name of an existing database table.
    /// </summary>
    /// <param name="currentTableName">The current / old name of the table.</param>
    /// <param name="newTableName">The new name of the table.</param>
    Task RenameTableAsync(string currentTableName, string newTableName);

    /// <summary>
    /// Optimize one or more tables.
    /// </summary>
    /// <param name="tableNames">The name(s) of the table(s) to optimize.</param>
    /// <returns></returns>
    Task OptimizeTablesAsync(params string[] tableNames);

    /// <summary>
    /// Get the enum values of a column that has the enum type
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <returns>A list of the enum values of the specified column.</returns>
    Task<IList<string>> GetColumnEnumValues(string tableName, string columnName);

    /// <summary>
    /// Update the enum values of a column to the values specified in the settings model.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="column">The desired settings of the column.</param>
    Task UpdateColumnEnumValues(string tableName, ColumnSettingsModel column);
}