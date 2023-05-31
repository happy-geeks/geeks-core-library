using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MySqlX;
using MySqlX.XDevAPI;

namespace GeeksCoreLibrary.Modules.Databases.Services;


public class MySqlDocumentStorageService : IDocumentStorageService, IScopedService
{
    private readonly GclSettings gclSettings;

    public MySqlDocumentStorageService(IOptions<GclSettings> gclSettings)
    {
        this.gclSettings = gclSettings.Value;
    }
    
    public async Task<WiserItemModel> StoreDocumentAsync(WiserItemModel wiserItem, EntitySettingsModel entitySettingsModel)
    {
        var session = MySQLX.GetSession(gclSettings.ConnectionStringDocumentStore);
        var schema = session.GetCurrentSchema();
        
        var collection = schema.GetCollection($"{entitySettingsModel.DedicatedTablePrefix}wiser_item_store");

        if (wiserItem.Id != 0)
        {
            await collection.Modify("Id = ?id").Bind("?id", wiserItem.Id).Patch(wiserItem).ExecuteAsync();
        }
        else
        {
            var result = await collection.Add(wiserItem).ExecuteAsync();
            wiserItem.Id = result.AutoIncrementValue;
        }

        return wiserItem;
    }
}