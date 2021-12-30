using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.Filter.Models
{
    public class FilterItem
    {
        public FilterItem(string value, int count, SortedList<string, string> itemDetails = null)
        {
            Value = value;
            Count = count;
            ItemDetails = itemDetails;
        }

        public string Value { get; set; }

        public string ValueSEO { get; set; }

        public int Count { get; set; }

        public SortedList<string, string> ItemDetails { get; set; }
    }
}
