using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.OAuth.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.OAuth.Controllers;

[Area("OAuth")]
[Route("oauth")]
public class OAuthController(IOAuthService oAuthService) : Controller
{
    [HttpGet("handle-callback")]
    public async Task<IActionResult> HandleCallbackAsync(string apiName, string code)
    {
        if (String.IsNullOrWhiteSpace(code) || String.IsNullOrWhiteSpace(apiName))
        {
            return View("AuthorizationFailed");
        }

        var result = await oAuthService.HandleCallbackAsync(apiName, code);
        return View(result ? "AuthorizationSuccessful" : "AuthorizationFailed");
    }
}