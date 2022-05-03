using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Databases.Models;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Core.Models
{
    public class WiserItemPropertyAggregateOptionsModel
    {
        /// <summary>
        /// Gets or sets the name of the property/field these settings belong to.
        /// </summary>
        [JsonIgnore]
        public string PropertyName { get; set; }
        
        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        [JsonIgnore]
        public string LanguageCode { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the table that contains the aggregated values.
        /// If this contains no value, the name "aggregate_[entityName]" will be used.
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the column where the values of this field should be saved.
        /// If this contains no value, the property name will be used as column name.
        /// </summary>
        public string ColumnName { get; set; }
        
        /// <summary>
        /// Gets or sets the column settings where the values of this field should be saved.
        /// </summary>
        [JsonIgnore]
        public ColumnSettingsModel ColumnSettings { get; set; }

        /// <summary>
        /// Gets or sets the aggregation methods for this field/property.
        /// This can be used for executing functions on values, such as calculating the sum of a field of all other children with the same parent.
        /// </summary>
        public List<WiserItemPropertyAggregateMethodModel> AggregationMethods { get; set; } = new();
    }
}