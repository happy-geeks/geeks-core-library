using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Core.Interfaces;

public interface IEntityTypesService
{
    /// <summary>
    /// Get a list of all dedicated prefixes used on the server
    /// </summary>
    /// <returns>a list of dedicated prefixes strings, or an empty list none have been found.</returns>
    Task<List<string>> GetDedicatedTablePrefixesAsync();

    /// <summary>
    /// Gets the prefix for the wiser_item and wiser_itemdetail tables for a specific entity type.
    /// Certain entity types can have dedicated tables, they won't use wiser_item and wiser_itemdetail, but something like basket_wiser_item and basket_wiser_itemdetail instead.
    /// This function checks wiser_entity if the entity type has dedicated tables and returns the prefix for those tables.
    /// If it doesn't have a dedicated table, an empty string will be returned.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <returns>The table prefix for the given entity type. Returns an empty string if the entity type uses the default tables.</returns>
    Task<string> GetTablePrefixForEntityAsync(string entityType);

    /// <summary>
    /// Gets the prefix for the wiser_item and wiser_itemdetail tables for a specific entity type.
    /// Certain entity types can have dedicated tables, they won't use wiser_item and wiser_itemdetail, but something like basket_wiser_item and basket_wiser_itemdetail instead.
    /// This function checks wiser_entity if the entity type has dedicated tables and returns the prefix for those tables.
    /// If it doesn't have a dedicated table, an empty string will be returned.
    /// </summary>
    /// <param name="entityTypesService">The <see cref="IEntityTypesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
    /// <param name="entityType">The entity type name.</param>
    /// <returns>The table prefix for the given entity type. Returns an empty string if the entity type uses the default tables.</returns>
    Task<string> GetTablePrefixForEntityAsync(IEntityTypesService entityTypesService, string entityType);

    /// <summary>
    /// Gets the prefix for the wiser_item and wiser_itemdetail tables for a specific entity type.
    /// Certain entity types can have dedicated tables, they won't use wiser_item and wiser_itemdetail, but something like basket_wiser_item and basket_wiser_itemdetail instead.
    /// This function checks wiser_entity if the entity type has dedicated tables and returns the prefix for those tables.
    /// If it doesn't have a dedicated table, an empty string will be returned.
    /// </summary>
    /// <param name="entityTypeSettings">A <see cref="EntitySettingsModel"/> with the settings of the entity type.</param>
    /// <returns>The table prefix for the given entity type. Returns an empty string if the entity type uses the default tables.</returns>
    string GetTablePrefixForEntity(EntitySettingsModel entityTypeSettings);

    /// <summary>
    /// Gets the settings for an entity type.
    /// </summary>
    /// <param name="entityType">The name of the entity type.</param>
    /// <param name="moduleId">Optional: The ID of the module, in case the entity type has different settings for different modules.</param>
    /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
    Task<EntitySettingsModel> GetEntityTypeSettingsAsync(string entityType, int moduleId = 0);
}