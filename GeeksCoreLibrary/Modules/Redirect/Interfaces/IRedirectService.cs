using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Redirect.Models;

namespace GeeksCoreLibrary.Modules.Redirect.Interfaces
{
    public interface IRedirectService
    {
        /// <summary>
        /// Gets redirect data for specific page / URL.
        /// </summary>
        /// <param name="uri">The URI of the page which must me redirected.</param>
        /// <returns>A <see cref="RedirectModel"/> with the redirect data.</returns>
        Task<RedirectModel> GetRedirectAsync(Uri uri);

        /// <summary>
        /// Gets whether or not the Redirect module is enabled in the settings.
        /// </summary>
        /// <returns>A <see langword="bool"/> indicating whether or not the Redirect module is enabled.</returns>
        Task<bool> RedirectModuleIsEnabledAsync();

        /// <summary>
        /// Checks if the redirect to main domain is enabled and gives back the main domain if redirect is enabled
        /// Only works on live environment
        /// </summary>
        /// <returns></returns>
        Task<string> GetMainDomainForRedirectAsync();

        /// <summary>
        /// Check if URL's must end on slash 
        /// </summary>
        /// <returns></returns>
        Task<bool> ShouldRedirectToUrlWithTrailingSlashAsync();

        /// <summary>
        /// Check if URL's only may contain lower case characters
        /// </summary>
        /// <returns></returns>
        Task<bool> ShouldRedirectToLowerCaseUrlAsync();

        /// <summary>
        /// Check if URL must be https
        /// Only works on live environment
        /// </summary>
        /// <returns></returns>
        Task<bool> ShouldRedirectToHttpsAsync();
    }
}