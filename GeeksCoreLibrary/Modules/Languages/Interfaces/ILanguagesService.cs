using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Languages.Interfaces
{
    public interface ILanguagesService
    {
        string CurrentLanguageCode { get; set; }

        string Wiser2TranslationsGroupName { get; set; }

        /// <summary>
        /// Returns the translated word if exists in cache, if not it will return the original word
        /// </summary>
        /// <param name="original"></param>
        /// <param name="languageCode"></param>
        /// <returns></returns>
        Task<string> GetTranslationAsync(string original, string languageCode = null);

        Task<string> GetLanguageCodeAsync();
    }
}
