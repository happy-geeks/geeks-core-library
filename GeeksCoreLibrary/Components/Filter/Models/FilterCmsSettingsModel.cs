using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.Filter.Models;

public class FilterCmsSettingsModel : CmsSettings
{
    public Filter.ComponentModes ComponentMode { get; set; } = Filter.ComponentModes.Aggregation;

    #region Tab Layout properties

    /// <summary>
    /// The complete template that contains the filters and the summary.
    /// </summary>
    [CmsProperty(
        PrettyName = "Complete template",
        Description = "The complete template that contains the filters and the summary.",
        DeveloperRemarks = @"Use {summary} to place the filter summary and {filters} to place the filters.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 10
    )]
    public string TemplateFull { get; set; }

    /// <summary>
    /// The template for a filter group.
    /// </summary>
    [CmsProperty(
        PrettyName = "Filter group template",
        Description = "The template for a filter group.",
        DeveloperRemarks = @"Use {items:Raw} to place the filter items. Use {name} or {name_cf} for the name of the filter.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 20
    )]
    public string TemplateFilterGroup { get; set; }

    /// <summary>
    /// The template for a slider filter group.
    /// </summary>
    [CmsProperty(
        PrettyName = "Slider filter group template",
        Description = "The template for a slider filter group.",
        DeveloperRemarks = @"Use {items:Raw} to place the slider filter. The normal filter group template will be used if this one is left empty. Use {name} or {name_cf} for the name of the filter. Use {querystring} for the name of the querystring.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 30
    )]
    public string TemplateFilterGroupSlider { get; set; }

    /// <summary>
    /// The template for a filter item for single select filters.
    /// </summary>
    [CmsProperty(
        PrettyName = "Single select filter item template",
        Description = "The template for a filter item for single select filters.",
        DeveloperRemarks = @"Possible variables: {name}, {url}, {count}, {group}.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 40
    )]
    public string TemplateSingleSelectItem { get; set; }

    /// <summary>
    /// The template for a selected filter item for single select filters.
    /// </summary>
    [CmsProperty(
        PrettyName = "Selected single select filter item template",
        Description = "The template for a selected filter item for single select filters.",
        DeveloperRemarks = @"Possible variables: {name}, {url}, {count}, {group}.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 50
    )]
    public string TemplateSingleSelectItemSelected { get; set; }

    /// <summary>
    /// The template for a filter item for multi select filters.
    /// </summary>
    [CmsProperty(
        PrettyName = "Multi select filter item template",
        Description = "The template for a filter item for multi select filters.",
        DeveloperRemarks = @"Possible variables: {name}, {url}, {count}, {group}.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 60
    )]
    public string TemplateMultiSelectItem { get; set; }

    /// <summary>
    /// The template for a selected filter item for multi select filters.
    /// </summary>
    [CmsProperty(
        PrettyName = "Selected multi select filter item template",
        Description = "The template for a selected filter item for multi select filters.",
        DeveloperRemarks = @"Possible variables: {name}, {url}, {count}, {group}.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 70
    )]
    public string TemplateMultiSelectItemSelected { get; set; }

    /// <summary>
    /// The template of a slider filter.
    /// </summary>
    [CmsProperty(
        PrettyName = "Slider template",
        Description = "The template of a slider filter.",
        DeveloperRemarks = @"Possible variables: {minValue}, {maxValue}, {selectedMin}, {selectedMax}. Use {filterName} for the item-title of the filter and user {filterNameSeo} for the detail 'filtername' of the filter.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 80
    )]
    public string TemplateSlider { get; set; }

    /// <summary>
    /// The complete template for the summary of the selected filters.
    /// </summary>
    [CmsProperty(
        PrettyName = "Summary complete template",
        Description = "The complete template for the summary of the selected filters.",
        DeveloperRemarks = @"Possible variables: {url} (to reset all selections), {items:Raw}.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 90
    )]
    public string TemplateSummary { get; set; }

    /// <summary>
    /// The template for a filter group's summary.
    /// </summary>
    [CmsProperty(
        PrettyName = "Summary filter group template",
        Description = "The template for a filter group's summary.",
        DeveloperRemarks = @"Possible variables: {groupname}, {url} (to reset all selections of the group), {selectedvalues}.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 100
    )]
    public string TemplateSummaryFilterGroup { get; set; }

    /// <summary>
    /// The template for an item in a filter group's summary.
    /// </summary>
    [CmsProperty(
        PrettyName = "Summary filter group item template",
        Description = "The template for an item in a filter group's summary.",
        DeveloperRemarks = @"Possible variables: {name}, {group}, {groupname}, {url}.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 110
    )]
    public string TemplateSummaryFilterGroupItem { get; set; }

    #endregion

    #region Tab Datasource properties

    /// <summary>
    /// Query to get the category (or other parent) id, based on request values.
    /// </summary>
    [CmsProperty(
        PrettyName = "Filter category id query",
        Description = "Query to get the category (or other parent) id, based on request values.",
        DeveloperRemarks = @"Select one column and one row, containing the id",
        TabName = CmsAttributes.CmsTabName.DataSource,
        GroupName = CmsAttributes.CmsGroupName.CustomSql,
        TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
        DisplayOrder = 10
    )]
    public string FilterCategoryIdQuery { get; set; }

    /// <summary>
    /// Query to generate the filters. Query must select all filter-items of all filter-groups.
    /// </summary>
    [CmsProperty(
        PrettyName = "Filter items query",
        Description = "Query to generate the filters. Query must select all filter-items of all filter-groups.",
        DeveloperRemarks = @"<p>Place the variable {filters} after the last JOIN to exclude values when filtered. Also place the variable {filtersWhere} on the position where the 'where part' should be inserted. Use same aliasses as in the product overview query. Also use alias 'filterName' for wiser_itemdetail table with detail 'filtername'.</p>
                                <p>Query must contain the following columns:</p>
                                <ul>
                                    <li>filtergroup (mandatory) - The name (seo friendly syntax) of the filter group to which the item belongs</li>
                                    <li>filtervalue (mandatory) - The value (seo friendly syntax) on which the customer filters.</li>
                                    <li>count (optional) - The total number of items which matches to the filter value.</li>
                                    <li>itemdetail_... (multiple / optional) - Extra information for use in the template, like the normal name of an item of an image.</li>                                    
                                </ul>",
        TabName = CmsAttributes.CmsTabName.DataSource,
        GroupName = CmsAttributes.CmsGroupName.CustomSql,
        TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
        DisplayOrder = 20
    )]
    public string FilterItemsQuery { get; set; }

    /// <summary>
    /// Give komma separated the property names of the extra properties (on filter level) to get. These properties can be used in the group templates.
    /// </summary>
    [CmsProperty(
        PrettyName = "Extra filter properties",
        Description = "Give komma separated the property names of the extra properties (on filter level) to get. These properties can be used in the group templates.",
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.DataSource,
        GroupName = CmsAttributes.CmsGroupName.DataHandling,
        TextEditorType = CmsAttributes.CmsTextEditorType.TextBox,
        DisplayOrder = 30
    )]
    public string ExtraFilterProperties { get; set; }

    #endregion

    #region Tab Behavior properties

    /// <summary>
    /// The name of the query-string which contains the search value.
    /// </summary>
    [CmsProperty(
        PrettyName = "Search querystring name",
        Description = "The name of the query-string which contains the search value",
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Behavior,
        GroupName = CmsAttributes.CmsGroupName.Search,
        TextEditorType = CmsAttributes.CmsTextEditorType.TextBox,
        DisplayOrder = 10
    )]
    public string SearchQuerystring { get; set; }

    /// <summary>
    /// The keys of the detail(s) on which the search function should be performed
    /// </summary>
    [CmsProperty(
        PrettyName = "Search keys",
        Description = "The name of the query-string which contains the search value",
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Behavior,
        GroupName = CmsAttributes.CmsGroupName.Search,
        TextEditorType = CmsAttributes.CmsTextEditorType.TextBox,
        DisplayOrder = 20
    )]
    public string SearchKeys { get; set; }


    #endregion
}