namespace GeeksCoreLibrary.Core.Enums;

public enum StoreType
{
    /// <summary>
    /// items will be stored in wiser_item table
    /// </summary>
    Table = 0,
    /// <summary>
    /// Items will be stored in the document store as json
    /// </summary>
    DocumentStore = 1,
    /// <summary>
    /// Items will be added to the document stored but not retrieved
    /// Use a task scheduler to move the items from the document store
    /// </summary>
    Hybrid = 2,
}