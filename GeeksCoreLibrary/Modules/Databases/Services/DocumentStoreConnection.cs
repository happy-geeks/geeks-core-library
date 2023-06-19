using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GeeksCoreLibrary.Modules.Databases.Services;

public class DocumentStoreConnection : IDocumentStoreConnection, IScopedService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<DocumentStoreConnection> logger;

    private Session Session { get; set; }

    private readonly GclSettings gclSettings;

    private readonly ConcurrentDictionary<string, object> parameters = new();
    private readonly JsonSerializerSettings jsonSerializerSettings;

    /// <summary>
    /// Creates a new instance of <see cref="DocumentStoreConnection"/>.
    /// </summary>
    public DocumentStoreConnection(IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, ILogger<DocumentStoreConnection> logger)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
        this.gclSettings = gclSettings.Value;

        jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    /// <inheritdoc />
    public string ConnectedDatabase { get; private set; }

    /// <inheritdoc />
    public Task CreateCollectionAsync(string collectionName, List<(string Name, DocumentStoreIndexModel Indexes)> collectionIndexes)
    {
        EnsureOpenSession();

        var collection = Session.Schema.CreateCollection(collectionName, true);
        if (collectionIndexes == null)
        {
            return Task.CompletedTask;
        }
        
        // Create indices
        foreach (var (name, indexes) in collectionIndexes)
        {
            collection.CreateIndex(name, JsonConvert.SerializeObject(indexes, jsonSerializerSettings));
        }

        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<bool> CollectionExists(string name)
    {
        EnsureOpenSession();
        var collection = Session.Schema.GetCollection(name);

        return Task.FromResult(collection.ExistsInDatabase());
    }
    
    private Collection GetCollection(string name, bool validateExistence = false)
    {
        EnsureOpenSession();
        var collection = Session.Schema.GetCollection(name, validateExistence);

        return collection.ExistsInDatabase() ? collection : Session.Schema.CreateCollection(name);
    }

    /// <inheritdoc />
    public async Task<JArray> GetDocumentsAsync(string collectionName, string condition)
    {
        EnsureOpenSession();

        var findParameters = new DbDoc();
        if (!parameters.IsEmpty)
        {
            foreach (var parameter in parameters)
            {
                findParameters.SetValue(parameter.Key, parameter.Value);
            }
        }

        var collection = Session.Schema.GetCollection(collectionName, true);

        // Create the FindStatement first.
        var findStatement = collection.Find(condition);

        // Bind the parameters.
        foreach (var parameter in parameters)
        {
            findStatement.Bind(parameter.Key, parameter.Value);
        }

        using var documents = await findStatement.ExecuteAsync();
        
        var items = documents.FetchAll();

        JArray array = new JArray();
        foreach (var item in items)
        {
            array.Add(JToken.Parse(item.ToString()));
        }

        return array;
    }

    /// <inheritdoc />
    public async Task<JObject> GetItemByIdAsync(string collectionName, object id)
    {
        if (id == null)
        {
            return null;
        }

        EnsureOpenSession();

        var collection = Session.Schema.GetCollection(collectionName, true);
        var result = await collection.Find("_id=?id").Bind("?id", id).ExecuteAsync();

        var doc = result.FetchOne();

        return JObject.Parse(doc.ToString());
    }

    /// <inheritdoc />
    public void AddParameter(string key, object value)
    {
        if (parameters.ContainsKey(key))
        {
            parameters.TryRemove(key, out _);
        }

        parameters.TryAdd(key, value);
    }

    /// <inheritdoc />
    public void ClearParameters()
    {
        parameters.Clear();
    }

    /// <summary>
    /// Will make sure the Session is open and is connected to the correct Schema.
    /// </summary>
    private void EnsureOpenSession()
    {
        if (Session != null)
        {
            return;
        }

        Session = MySQLX.GetSession(gclSettings.DocumentStoreConnectionString);
        
        ConnectedDatabase = Session.Schema.Name;
    }

    /// <inheritdoc />
    public async Task<string> InsertOrUpdateDocumentAsync(string collectionName, object item, ulong id = 0)
    {
        var collection = GetCollection(collectionName);

        var jsonItem = JObject.FromObject(item, new JsonSerializer()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        });
        
        if (id == 0UL)
        {
            // New item.
            var result = await collection.Add(jsonItem).ExecuteAsync();
            return result.GeneratedIds[0];
        }
        
        await collection.Modify("id = :docId").Patch(jsonItem).Bind("docId", $"%{id}").ExecuteAsync();

        return null;
    }

    /// <inheritdoc />
    public async Task ModifyDocumentByIdAsync(string collectionName, string id, object item)
    {
        var collection = GetCollection(collectionName);
        await collection.Modify("id = :docId").Patch(JsonConvert.SerializeObject(item, jsonSerializerSettings)).Bind("docId", id).ExecuteAsync();
    }

    /// <inheritdoc />
    public async Task<ulong> RemoveDocumentsAsync(string collectionName, string condition = "")
    {
        EnsureOpenSession();
        var collection = GetCollection(collectionName);
        var removeStatement = collection.Remove(condition);
        
        // Bind the parameters.
        foreach (var parameter in parameters)
        {
            removeStatement.Bind(parameter.Key, parameter.Value);
        }

        var result = await removeStatement.ExecuteAsync();

        return result.AffectedItemsCount;
    }
    
    /// <inheritdoc />
    public async Task RemoveDocumentAsync(string collectionName, ulong id)
    {
        EnsureOpenSession();
        var collection = GetCollection(collectionName);
        await collection.Remove("id = :docId").Bind("docId", id).ExecuteAsync();
    }

    /// <inheritdoc />
    public void StartTransaction()
    {
        EnsureOpenSession();
        Session.StartTransaction();
    }

    /// <inheritdoc />
    public void CommitTransaction()
    {
        Session.Commit();
    }

    /// <inheritdoc />
    public void RollbackTransaction()
    {
        Session.Rollback();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        logger.LogTrace($"Disposing instance of DocumentStoreConnection on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext)}");
        Session?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        logger.LogTrace($"Disposing instance of DocumentStoreConnection on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext)}");
        Session?.Dispose();
        GC.SuppressFinalize(this);
    }
}