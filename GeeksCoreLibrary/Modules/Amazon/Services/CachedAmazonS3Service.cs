using System;
using System.IO;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Amazon.Interfaces;
using GeeksCoreLibrary.Modules.Amazon.Models;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Amazon.Services;

public class CachedAmazonS3Service : IAmazonS3Service
{
    private readonly GclSettings gclSettings;
    private readonly IAmazonS3Service amazonS3Service;

    public CachedAmazonS3Service(IOptions<GclSettings> gclSettings, IAmazonS3Service amazonS3Service)
    {
        this.gclSettings = gclSettings.Value;
        this.amazonS3Service = amazonS3Service;
    }

    /// <inheritdoc />
    public async Task<bool> CreateBucketAsync(string bucketName, AwsSettings awsSettings = null)
    {
        return await amazonS3Service.CreateBucketAsync(bucketName, awsSettings);
    }

    /// <inheritdoc />
    public async Task<bool> UploadFileAsync(string bucketName, string objectName, string filePath, AwsSettings awsSettings = null)
    {
        return await amazonS3Service.UploadFileAsync(bucketName, objectName, filePath, awsSettings);
    }

    /// <inheritdoc />
    public async Task<bool> DownloadObjectFromBucketAsync(string bucketName, string objectName, string saveDirectory, AwsSettings awsSettings = null)
    {
        // First check if local file exists.
        var fileInfo = new FileInfo(Path.Combine(saveDirectory, objectName));
        if (fileInfo.Exists && DateTime.UtcNow.Subtract(fileInfo.LastWriteTimeUtc) <= gclSettings.DefaultItemFileCacheDuration)
        {
            return true;
        }

        return await amazonS3Service.DownloadObjectFromBucketAsync(bucketName, objectName, saveDirectory, awsSettings);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteObjectAsync(string bucketName, string objectName, AwsSettings awsSettings = null)
    {
        return await amazonS3Service.DeleteObjectAsync(bucketName, objectName, awsSettings);
    }
}