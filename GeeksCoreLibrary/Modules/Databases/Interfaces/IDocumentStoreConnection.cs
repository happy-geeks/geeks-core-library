using GeeksCoreLibrary.Core.Models;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Databases.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;

 public interface IDocumentStoreConnection : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the name of the database that the connection is currently connected to.
    /// </summary>
    string ConnectedDatabase { get; }

    /// <summary>
    /// Creates a new collection in the document store.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="collectionIndexes">The indexes the store will get.</param>
    /// <returns>The newly created <see cref="Collection"/>.</returns>
    Task CreateCollectionAsync(string collectionName, List<(string Name, DocumentStoreIndexModel Indexes)> collectionIndexes = null);

    /// <summary>
    /// Retrieves documents from a collection, filtered by a condition.
    /// </summary>
    /// <param name="collectionName">The name of the collection to insert the new item into.</param>
    /// <param name="condition">The condition by which the documents will be retrieved</param>
    /// <returns></returns>
    Task<JArray> GetDocumentsAsync(string collectionName, string condition);

    /// <summary>
    /// Retrieves a single document from a collection by ID.
    /// </summary>
    /// <param name="collectionName">The name of the collection to insert the new item into.</param>
    /// <param name="id">id of the document</param>
    /// <returns></returns>
    Task<JObject> GetItemByIdAsync(string collectionName, object id);

    /// <summary>
    /// Add a parameter that will be used in the <see cref="FindStatement"/> to safely use user input in a query.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    void AddParameter(string key, object value);

    /// <summary>
    /// Clear all previously added parameters.
    /// </summary>
    void ClearParameters();

    /// <summary>
    /// Inserts a new document, or updates an existing one.
    /// </summary>
    /// <param name="collectionName">The name of the collection to insert the new item into.</param>
    /// <param name="item">The item that will be serialized.</param>
    /// <param name="id">The document ID. If the item doesn't exist yet, a new ID will be generated.</param>
    /// <returns>The <see cref="Result"/> object that contains data about the inserted/updated item.</returns>
    Task<string> InsertOrUpdateDocumentAsync(string collectionName, object item, ulong id = 0);

    /// <summary>
    /// Modifies a document by its internal id (the _id column). Note that it has to match the _id column exactly.
    /// </summary>
    /// <param name="collectionName">The name of the collection to insert the new item into.</param>
    /// <param name="id">The exact document ID.</param>
    /// <param name="item">The item that will be serialized.</param>
    /// <returns>The <see cref="Result"/> object that contains data about the modified item.</returns>
    Task ModifyDocumentByIdAsync(string collectionName, string id, object item);

    /// <summary>
    /// Removes all documents from the collection
    /// </summary>
    /// <param name="collectionName">The name of the collection to insert the new item into.</param>
    /// <param name="condition">The condition by which the documents will be retrieved</param>
    /// <returns>Amount of documents that were deleted</returns>
    Task<ulong> RemoveDocumentsAsync(string collectionName, string condition = "");
    
    /// <summary>
    /// Removes the given document from the document store
    /// </summary>
    /// <param name="collectionName">The name of the collection to insert the new item into.</param>
    /// <param name="id">The exact document ID.</param>
    /// <returns></returns>
    Task RemoveDocumentAsync(string collectionName, ulong id);

    /// <summary>
    /// Attempts to start a transaction.
    /// </summary>
    void StartTransaction();

    /// <summary>
    /// Commits the active transaction.
    /// </summary>
    void CommitTransaction();

    /// <summary>
    /// Rolls back all changes in the active transaction.
    /// </summary>
    void RollbackTransaction();

    /// <summary>
    /// Checks whether the collection with the given name exists
    /// </summary>
    /// <param name="name">The name of the collection</param>
    /// <returns>Boolean indicating whether the collection exists</returns>
    Task<bool> CollectionExists(string name);
}