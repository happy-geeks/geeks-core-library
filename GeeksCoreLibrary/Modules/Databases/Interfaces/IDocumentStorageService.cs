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
    /// 
    /// </summary>
    /// <param name="wiserItem"></param>
    /// <param name="entitySettings"></param>
    /// <returns></returns>
    Task<WiserItemModel> StoreItemAsync(WiserItemModel wiserItem, EntitySettingsModel entitySettings = null);
    
    /// <summary>
    /// Gets items that were changed after the given date
    /// </summary>
    /// <param name="dateTime">the date the items are compared by</param>
    /// <param name="entitySettings">Optional: settings for the entityType, used to determine name of collection</param>
    /// <returns></returns>
    Task<IReadOnlyCollection<WiserItemModel>> GetItemsChangedAfter(DateTime dateTime, EntitySettingsModel entitySettings = null);
    
    /// <summary>
    /// Gets the items that
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="parameters"></param>
    /// <param name="entitySettings"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<WiserItemModel>> GetItems(string condition, Dictionary<string, object> parameters, EntitySettingsModel entitySettings = null);

    /// <inheritdoc />
    Task CreateCollection(string prefix = "");
}