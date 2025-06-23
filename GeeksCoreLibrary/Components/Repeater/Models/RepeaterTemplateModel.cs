using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.Repeater.Models;

public class RepeaterTemplateModel
{
    /// <summary>
    /// This template will be rendered once every time at the start of rendering this layer.
    /// </summary>
    [CmsProperty(
        PrettyName = "Header template",
        Description = "This template will be rendered once every time at the start of rendering this layer.",
        DeveloperRemarks = "",
        DisplayOrder = 10,
        ComponentMode = "NonLegacy",
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
    )]
    public string HeaderTemplate { get; set; } = "";

    /// <summary>
    /// This template will be rendered for every item/row of this layer.
    /// </summary>
    [CmsProperty(
        PrettyName = "Item template",
        Description = "This template will be rendered for every item/row of this layer.",
        DeveloperRemarks = "You can use the {subLayer} placeholder to render the next layer of this item on that location. If you don't use this placeholder, the next layer will be rendered right after the ItemTemplate.",
        DisplayOrder = 20,
        ComponentMode = "NonLegacy",
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
    )]
    public string ItemTemplate { get; set; } = "";

    [CmsProperty(
        PrettyName = "Selected item template",
        HideInCms = true
    )]
    public string SelectedItemTemplate { get; set; } = "";

    /// <summary>
    /// This template will be rendered if the data source contains no data for this layer.
    /// </summary>
    [CmsProperty(
        PrettyName = "No data template",
        Description = "This template will be rendered if the data source contains no data for this layer.",
        DeveloperRemarks = "",
        DisplayOrder = 30,
        ComponentMode = "NonLegacy",
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
    )]
    public string NoDataTemplate { get; set; } = "";

    /// <summary>
    /// This template will be rendered in between every 2 items/rows of this layer.
    /// </summary>
    [CmsProperty(
        PrettyName = "Between items template",
        Description = "This template will be rendered in between every 2 items/rows of this layer.",
        DeveloperRemarks = "",
        DisplayOrder = 40,
        ComponentMode = "NonLegacy",
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
    )]
    public string BetweenItemsTemplate { get; set; } = "";

    /// <summary>
    /// This template will be rendered once every time at the end of rendering this layer.
    /// </summary>
    [CmsProperty(
        PrettyName = "Footer template",
        Description = "This template will be rendered once every time at the end of rendering this layer.",
        DeveloperRemarks = "",
        DisplayOrder = 50,
        ComponentMode = "NonLegacy",
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
    )]
    public string FooterTemplate { get; set; } = "";
}