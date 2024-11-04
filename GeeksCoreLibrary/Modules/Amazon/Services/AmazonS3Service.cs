using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Amazon.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Amazon.Services;

public class AmazonS3Service : IAmazonS3Service, IScopedService
{
    private readonly ILogger<AmazonS3Service> logger;
    private readonly GclSettings gclSettings;
    private IAmazonS3 client;

    public AmazonS3Service(ILogger<AmazonS3Service> logger, IOptions<GclSettings> gclSettings)
    {
        this.logger = logger;
        this.gclSettings = gclSettings.Value;
    }

    /// <inheritdoc />
    public async Task<bool> CreateBucketAsync(string bucketName)
    {
        try
        {
            var request = new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true,
            };

            PrepareClient();
            var response = await client.PutBucketAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "Error creating bucket: '{Message}'", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UploadFileAsync(string bucketName, string objectName, string filePath)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectName,
            FilePath = filePath,
        };

        PrepareClient();
        var response = await client.PutObjectAsync(request);
        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            logger.LogError("Could not upload {ObjectName} to {BucketName}. Response returned status code: {StatusCode:D}", objectName, bucketName, response.HttpStatusCode);
            return false;
        }

        logger.LogInformation("Successfully uploaded {ObjectName} to {BucketName}.", objectName, bucketName);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DownloadObjectFromBucketAsync(string bucketName, string objectName, string saveDirectory)
    {
        // Create a GetObject request.
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectName,
        };

        // Issue request.
        PrepareClient();
        using var response = await client.GetObjectAsync(request);

        try
        {
            // Save object to local file.
            await response.WriteResponseStreamToFileAsync($"{saveDirectory}\\{objectName}", true, CancellationToken.None);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "Error saving {ObjectName}: {Message}", objectName, ex.Message);
            Console.WriteLine($"Error saving {objectName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Makes sure the client is prepared for use.
    /// </summary>
    private void PrepareClient()
    {
        if (client != null) return;

        var credentials = new BasicAWSCredentials(gclSettings.AwsSettings.AccessKey, gclSettings.AwsSettings.SecretKey);
        client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(gclSettings.AwsSettings.Region));
    }
}