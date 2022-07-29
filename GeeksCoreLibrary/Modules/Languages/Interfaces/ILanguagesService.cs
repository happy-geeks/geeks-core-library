using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Languages.Models;

namespace GeeksCoreLibrary.Modules.Languages.Interfaces
{
    public interface ILanguagesService
    {
        /// <summary>
        /// Gets or sets the current language code.
        /// </summary>
        string CurrentLanguageCode { get; set; }

        /// <summary>
        /// Returns the translated word if exists in cache, if not it will return the original word
        /// </summary>
        /// <param name="original">The value to translate.</param>
        /// <param name="languageCode">Optional: The language code of the language to translate to. If <see langword="null"/>, the current language of the site will be used.</param>
        /// <param name="defaultValue">Optional: The value to return if no translation can be found. If <see langword="null"/>, the original input will be returned.</param>
        /// <returns>The translated string, or the original string if no translation has been found.</returns>
        Task<string> GetTranslationAsync(string original, string languageCode = null, string defaultValue = null);

        /// <summary>
        /// Gets the current language code from domain / httpContext.
        /// </summary>
        /// <returns>The language code.</returns>
        Task<string> GetLanguageCodeAsync();

        /// <summary>
        /// Gets all languages that are configured in Wiser.
        /// </summary>
        /// <returns>A list of <see cref="LanguageModel"/> with all configured languages.</returns>
        Task<List<LanguageModel>> GetAllLanguagesAsync();
    }
}
