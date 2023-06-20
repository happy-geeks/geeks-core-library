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
    /// <returns>Tuple of the retrieved wiser item and document id</returns>
    Task<(WiserItemModel model, string documentId)> StoreItemAsync(WiserItemModel wiserItem, EntitySettingsModel entitySettings = null);

    /// <summary>
    /// Gets the items that
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="parameters"></param>
    /// <param name="entitySettings"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<(WiserItemModel model, string documentId)>> GetItems(string condition, Dictionary<string, object> parameters, EntitySettingsModel entitySettings = null);

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
    /// Delete the given item
    /// </summary>
    /// <param name="documentId">The exact document id</param>
    /// <param name="entitySettings">Optional: settings for the entityType</param>
    Task DeleteItem(string documentId, EntitySettingsModel entitySettings = null);
}