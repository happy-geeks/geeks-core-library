using System.Collections.Generic;

namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// A model for storing extra data for a file, such as alt texts in multiple languages for images.
/// </summary>
public class WiserItemFileExtraDataModel
{
    /// <summary>
    /// Gets or sets the alt texts for images in multiple languages.
    /// The key is the language code and the value is the alt text for that language.
    /// </summary>
    public Dictionary<string, string> AltTexts { get; set; }
}