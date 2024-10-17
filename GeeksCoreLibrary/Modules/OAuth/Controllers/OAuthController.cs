using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.OAuth;

[Area("OAuth")]
[Route("oauth")]

public class OAuthController : Controller
{
    private readonly IOAuthService oAuthService;
    
    public OAuthController(IOAuthService oAuthService)
    {
        this.oAuthService = oAuthService;
    }

    [Route("handle-callback")]
    [HttpGet]
    public async Task<IActionResult> HandleCallbackAsync(string code)
    {
        var success = await oAuthService.HandleCallbackAsync(code);
        return View(success == 1 ? "AuthorizationSuccessful" : "AuthorizationFailed");
    }
}

