using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Amazon.Models;

namespace GeeksCoreLibrary.Modules.Amazon.Interfaces;

public interface IAmazonS3Service
{
    /// <summary>
    /// Create a new Amazon S3 bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to create.</param>
    /// <param name="awsSettings">Optional: The settings for the Amazon Web Services account. If not given then the settings in the app settings are used.</param>
    /// <returns>A boolean value representing the success or failure of the bucket creation process.</returns>
    Task<bool> CreateBucketAsync(string bucketName, AwsSettings awsSettings = null);

    /// <summary>
    /// Upload a file from the local computer to an Amazon S3 bucket.
    /// </summary>
    /// <param name="bucketName">The Amazon S3 bucket to which the object will be uploaded.</param>
    /// <param name="objectName">The object to upload.</param>
    /// <param name="filePath">The path, including file name, of the object on the local computer to upload.</param>
    /// <param name="awsSettings">Optional: The settings for the Amazon Web Services account. If not given then the settings in the app settings are used.</param>
    /// <returns>A boolean value indicating the success or failure of the upload procedure.</returns>
    Task<bool> UploadFileAsync(string bucketName, string objectName, string filePath, AwsSettings awsSettings = null);

    /// <summary>
    /// Download an object from an Amazon S3 bucket to the local computer.
    /// </summary>
    /// <param name="bucketName">The name of the bucket where the object is currently stored.</param>
    /// <param name="objectName">The name of the object to download.</param>
    /// <param name="saveDirectory">The path, excluding filename, where the downloaded object will be stored.</param>
    /// <param name="awsSettings">Optional: The settings for the Amazon Web Services account. If not given then the settings in the app settings are used.</param>
    /// <returns>A boolean value indicating the success or failure of the download process.</returns>
    Task<bool> DownloadObjectFromBucketAsync(string bucketName, string objectName, string saveDirectory, AwsSettings awsSettings = null);

    /// <summary>
    /// Deletes an object from an Amazon S3 bucket to the local computer.
    /// </summary>
    /// <param name="bucketName">The name of the bucket where the object is currently stored.</param>
    /// <param name="objectName">The name of the object to delete.</param>
    /// <param name="awsSettings">Optional: The settings for the Amazon Web Services account. If not given then the settings in the app settings are used.</param>
    /// <returns>A boolean value indicating the success or failure of the deletion process.</returns>
    Task<bool> DeleteObjectAsync(string bucketName, string objectName, AwsSettings awsSettings = null);
}