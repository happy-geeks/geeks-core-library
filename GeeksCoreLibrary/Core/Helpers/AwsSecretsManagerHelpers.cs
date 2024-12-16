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
        if (String.IsNullOrWhiteSpace(secretName) || !ModelValidationHelpers.IsValid(awsSecretsManagerSettings))
        {
            throw new ArgumentException("Secret name, access key, secret key, and region cannot be null, empty, or whitespace.");
        }

        // Set up AWS credentials using the provided accessKey and secretKey
        var credentials = new BasicAWSCredentials(awsSecretsManagerSettings.AccessKey, awsSecretsManagerSettings.SecretKey);

        // Create the AWS Secrets Manager client with custom credentials
        var client = new AmazonSecretsManagerClient(credentials, RegionEndpoint.GetBySystemName(awsSecretsManagerSettings.Region));

        try
        {
            // Retrieve the secret from AWS Secrets Manager
            var secretResponse = await client.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            });

            // Check if the secret has a valid string value
            if (string.IsNullOrEmpty(secretResponse.SecretString))
            {
                throw new InvalidOperationException($"The secret '{secretName}' does not contain a string value.");
            }

            return secretResponse.SecretString;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve or parse secret '{secretName}' from AWS Secrets Manager: {ex.Message}", ex);
        }
    }
}