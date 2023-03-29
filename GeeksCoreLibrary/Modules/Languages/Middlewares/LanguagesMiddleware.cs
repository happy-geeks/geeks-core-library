using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Languages.Middlewares
{
    public class LanguagesMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<LanguagesMiddleware> logger;
        private ILanguagesService languagesService;

        public LanguagesMiddleware(RequestDelegate next, ILogger<LanguagesMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context, ILanguagesService languagesService)
        {
            logger.LogDebug("Invoked LanguagesMiddleware");

            this.languagesService = languagesService;
            await SetLanguageSession(context);
            await next.Invoke(context);
        }

        private async Task SetLanguageSession(HttpContext context)
        {
            var session = context?.Session;
            if (session == null)
            {
                return;
            }

            var languageCode = await languagesService.GetLanguageCodeAsync();
            session.SetString(Constants.LanguageCodeSessionKey, languageCode);
            session.SetString(Constants.LegacyLanguageCodeSessionKey, languageCode);

            logger.LogDebug("Set language code '{languageCode}' in session '{sessionName}'.", languageCode, Constants.LanguageCodeSessionKey);
        }
    }
}
