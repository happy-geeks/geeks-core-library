using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.Filter.Models;

public class FilterItem(string value, int count, SortedList<string, string> itemDetails = null)
{
    public string Value { get; set; } = value;

    public string ValueSEO { get; set; }

    public int Count { get; set; } = count;

    public SortedList<string, string> ItemDetails { get; set; } = itemDetails;
}