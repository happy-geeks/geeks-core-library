namespace GeeksCoreLibrary.Modules.Languages.Models;

/// <summary>
/// A model for a language of Wiser.
/// </summary>
public class LanguageModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Gets or sets the name or title.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the 2-letter language code.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets the code to use for href lang tags in HTML.
    /// </summary>
    public string HrefLangCode { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default language for the application/website.
    /// </summary>
    public bool IsDefaultLanguage { get; set; }
}