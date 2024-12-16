using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Amazon.Models;
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
        /// <param name="awsSecretsManagerSettings"></param>
        /// <returns>The updated IConfigurationBuilder with the secret added to the configuration.</returns>
        public static async Task<IConfigurationBuilder> GetAppSecretsFromAwsAsync(
            this IConfigurationBuilder builder,
            AwsSecretsManagerSettings awsSecretsManagerSettings)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            // Use the helper method to retrieve the secret content
            var secretContent = await AwsSecretsManagerHelpers.GetAppSecretsFromAwsAsync($"{awsSecretsManagerSettings.BaseDirectory}/appsettings-secrets", awsSecretsManagerSettings);

            // Add the secret's JSON content to the configuration builder
            var secretStream = new MemoryStream(Encoding.UTF8.GetBytes(secretContent));

            // Add the secret's JSON content to the configuration builder
            builder.AddJsonStream(secretStream);

            return builder;
        }
    }
}