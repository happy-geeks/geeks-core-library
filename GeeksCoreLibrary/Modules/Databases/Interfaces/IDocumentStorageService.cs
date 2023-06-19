using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Databases.Interfaces;

/// <summary>
/// Service for storing and retrieving wiser items from the document store
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>
    /// Add items to the document store
    /// </summary>
    /// <param name="wiserItem"></param>
    /// <param name="entitySettings"></param>
    /// <returns>The retrieved wiser item</returns>
    Task<WiserItemModel> StoreItemAsync(WiserItemModel wiserItem, EntitySettingsModel entitySettings = null);
    
    /// <summary>
    /// Gets items that were changed after the given date
    /// </summary>
    /// <param name="dateTime">the date the items are compared by</param>
    /// <param name="entitySettings">Optional: settings for the entityType</param>
    /// <returns>Collection of retrieved wiser items</returns>
    Task<IReadOnlyCollection<WiserItemModel>> GetItemsChangedAfter(DateTime dateTime, EntitySettingsModel entitySettings = null);
    
    /// <summary>
    /// Gets the items that
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="parameters"></param>
    /// <param name="entitySettings"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<WiserItemModel>> GetItems(string condition, Dictionary<string, object> parameters, EntitySettingsModel entitySettings = null);

    /// <summary>
    /// Creates the collection the documents will be stored in
    /// </summary>
    /// <param name="prefix">Prefix of the collection name</param>
    /// <returns></returns>
    Task CreateCollection(string prefix = "");

    /// <summary>
    /// Empty the document store collection
    /// </summary>
    /// <param name="entitySettings">Optional: settings for the entityType</param>
    /// <returns></returns>
    public Task<ulong> EmptyItemCollectionAsync(EntitySettingsModel entitySettings = null);

    /// <summary>
    /// Delete all items older than the given date
    /// </summary>
    /// <param name="dateTime">The datetime used for the age comparison</param>
    /// <param name="entitySettings">Optional: settings for the entityType</param>
    /// <returns></returns>
    public Task<ulong> DeleteItemOlderThanAsync(DateTime dateTime, EntitySettingsModel entitySettings = null);

    /// <summary>
    /// Delete the given item
    /// </summary>
    /// <param name="wiserItem">The item that should be deleted</param>
    /// <param name="entitySettings">Optional: settings for the entityType</param>
    public Task DeleteItem(WiserItemModel wiserItem, EntitySettingsModel entitySettings = null);
}