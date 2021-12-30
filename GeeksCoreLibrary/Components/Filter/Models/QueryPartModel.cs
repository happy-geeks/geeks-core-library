namespace GeeksCoreLibrary.Components.Filter.Models
{
    /// <summary>
    /// A model with all query parts for replacing variables in the filter items query
    /// </summary>
    public class QueryPartModel
    {
        /// <summary>
        /// The 'select' part (start) replacement
        /// </summary>
        public string SelectPartStart { get; set; }

        /// <summary>
        /// The 'select' part (end) replacement
        /// </summary>
        public string SelectPartEnd { get; set; }

        /// <summary>
        /// The 'join' replacement
        /// </summary>
        public string JoinPart { get; set; }

        /// <summary>
        /// The 'where' replacement
        /// </summary>
        public string WherePart { get; set; }
    }
}