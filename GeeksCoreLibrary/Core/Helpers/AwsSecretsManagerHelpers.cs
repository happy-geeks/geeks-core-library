using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using GeeksCoreLibrary.Modules.Amazon.Models;

namespace GeeksCoreLibrary.Core.Helpers;

public static class AwsSecretsManagerHelpers
{
    /// <summary>
    /// Get a specific secret from AWS Secrets Manager. This works with credentials from the appsettings.json file, the AWS CLI or in applications that run within AWS ECR.<br/>
    /// Note: This method does not cache the secret. It is recommended to cache the secret in the application to reduce the number of AWS API calls, if this is called from other places than the Startup/Program class.<br/>
    /// Note 2: This is a static method and not a service, because this needs to be called when the application starts, before our services are registered with the DI container.
    /// </summary>
    /// <param name="secretName"></param>
    /// <param name="awsSecretsManagerSettings"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<string> GetAppSecretsFromAwsAsync(string secretName, AwsSecretsManagerSettings awsSecretsManagerSettings)
    {
        if (String.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.");
        }

        // Determine AWS credentials
        AWSCredentials credentials;

        if (!String.IsNullOrWhiteSpace(awsSecretsManagerSettings?.AccessKey) && !String.IsNullOrWhiteSpace(awsSecretsManagerSettings.SecretKey))
        {
            // Use the provided accessKey and secretKey.
            credentials = new BasicAWSCredentials(awsSecretsManagerSettings.AccessKey, awsSecretsManagerSettings.SecretKey);
        }
        else
        {
            var chain = new CredentialProfileStoreChain();
            if (!chain.TryGetAWSCredentials(awsSecretsManagerSettings?.ProfileName ?? "default", out credentials))
            {
                // Fallback to default credentials from ~/.aws/credentials or environment variables.
                credentials = FallbackCredentialsFactory.GetCredentials();
            }
        }

        // Determine the AWS region
        var region = !String.IsNullOrWhiteSpace(awsSecretsManagerSettings?.Region)
            ? RegionEndpoint.GetBySystemName(awsSecretsManagerSettings.Region)
            : FallbackRegionFactory.GetRegionEndpoint() ?? RegionEndpoint.EUCentral1;

        // Create the AWS Secrets Manager client with custom credentials
        using var client = new AmazonSecretsManagerClient(credentials, region);

        try
        {
            // Retrieve the secret from AWS Secrets Manager
            var secretResponse = await client.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            });

            // Check if the secret has a valid string value
            if (String.IsNullOrEmpty(secretResponse.SecretString))
            {
                throw new InvalidOperationException($"The secret '{secretName}' does not contain a string value.");
            }

            return secretResponse.SecretString;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to retrieve or parse secret '{secretName}' from AWS Secrets Manager: {exception.Message}", exception);
        }
    }
}