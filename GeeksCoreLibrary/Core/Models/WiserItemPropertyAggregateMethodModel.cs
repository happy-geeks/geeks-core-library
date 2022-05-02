using GeeksCoreLibrary.Core.Enums;

namespace GeeksCoreLibrary.Core.Models
{
    public class WiserItemPropertyAggregateMethodModel
    {
        /// <summary>
        /// Gets or sets the method of aggregation, such as Sum, Min, Max etc.
        /// </summary>
        public WiserItemPropertyAggregateMethods Method { get; set; } = WiserItemPropertyAggregateMethods.None;

        /// <summary>
        /// Gets or sets the link type that is used to link this item to it's parent.
        /// This is required when using an aggregation method, because we then need to find all other children of the same parent with the same link type to be able to do the calculation.
        /// </summary>
        public int ParentLinkType { get; set; } = 1;
    }
}