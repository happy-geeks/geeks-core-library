using System.Text;

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
        public StringBuilder SelectPartStart { get; set; } = new();

        /// <summary>
        /// The 'select' part (end) replacement
        /// </summary>
        public StringBuilder SelectPartEnd { get; set; } = new();

        /// <summary>
        /// The 'join' replacement
        /// </summary>
        public StringBuilder JoinPart { get; set; } = new();

        /// <summary>
        /// The 'where' replacement
        /// </summary>
        public StringBuilder WherePart { get; set; } = new();
    }
}