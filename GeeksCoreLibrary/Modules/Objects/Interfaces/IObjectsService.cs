using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Objects.Interfaces
{
    public interface IObjectsService
    {
        Task<string> FindSystemObjectByDomainNameAsync(string objectKey, string defaultResult = "", string overrideDomain = "", bool searchFromSpecificToGeneral = true, bool stripAllLowerLevelDomains = false, bool throwErrorIfEmpty = false);

        /// <summary>
        /// GetAsync a value from the cached objects
        /// </summary>
        /// <param name="key"></param>
        /// <param name="typeNumber">if not given, the object based on the key only is returned</param>
        /// <returns></returns>
        Task<string> GetObjectValueAsync(string key, int typeNumber = -1);

        Task<string> GetSystemObjectValueAsync(string key);

        /// <summary>
        /// Set an object value for the specified key and type number.
        /// </summary>
        /// <param name="key">The key to create or update.</param>
        /// <param name="value">The value to set/</param>
        /// <param name="typeNumber">The type number of the key.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <returns></returns>
        Task SetObjectValueAsync(string key, string value, int typeNumber, bool saveHistory = true);

        /// <summary>
        /// Set an object value for the specified key.
        /// </summary>
        /// <param name="key">The key to create or update.</param>
        /// <param name="value">The value to set/</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <returns></returns>
        Task SetSystemObjectValueAsync(string key, string value, bool saveHistory = true);
    }
}
