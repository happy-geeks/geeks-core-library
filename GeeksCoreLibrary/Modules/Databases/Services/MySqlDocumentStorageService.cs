using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;

namespace GeeksCoreLibrary.Modules.Databases.Services;

/// <inheritdoc cref="GeeksCoreLibrary.Modules.Databases.Interfaces.IDocumentStorageService" />
public class MySqlDocumentStorageService : IDocumentStorageService, IScopedService
{
    private readonly IDocumentStoreConnection documentStorageConnection;
    
    public MySqlDocumentStorageService(IDocumentStoreConnection documentStorageConnection)
    {
        this.documentStorageConnection = documentStorageConnection;
    }

    /// <inheritdoc />
    public async Task CreateCollectionAsync(string prefix = "")
    {
        List<(string name, DocumentStoreIndexModel index)> indices = new()
        {
            ("idx_id", new DocumentStoreIndexModel
            {
                Fields =
                {
                    new DocumentStoreIndexFieldModel
                    {
                        Field = "id",
                        Required = true,
                        Type = "BIGINT UNSIGNED"
                    }
                }
            }),
            ("idx_title", new DocumentStoreIndexModel
            {
                Fields =
                {
                    new DocumentStoreIndexFieldModel
                    {
                        Field = "title",
                        Required = true,
                        Type = "VARCHAR(255)"
                    }
                }
            }),
            ("idx_changedOn", new DocumentStoreIndexModel
            {
                Fields =
                {
                    new DocumentStoreIndexFieldModel
                    {
                        Field = "changedOn",
                        Required = true,
                        Type = "DATETIME"
                    }
                }
            })
        };
        await documentStorageConnection.CreateCollectionAsync($"{prefix}{WiserTableNames.WiserItemStore}", indices);
    }

    /// <inheritdoc />
    public async Task<(WiserItemModel model, string documentId)> StoreItemAsync(WiserItemModel wiserItem, EntitySettingsModel entitySettings = null)
    {
        var prefix = entitySettings?.DedicatedTablePrefix ?? String.Empty;
        
        wiserItem.ChangedOn = DateTime.Now;
        
        var id = await documentStorageConnection.InsertOrUpdateDocumentAsync($"{prefix}{WiserTableNames.WiserItemStore}", wiserItem, wiserItem.UniqueUuid);

        return (wiserItem, id);
    }
    
    /// <inheritdoc />
    public async Task<ulong> EmptyItemCollectionAsync(EntitySettingsModel entitySettings = null)
    {
        var prefix = entitySettings?.DedicatedTablePrefix ?? String.Empty;
        return await documentStorageConnection.RemoveDocumentsAsync($"{prefix}wiser_item_store");
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(string documentId, EntitySettingsModel entitySettings = null)
    {
        var prefix = entitySettings?.DedicatedTablePrefix ?? String.Empty;

        await documentStorageConnection.RemoveDocumentAsync($"{prefix}wiser_item_store", documentId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<(WiserItemModel model, string documentId)>> GetItemsAsync(string condition, Dictionary<string, object> parameters, EntitySettingsModel entitySettings = null)
    {
        var prefix = entitySettings?.DedicatedTablePrefix ?? String.Empty;
        documentStorageConnection.ClearParameters();
        foreach (var parameter in parameters)
        {
            documentStorageConnection.AddParameter(parameter.Key, parameter.Value);   
        }

        var array = await documentStorageConnection.GetDocumentsAsync($"{prefix}wiser_item_store", condition);

        var collection = array.ToObject<List<WiserItemModel>>().AsReadOnly();

        var ids = array.Select(t => t.Value<string>("_id"));

        return collection.Zip(ids).ToList().AsReadOnly();
    }
}