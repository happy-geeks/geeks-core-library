using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Filter.Models
{
    internal class FilterAggregationSettingsModel
    {
        [DefaultValue(Constants.TemplateFull)]
        internal string TemplateFull { get; set; }

        [DefaultValue(Constants.TemplateFilterGroup)]
        internal string TemplateFilterGroup { get; set; }

        [DefaultValue(Constants.TemplateSingleSelectItem)]
        internal string TemplateSingleSelectItem { get; set; }

        [DefaultValue(Constants.TemplateSingleSelectItemSelected)]
        internal string TemplateSingleSelectItemSelected { get; set; }

        [DefaultValue(Constants.TemplateMultiSelectItem)]
        internal string TemplateMultiSelectItem { get; set; }
        
        [DefaultValue(Constants.TemplateMultiSelectItemSelected)]
        internal string TemplateMultiSelectItemSelected { get; set; }
        
        [DefaultValue(Constants.TemplateSlider)]
        internal string TemplateSlider { get; set; }

        [DefaultValue(Constants.TemplateSummary)]
        internal string TemplateSummary { get; set; }

        [DefaultValue(Constants.TemplateSummaryFilterGroup)]
        internal string TemplateSummaryFilterGroup { get; set; }

        [DefaultValue(Constants.TemplateSummaryFilterGroupItem)]
        internal string TemplateSummaryFilterGroupItem { get; set; }

        [DefaultValue(@"SELECT f.*, COUNT(f.product_id) AS count
                         FROM `wiser_filter_aggregation_{languageCode}` f
                         {filters}
                         WHERE f.category_id={categoryId} {filterGroup}
                         GROUP BY f.filtergroup,f.filtervalue
                         ORDER BY f.filtergroup,f.filtervalue")] 
        internal  string FilterItemsQuery { get; set; }

    }
}