namespace GeeksCoreLibrary.Modules.DataSelector.Models
{
    public class DataSelector
    {
        public MainConnection Main { get; set; }

        public Connection[] Connections { get; set; }

        public string QueryAddition { get; set; }

        public string[] GroupBy { get; set; }

        public Having[] Having { get; set; }

        public OrderBy[] OrderBy { get; set; }

        /// <summary>
        /// Gets or sets how many items will be retrieved. The limit is set in a style similar to MySQL, so either a single number, or an offset and number separated by comma.
        /// </summary>
        public string Limit { get; set; }

        /// <summary>
        /// Gets or sets whether it's allowed to load this data selector unsecured. Only applies to data selectors that are loaded via ID.
        /// </summary>
        public bool Insecure { get; set; }
    }
}
