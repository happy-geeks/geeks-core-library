using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class ConfigurationsModel
    {
        /// <summary>
        /// Gets or sets the name of the configurator.
        /// </summary>
        [JsonProperty("configurator")]
        public string Configurator { get; set; }

        /// <summary>
        /// Gets or sets the URL of the image.
        /// </summary>
        [JsonProperty("image")]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the amount of times the user wants to order this configuration.
        /// </summary>
        [JsonProperty("quantity")]
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Gets or sets all items/steps. The keys are either the index or the ID of the item (depending on which model was sent via javascript).
        /// </summary>
        /// 
        [JsonProperty("items")]
        public Dictionary<string, StepsModel> Items { get; set; }

        /// <summary>
        /// Gets or sets Custom variables
        /// </summary>
        /// 
        [JsonProperty("customValues")]
        public Dictionary<string, string> CustomValues { get; set; } = new Dictionary<string, string>();

        [JsonProperty("qsItems")]
        public Dictionary<string, string> QueryStringItems { get; set; } = new Dictionary<string, string>();
    }
}
