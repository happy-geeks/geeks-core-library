using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.OAuth.Interfaces;

public interface IOAuthService
{
    /// <summary>
    /// Receives and handles response from google after user has logged in. Response typically contains access token. This token is saved to the db so that the WTS can use it later.
    /// </summary>
    /// <param name="apiName">The name of the API as it's set in the WTS OAUTH configuration.</param>
    /// <param name="code">The authentication code from the external service.</param>
    public Task HandleCallbackAsync(string apiName, string code);
}