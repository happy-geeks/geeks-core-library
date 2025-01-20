using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.DataSelector.Models;

/// <summary>
/// Class that represents the query string variables for a get_items request.
/// </summary>
public class DataSelectorRequestModel
{
    /// <summary>
    /// Gets or sets the settings for the data selector.
    /// </summary>
    public DataSelector Settings { get; set; }

    /// <summary>
    /// Gets or sets the ID of the data selector.
    /// </summary>
    public int? DataSelectorId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the query.
    /// </summary>
    public string QueryId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the module that this data selector belongs to.
    /// </summary>
    public string ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the number of levels.
    /// </summary>
    public int? NumberOfLevels { get; set; }

    /// <summary>
    /// Gets or sets if there are descendants.
    /// </summary>
    public bool? Descendants { get; set; }

    /// <summary>
    /// Gets or sets the language code.
    /// </summary>
    public string LanguageCode { get; set; }

    /// <summary>
    /// Gets or set the number of items.
    /// </summary>
    public string NumberOfItems { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [JsonProperty("pagenr")]
    public int? PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the contains path.
    /// </summary>
    [JsonProperty("path")]
    public string ContainsPath { get; set; }

    /// <summary>
    /// Gets or sets the contains url.
    /// </summary>
    public string ContainsUrl { get; set; }

    /// <summary>
    /// Gets or sets the ID of the parent.
    /// </summary>
    public string ParentId { get; set; }

    /// <summary>
    /// Gets or sets the entity types.
    /// </summary>
    public string EntityTypes { get; set; }

    /// <summary>
    /// Gets or sets the link type.
    /// </summary>
    public int? LinkType { get; set; }

    /// <summary>
    /// Gets or sets the query addition.
    /// </summary>
    public string QueryAddition { get; set; }

    /// <summary>
    /// Gets or sets the order part.
    /// </summary>
    public string OrderPart { get; set; }

    /// <summary>
    /// Gets or sets the environment.
    /// </summary>
    public int? Environment { get; set; }

    /// <summary>
    /// Gets or sets the fields.
    /// </summary>
    public string Fields { get; set; }

    /// <summary>
    /// Gets or sets the file types.
    /// </summary>
    public string FileTypes { get; set; }

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the extra data.
    /// </summary>
    public object ExtraData { get; set; }

    /// <summary>
    /// Gets or sets the template for the output.
    /// </summary>
    public string OutputTemplate { get; set; }

    /// <summary>
    /// Gets or sets the ID for the content item.
    /// </summary>
    public string ContentItemId { get; set; }

    /// <summary>
    /// Gets or sets the content property name.
    /// </summary>
    public string ContentPropertyName { get; set; }

    /// <summary>
    /// Gets or sets whether the result should be returned as an Excel document.
    /// </summary>
    public bool? ToExcel { get; set; }

    #region For validation purposes

    public string Hash { get; set; }

    public string DateTime { get; set; }

    #endregion
}