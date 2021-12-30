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
    }
}
