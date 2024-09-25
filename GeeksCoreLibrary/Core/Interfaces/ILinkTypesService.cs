using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Core.Interfaces;

public interface ILinkTypesService
{
    /// <summary>
    /// Gets the settings for a link type. These settings will be cached for 1 hour.
    /// </summary>
    /// <param name="linkType">Optional: The type number of the link type.</param>
    /// <param name="sourceEntityType">Optional: The entity type of the source item.</param>
    /// <param name="destinationEntityType">Optional: The entity type of the destination item.</param>
    /// <exception cref="ArgumentException">If linkType, sourceEntityType and destinationEntityType are all empty.</exception>
    /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
    Task<LinkSettingsModel> GetLinkTypeSettingsAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null);

    /// <summary>
    /// Gets the settings for a link type. These settings will be cached for 1 hour.
    /// </summary>
    /// <returns>A List of <see cref="EntitySettingsModel"/> containing all link settings.</returns>
    Task<List<LinkSettingsModel>> GetAllLinkTypeSettingsAsync();

    /// <summary>
    /// Gets the settings for a link type by id. These settings will be cached for 1 hour.
    /// </summary>
    /// <param name="linkId">The id of the link type</param>
    /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
    Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(int linkId);

    /// <summary>
    /// Gets the settings for a link type by id. These settings will be cached for 1 hour.
    /// </summary>
    /// <param name="linkTypesService">The <see cref="ILinkTypesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
    /// <param name="linkId">The id of the link type</param>
    /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
    Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(ILinkTypesService linkTypesService, int linkId);

    /// <summary>
    /// Gets the prefix for the wiser_itemlink and wiser_itemlinkdetail tables for a specific link type.
    /// Certain link types can have dedicated tables, they won't use wiser_itemlink and wiser_itemlinkdetail, but something like 123_wiser_itemlink and 123_wiser_itemlinkdetail instead.
    /// This function checks wiser_link if the entity type has dedicated tables and returns the prefix for those tables.
    /// If it doesn't have a dedicated table, an empty string will be returned.
    /// </summary>
    /// <param name="linkType">Optional: The type number of the link type.</param>
    /// <param name="sourceEntityType">Optional: The entity type of the source item.</param>
    /// <param name="destinationEntityType">Optional: The entity type of the destination item.</param>
    /// <exception cref="ArgumentException">If linkType, sourceEntityType and destinationEntityType are all empty.</exception>
    /// <returns>The table prefix for the given link type. Returns an empty string if the link type uses the default tables.</returns>
    Task<string> GetTablePrefixForLinkAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null);

    /// <summary>
    /// Gets the prefix for the wiser_itemlink and wiser_itemlinkdetail tables for a specific link type.
    /// Certain link types can have dedicated tables, they won't use wiser_itemlink and wiser_itemlinkdetail, but something like 123_wiser_itemlink and 123_wiser_itemlinkdetail instead.
    /// This function checks wiser_link if the entity type has dedicated tables and returns the prefix for those tables.
    /// If it doesn't have a dedicated table, an empty string will be returned.
    /// </summary>
    /// <param name="linkTypesService">The <see cref="ILinkTypesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
    /// <param name="linkType">Optional: The type number of the link type.</param>
    /// <param name="sourceEntityType">Optional: The entity type of the source item.</param>
    /// <param name="destinationEntityType">Optional: The entity type of the destination item.</param>
    /// <exception cref="ArgumentException">If linkType, sourceEntityType and destinationEntityType are all empty.</exception>
    /// <returns>The table prefix for the given link type. Returns an empty string if the link type uses the default tables.</returns>
    Task<string> GetTablePrefixForLinkAsync(ILinkTypesService linkTypesService, int linkType = 0, string sourceEntityType = null, string destinationEntityType = null);

    /// <summary>
    /// Gets the prefix for the wiser_itemlink and wiser_itemlinkdetail tables for a specific link type.
    /// Certain link types can have dedicated tables, they won't use wiser_itemlink and wiser_itemlinkdetail, but something like 123_wiser_itemlink and 123_wiser_itemlinkdetail instead.
    /// This function checks wiser_link if the entity type has dedicated tables and returns the prefix for those tables.
    /// If it doesn't have a dedicated table, an empty string will be returned.
    /// </summary>
    /// <param name="linkTypeSettings">A <see cref="LinkSettingsModel"/> with the settings of the link type.</param>
    /// <returns>The table prefix for the given link type. Returns an empty string if the link type uses the default tables.</returns>
    string GetTablePrefixForLink(LinkSettingsModel linkTypeSettings);
}