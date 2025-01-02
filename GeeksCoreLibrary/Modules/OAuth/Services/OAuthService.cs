using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.OAuth.Interfaces;
using GeeksCoreLibrary.Modules.OAuth.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;

namespace GeeksCoreLibrary.Modules.OAuth.Services;

/// <inheritdoc cref="IOAuthService" />
public class OAuthService : IOAuthService, IScopedService
{
    private readonly IObjectsService objectsService;

    /// <summary>
    /// Creates a new instance of <see cref="OAuthService"/>.
    /// </summary>
    public OAuthService(IObjectsService objectsService)
    {
        this.objectsService = objectsService;
    }

    /// <inheritdoc />
    public async Task HandleCallbackAsync(string apiName, string code)
    {
        // Save the authorization code, so that the WTS can use it to get the access token.
        await objectsService.SetSystemObjectValueAsync($"WTS_{apiName}_{Constants.AuthorizationCodeKey}", code.EncryptWithAes(), false);

        // Set the mail sent flag to false, so that the next time that this authorization code expires, the WTS will send the mail again.
        await objectsService.SetSystemObjectValueAsync($"WTS_{apiName}_{Constants.AuthorizationCodeMailSentKey}", "false", false);

        // Remove the access token, refresh token, and expire time, so that the WTS will get a new access token.
        await objectsService.SetSystemObjectValueAsync($"WTS_{apiName}_{Constants.AccessTokenKey}", "", false);
        await objectsService.SetSystemObjectValueAsync($"WTS_{apiName}_{Constants.RefreshTokenKey}", "", false);
        await objectsService.SetSystemObjectValueAsync($"WTS_{apiName}_{Constants.ExpireTimeKey}", "", false);
    }
}