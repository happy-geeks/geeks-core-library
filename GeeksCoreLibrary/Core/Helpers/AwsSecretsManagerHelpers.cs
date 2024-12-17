using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using GeeksCoreLibrary.Modules.Amazon.Models;

namespace GeeksCoreLibrary.Core.Helpers;

public static class AwsSecretsManagerHelpers
{
    public static async Task<string> GetAppSecretsFromAwsAsync(string secretName, AwsSecretsManagerSettings awsSecretsManagerSettings)
    {
        if (String.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.");
        }

        // Set up AWS credentials using the provided accessKey and secretKey
        // Determine AWS credentials
        AWSCredentials credentials;

        if (!String.IsNullOrWhiteSpace(awsSecretsManagerSettings?.AccessKey) && !String.IsNullOrWhiteSpace(awsSecretsManagerSettings.SecretKey))
        {
            // Use the provided accessKey and secretKey.
            credentials = new BasicAWSCredentials(awsSecretsManagerSettings.AccessKey, awsSecretsManagerSettings.SecretKey);
        }
        else
        {
            // Fallback to default credentials from ~/.aws/credentials or environment variables.
            credentials = FallbackCredentialsFactory.GetCredentials();
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