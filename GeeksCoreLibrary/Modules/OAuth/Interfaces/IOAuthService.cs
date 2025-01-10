using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.OAuth.Interfaces;

/// <summary>
/// A service for handling OAuth authentication.
/// This contains methods for handling the callback from an OAuth2 application.
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Receives and handles response from an OAUTH2 application that uses the Authorization Code grant type, after the user has logged in.
    /// The response should contain the authorization code, which we will save in the database so that the WTS can access it.
    /// </summary>
    /// <param name="apiName">The name of the API as it's set in the WTS OAUTH configuration.</param>
    /// <param name="code">The authentication code from the external service.</param>
    public Task<bool> HandleCallbackAsync(string apiName, string code);
}