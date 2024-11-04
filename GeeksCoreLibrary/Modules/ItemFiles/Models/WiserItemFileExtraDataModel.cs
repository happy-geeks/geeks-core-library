using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Amazon.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GeeksCoreLibrary.Modules.ItemFiles.Models;

/// <summary>
/// A model for storing extra data for a file, such as alt texts in multiple languages for images.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class WiserItemFileExtraDataModel
{
    /// <summary>
    /// Gets or sets the alt texts for images in multiple languages.
    /// The key is the language code and the value is the alt text for that language.
    /// </summary>
    public Dictionary<string, string> AltTexts { get; set; }

    /// <summary>
    /// Gets or sets additional keys and values that are not directly part of the model.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JToken> AdditionalData { get; set; }
}