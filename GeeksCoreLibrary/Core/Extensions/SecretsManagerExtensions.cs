using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class SecretsManagerExtensions
    {
        /// <summary>
        /// Retrieves a secret from AWS Secrets Manager and adds it to the configuration builder.
        /// Assumes the secret is stored in a JSON format and adds it directly to the configuration.
        /// Caches the secret to improve performance and reduce AWS API calls.
        /// </summary>
        /// <param name="builder">The configuration builder to which the secret will be added.</param>
        /// <param name="secretName">The name of the secret stored in AWS Secrets Manager.</param>
        /// <param name="region">The AWS region where Secrets Manager is located (defaults to "eu-central-1").</param>
        /// <returns>The updated IConfigurationBuilder with the secret added to the configuration.</returns>
        public static async Task<IConfigurationBuilder> GetAppSecretsFromAwsAsync(
            this IConfigurationBuilder builder,
            string secretName,
            string region = "eu-central-1")
        {
            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

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

                // Add the secret's JSON content to the configuration
                var secretStream = new MemoryStream(Encoding.UTF8.GetBytes(secretResponse.SecretString));
                builder.AddJsonStream(secretStream);

                return builder;
            }
            catch (Exception ex)
            {
                // Log the error and provide context for easier debugging
                throw new InvalidOperationException(
                    $"Failed to retrieve or parse secret '{secretName}' from AWS Secrets Manager: {ex.Message}", ex);
            }
        }
    }
}