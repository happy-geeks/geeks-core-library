﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.Extensions;

namespace GeeksCoreLibrary.Components.Filter.Models;

/// <summary>
/// The model
/// </summary>
public class FilterGroup
{
    public enum FilterGroupType
    {
        [CmsEnum(PrettyName = "Single select")]
        SingleSelect = 5,

        [CmsEnum(PrettyName = "Multi select")]
        MultiSelect = 2,

        [CmsEnum(PrettyName = "Slider")]
        Slider = 3
    }

    private decimal selectedMinValue;
    private decimal selectedMaxValue = 1000000000;
    private string selectedValueString; // is comma separated list of selected values

    public FilterGroupType FilterType { get; set; } = FilterGroupType.MultiSelect;

    public string Name { get; set; }

    public string NameSeo { get; set; }

    public string GetParamKey()
    {
        return !String.IsNullOrEmpty(QueryString) ? QueryString : NameSeo;
    }

    public string ColumnName { get; set; }

    public bool ShowCount { get; set; } = true;

    public bool HideInSummary { get; set; }

    public Dictionary<string, FilterItem> Items { get; } = new();

    public bool IsGroupFilter { get; set; }

    public string MatchValue { get; set; }

    public string AdvancedFilter { get; set; }

    public string CustomJoin { get; set; }

    public string CustomSelect { get; set; }

    public string Group { get; set; }

    public string ConnectedEntity { get; set; }

    public string ConnectedEntityProperty { get; set; }

    public string ConnectedEntityLinkType { get; set; }

    public bool IsMultiLanguage { get; set; }

    public string QueryString { get; set; }

    public bool FilterOnSeoValue { get; set; }

    public bool SingleConnectedItem { get; set; }

    /// <summary>
    /// Gets or sets the minimum amount of filter options this group should have for it to become visible.
    /// If set to 0, the general value set in the system object 'filterminimumitemsrequired' will be used.
    /// The default value is 0.
    /// </summary>
    public int MinimumItemsRequired { get; set; }

    public SortedList<string, string> ExtraProperties { get; set; }

    public SortedList<string, string> GetAdvancedFilters
    {
        get
        {
            // For example:
            // Ja~JOIN wiser_item newfilter ON newfilter.id=package.id AND newfilter.added_on>DATE_ADD(CURDATE(), INTERVAL -14 DAY)
            // Nee~JOIN wiser_item newfilter On newfilter.id=package.id And newfilter.added_on<=DATE_ADD(CURDATE(), INTERVAL -14 DAY)

            var output = new SortedList<string, string>();
            foreach (var rule in AdvancedFilter.Replace(Environment.NewLine, "^").Split("^"))
            {
                output.Add(rule.Split('~')[0], rule.Split('~')[1]);
            }

            return output;
        }
    }

    /// <summary>
    /// Gets the minimum allowed value.
    /// </summary>
    public decimal MinValue { get; set; } = 1000000000;

    /// <summary>
    /// Gets the maximum allowed value.
    /// </summary>
    public decimal MaxValue { get; set; }

    /// <summary>
    /// Gets the selected min value. If it's less than the allowed min value, the min value is returned instead.
    /// </summary>
    public decimal SelectedMinValue => selectedMinValue < MinValue ? MinValue : selectedMinValue;

    /// <summary>
    /// Gets the selected max value. If it exceeds the allowed max value, the max value is returned instead.
    /// </summary>
    public decimal SelectedMaxValue => selectedMaxValue > MaxValue ? MaxValue : selectedMaxValue;

    public string ParentColumnName { get; set; }

    public bool ContainsOrder { get; set; } = false;

    public string Classes { get; set; } = "";

    /// <summary>
    ///  Gets or sets the index if the filter, to remember the table alias used in the JOIN in the query (such as "fi2").
    /// </summary>
    public int Index { get; set; }

    public string EntityName { get; set; } = "";

    public bool UseAggregationTable { get; set; }

    public string GroupTemplate { get; set; } = "";

    public string ItemTemplate { get; set; } = "";

    public string SelectedItemTemplate { get; set; } = "";

    /// <summary>
    ///  Adds an item to the list of items, or parses the value to a decimal to see if it's a valid value for the min or max value for a slider.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="count"></param>
    /// <param name="itemDetails"></param>
    /// <remarks></remarks>
    public void AddItem(object value, int count, SortedList<string, string> itemDetails = null)
    {
        if (FilterType == FilterGroupType.Slider)
        {
            // if the filter is a slider the value can be converted as decimal
            try
            {
                Decimal.TryParse(value.ToString()?.Replace(",", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator).Replace(".", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator), out var decimalValue);
                if (Math.Ceiling(decimalValue) > MaxValue)
                {
                    MaxValue = Math.Ceiling(decimalValue);
                }

                if (Math.Floor(decimalValue) < MinValue)
                {
                    MinValue = Math.Floor(decimalValue);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Convert to double for price slider with value {value} not valid. {ex.Message}");
            }
        }
        else if (!String.IsNullOrWhiteSpace(value.ToString()))
        {
            var valueString = value.ToString()?.Trim() ?? String.Empty;

            // value must be string
            if (!Items.Keys.Contains(valueString))
            {
                Items.Add(valueString, new FilterItem(valueString, count, itemDetails));
            }
            else
            {
                // item does exist, so un-duplicate on unique id
                Items[valueString].Count += count;
            }
        }
    }

    /// <summary>
    /// The selected value for the filter group. In case of a slider with a min and max value, the value is split with a hyphen.
    /// </summary>
    public string SelectedValueString
    {
        get => selectedValueString;
        set
        {
            if (FilterType == FilterGroupType.Slider)
            {
                if (value.Contains("-"))
                {
                    selectedMinValue = Decimal.Parse(value.Split('-')[0].Replace(",", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator).Replace(".", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                    selectedMaxValue = Decimal.Parse(value.Split('-')[1].Replace(",", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator).Replace(".", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                }
                else
                {
                    selectedMinValue = 0;
                    selectedMaxValue = Decimal.Parse(value.Replace(",", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator).Replace(".", NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                }
            }

            // ATTENTION: if HTML encoding causes problems then please solve using HTML de-encode where necessary or HTML encoding of DB values, DO NOT REMOVE THIS HTML ENCODING!
            SelectedValues = HttpUtility.HtmlEncode(value).Split(',').ToList();
            selectedValueString = HttpUtility.HtmlEncode(value);
        }
    }

    public string ReplaceExtraPropertiesInTemplate(string template)
    {
        if (ExtraProperties == null)
        {
            return template;
        }

        foreach (var (key, value) in ExtraProperties)
        {
            template = template.Replace("{" + key + "}", value);
        }

        return template;
    }

    internal void AddExtraPropertiesToList(Dictionary<string, string> list)
    {
        if (ExtraProperties == null)
        {
            return;
        }

        foreach (var (key, value) in ExtraProperties)
        {
            list[key] = value;
        }
    }

    public List<string> SelectedValues { get; set; } = [];

    public FilterGroup(string newName)
    {
        Name = newName;
        NameSeo = newName.ConvertToSeo();
    }

    public FilterGroup(string newName, string newNameSeo)
    {
        Name = newName;
        NameSeo = newNameSeo;
    }

    public FilterGroup()
    {
    }
}