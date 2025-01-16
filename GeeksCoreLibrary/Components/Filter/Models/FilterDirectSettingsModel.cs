using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Filter.Models;

internal class FilterDirectSettingsModel
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

    [DefaultValue("""
                  SELECT 
                      IFNULL(queryString.`value`, filters.title) AS filtergroup,
                      IFNULL(productPropertyValueNameSeo.`value`, IFNULL(productPropertyValueName.`value`, productPropertyValue.title)) AS filtervalue,    
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

                  GROUP BY filtergroup,filtervalue
                  HAVING filtervalue IS NOT NULL 
                  ORDER BY ordering, filtervalue
                  """)]
    internal string FilterItemsQuery { get; set; }
}