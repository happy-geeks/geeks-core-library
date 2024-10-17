using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.OAuth;

public interface IOAuthService
{
    /// <summary>
    /// Receives and handles response from google after user has logged in. Response typically contains access token. This token is saved to the db so that the WTS can use it later.
    /// </summary>
    /// <returns></returns>
    public Task<int> HandleCallbackAsync(string code);
}