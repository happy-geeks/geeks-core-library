using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Amazon.Interfaces;

public interface IAmazonSecretsManagerService
{
    /// <summary>
    /// Gets the SSH private key from the project based on the baseDirectory set in the GCL AWS Secrets Manager settings.
    /// </summary>
    /// <returns>A string value representing the contents of the private key</returns>
    Task<string> GetSshPrivateKeyFromAwsSecretsManagerAsync();
}