using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Languages.Middlewares
{
    public class LanguagesMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<LanguagesMiddleware> logger;
        private IHttpContextAccessor httpContextAccessor;
        private ILanguagesService languagesService;

        public LanguagesMiddleware(RequestDelegate next, ILogger<LanguagesMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context, IHttpContextAccessor httpContextAccessor, ILanguagesService languagesService)
        {
            logger.LogDebug("Invoked LanguagesMiddleware");
            
            this.httpContextAccessor = httpContextAccessor;
            this.languagesService = languagesService;

            // Only handle the setting of the language session on pages, not on images, css, js, etc.
            var regEx = new Regex(@"(\.jpe?g|\.gif|\.png|\.webp|\.svg|\.bmp|\.tif|\.ico|\.woff2?|\.css|\.js|\.webmanifest)(?:\?.*)?$");
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(context);
            if (!regEx.IsMatch(currentUrl.ToString()))
            {
                await SetLanguageSession();
            }

            await next.Invoke(context);
        }

        private async Task SetLanguageSession()
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return;
            }

            var languageCode = await languagesService.GetLanguageCodeAsync();
            session.SetString(Constants.LanguageCodeSessionKey, languageCode);
            session.SetString(Constants.LegacyLanguageCodeSessionKey, languageCode);

            logger.LogDebug($"Set language code '{languageCode}' in session '{Constants.LanguageCodeSessionKey}'.");
        }
    }
}
