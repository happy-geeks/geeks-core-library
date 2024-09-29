using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Extensions.Caching;
using Microsoft.Extensions.Configuration;

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class SecretsManagerExtensions
    {
        /// <summary>
        /// Retrieves a secret from AWS Secrets Manager and adds it to the configuration builder.
        /// It assumes the secret is stored in a JSON format and adds it directly to the configuration.
        /// The secret is retrieved asynchronously and added as a configuration source to the provided IConfigurationBuilder.
        /// </summary>
        /// <param name="builder">The configuration builder to which the secret will be added.</param>
        /// <param name="secretName">The name of the secret (or app-setting) stored in AWS Secrets Manager. Defaults to the assembly name + launch environment.</param>
        /// <param name="region">The AWS region where the Secrets Manager is located. Defaults to "eu-central-1".</param>
        /// <returns>Returns the updated IConfigurationBuilder with the secret added to the configuration.</returns>
        public static async Task<IConfigurationBuilder> AddAppSettingsFromAwsAsync(
            this IConfigurationBuilder builder,
            string secretName = null,
            string region = "eu-central-1")
        {
            // If no secret name is provided, construct a default one using assembly information
            if (String.IsNullOrEmpty(secretName))
            {
                var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                if (String.IsNullOrEmpty(assemblyName))
                {
                    throw new Exception("Assembly name could not be determined.");
                }

                if (String.IsNullOrEmpty(environment))
                {
                    throw new Exception("Environment could not be determined.");
                }

                // Construct the default secret name based on assembly name and environment
                secretName = $"{assemblyName}/{environment}";
            }

            // Create an AmazonSecretsManager client with the specified region
            var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            // Use the Secrets Manager Cache to cache secrets
            var cache = new SecretsManagerCache(client);

            // Retrieve the secret value from the cache
            string secretValue;
            try
            {
                secretValue = await cache.GetSecretString(secretName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve secret: {ex.Message}", ex);
            }

            if (string.IsNullOrEmpty(secretValue))
            {
                throw new Exception($"Secret {secretName} is empty or not in a string format.");
            }

            // Add the secret to the configuration as a JSON
            builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(secretValue)));

            return builder;
        }
    }
}