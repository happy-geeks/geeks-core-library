using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.OAuth.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.OAuth.Controllers;

[Area("OAuth")]
[Route("oauth")]
public class OAuthController : Controller
{
    private readonly IOAuthService oAuthService;

    public OAuthController(IOAuthService oAuthService)
    {
        this.oAuthService = oAuthService;
    }

    [HttpGet("handle-callback")]
    public async Task<IActionResult> HandleCallbackAsync(string apiName, string code)
    {
        if (String.IsNullOrWhiteSpace(code) || String.IsNullOrWhiteSpace(apiName))
        {
            return View("AuthorizationFailed");
        }

        await oAuthService.HandleCallbackAsync(apiName, code);
        return View("AuthorizationSuccessful");
    }
}