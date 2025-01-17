using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Amazon.Interfaces;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Amazon.Services;

public class AmazonSecretsManagerService(IOptions<GclSettings> gclSettings) : IAmazonSecretsManagerService, IScopedService
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    /// <inheritdoc />
    public async Task<string> GetSshPrivateKeyFromAwsSecretsManagerAsync()
    {
        return await AwsSecretsManagerHelpers.GetAppSecretsFromAwsAsync($"{gclSettings.AwsSecretsManagerSettings.BaseDirectory}/rds-proxy-private-key", gclSettings.AwsSecretsManagerSettings);
    }
}