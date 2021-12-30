using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Filter.Models
{
    internal class FilterNormalSettingsModel
    {
        [DefaultValue(@"{summary}<br />
                        {filters}")]
        internal string TemplateFull { get; set; }

        [DefaultValue(@"<ul>
                            <b>{name}</b><br />
                            {items}
                        </ul>")]
        internal string TemplateFilterGroup { get; set; }

        [DefaultValue(@"<li><a href=""{url}"">{name} ({count})</a></li>")]
        internal string TemplateSingleSelectItem { get; set; }

        [DefaultValue(@"<li><a href=""{url}"">{name} ({count})</a></li>")]
        internal string TemplateSingleSelectItemSelected { get; set; }

        [DefaultValue(@"<input type=""checkbox"" data-url=""{url}""> {name} ({count})<br />")]
        internal string TemplateMultiSelectItem { get; set; }
        
        [DefaultValue(@"<input checked=""checked"" type=""checkbox"" data-url=""{url}""> {name} ({count})<br />")]
        internal string TemplateMultiSelectItemSelected { get; set; }
        
        [DefaultValue(@"<div id=""slider"" data-name=""{filterNameSeo}"" data-min=""{minValue}"" data-max=""{maxValue}"" data-currentmin=""{selectedMin}"" data-currentmax=""{selectedMax}""></div>
                        <input type=""text"" id=""minvalue"" value=""{selectedMin}"" disabled=""disabled"" />
                        <input type=""text"" id=""maxvalue"" value=""{selectedMax}"" disabled=""disabled"" />")]
        internal string TemplateSlider { get; set; }

        [DefaultValue(@"<u>Geselecteerde filters:</u> <a href=""{url}"">Wis alle</a><br /><br />
                        {items}")]
        internal string TemplateSummary { get; set; }

        [DefaultValue(@"<b>{groupname}</b> (<a href=""{url}"">Wis alle van deze groep</a>)<br />
                        {selectedvalues}")]
        internal string TemplateSummaryFilterGroup { get; set; }

        [DefaultValue(@"{name} (<a href=""{url}"">Wis</a>)<br />")]
        internal string TemplateSummaryFilterGroupItem { get; set; }

        [DefaultValue(@"SELECT 
    IFNULL(queryString.`value`, filters.title) AS filtergroupseo,
    IFNULL(productPropertyValueNameSeo.`value`, IFNULL(productPropertyValueName.`value`, productPropertyValue.title)) AS filteritem,    
    IFNULL(productPropertyValueName.`value`, productPropertyValue.title) AS itemdetail_name,
    filterCategoryLink.ordering AS ordering
  	
#Get all filters for category
FROM wiser_item filters
JOIN wiser_itemlink filterCategoryLink ON filterCategoryLink.item_id=filters.id AND filterCategoryLink.destination_item_id={categoryId} AND filterCategoryLink.type = 6001
LEFT JOIN wiser_itemdetail queryString ON queryString.item_id=filters.id AND queryString.`key`='querystring'

JOIN wiser_item AS i ON i.entity_type = 'product' AND i.published_environment >= 4
JOIN wiser_itemlink AS productCategoryLink ON productCategoryLink.destination_item_id={categoryId} AND productCategoryLink.type=1

#If all Property-Values are linked to the products via itemlink
LEFT JOIN wiser_itemlink AS propertyLink ON propertyLink.destination_item_id = i.id AND propertyLink.type >= 800 AND propertyLink.type  <= 806
LEFT JOIN wiser_item AS productPropertyValue ON productPropertyValue.id = propertyLink.item_id AND productPropertyValue.entity_type = 'waarde'
LEFT JOIN wiser_itemdetail AS productPropertyValueName ON productPropertyValueName.item_id = productPropertyValue.id AND productPropertyValueName.`key` = 'name' AND productPropertyValueName.language_code = '{languageCode}'
LEFT JOIN wiser_itemdetail AS productPropertyValueNameSeo ON productPropertyValueNameSeo.item_id = productPropertyValue.id AND productPropertyValueNameSeo.`key` = 'name_SEO' AND productPropertyValueNameSeo.language_code = '{languageCode}'

#If a Property-Value is linked to a Product-Type
LEFT JOIN wiser_itemlink AS productPropertyTypeLink ON productPropertyTypeLink.item_id = productPropertyValue.id AND productPropertyTypeLink.type = 1
LEFT JOIN wiser_item AS productPropertyType ON productPropertyType.id = productPropertyTypeLink.destination_item_id AND productPropertyType.entity_type = 'eigenschap'

{filters}

WHERE filters.entity_type='filter'

{filtersWhere}

GROUP BY filtergroupseo,filteritem
HAVING filteritem IS NOT NULL 
ORDER BY ordering, filteritem")] 
        internal  string FilterItemsQuery { get; set; }

    }
}