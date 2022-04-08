using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Filter.Models
{
    internal class FilterAggregationSettingsModel
    {
        [DefaultValue(@"{summary}<br />
                        {filters}")]
        internal string TemplateFull { get; set; }

        [DefaultValue(@"<ul>
                            <b>{name}</b><br />
                            {items:Raw}
                        </ul>")]
        internal string TemplateFilterGroup { get; set; }

        [DefaultValue(@"<li><a href=""{url}"">{filtername} ({count})</a></li>")]
        internal string TemplateSingleSelectItem { get; set; }

        [DefaultValue(@"<li><a href=""{url}"">{filtername} ({count})</a></li>")]
        internal string TemplateSingleSelectItemSelected { get; set; }

        [DefaultValue(@"<input type=""checkbox"" data-url=""{url}""> {filtername} ({count})<br />")]
        internal string TemplateMultiSelectItem { get; set; }
        
        [DefaultValue(@"<input checked=""checked"" type=""checkbox"" data-url=""{url}""> {filtername} ({count})<br />")]
        internal string TemplateMultiSelectItemSelected { get; set; }
        
        [DefaultValue(@"<div id=""slider"" data-name=""{filtergroup}"" data-min=""{minValue}"" data-max=""{maxValue}"" data-currentmin=""{selectedMin}"" data-currentmax=""{selectedMax}""></div>
                        <input type=""text"" id=""minvalue"" value=""{selectedMin}"" disabled=""disabled"" />
                        <input type=""text"" id=""maxvalue"" value=""{selectedMax}"" disabled=""disabled"" />")]
        internal string TemplateSlider { get; set; }

        [DefaultValue(@"<u>Geselecteerde filters:</u> <a href=""{url}"">Wis alle</a><br /><br />
                        {items:Raw}")]
        internal string TemplateSummary { get; set; }

        [DefaultValue(@"<>{groupname}</b> (<a href=""{url}"">Wis alle van deze groep</a>)<br />
                        {selectedvalues:Raw}")]
        internal string TemplateSummaryFilterGroup { get; set; }

        [DefaultValue(@"{name} (<a href=""{url}"">Wis</a>)<br />")]
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