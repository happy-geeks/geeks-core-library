using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.OAuth.Interfaces;
using GeeksCoreLibrary.Modules.OAuth.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;

namespace GeeksCoreLibrary.Modules.OAuth.Services;

public class OAuthService : IOAuthService, IScopedService
{
    private readonly IObjectsService objectsService;

    public OAuthService(IObjectsService objectsService)
    {
        this.objectsService = objectsService;
    }

    /// <inheritdoc />
    public async Task HandleCallbackAsync(string apiName, string code)
    {
        await objectsService.SetSystemObjectValueAsync($"WTS_{apiName}_{Constants.AuthorizationCodeKey}", code.EncryptWithAes(), false);
    }
}