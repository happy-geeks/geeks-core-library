using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Amazon.Interfaces;

public interface IAmazonS3Service
{
    /// <summary>
    /// Shows how to create a new Amazon S3 bucket.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to create.</param>
    /// <returns>A boolean value representing the success or failure of the bucket creation process.</returns>
    Task<bool> CreateBucketAsync(string bucketName);

    /// <summary>
    /// Shows how to upload a file from the local computer to an Amazon S3 bucket.
    /// </summary>
    /// <param name="bucketName">The Amazon S3 bucket to which the object will be uploaded.</param>
    /// <param name="objectName">The object to upload.</param>
    /// <param name="filePath">The path, including file name, of the object on the local computer to upload.</param>
    /// <returns>A boolean value indicating the success or failure of the upload procedure.</returns>
    Task<bool> UploadFileAsync(string bucketName, string objectName, string filePath);

    /// <summary>
    /// Shows how to download an object from an Amazon S3 bucket to the local computer.
    /// </summary>
    /// <param name="bucketName">The name of the bucket where the object is currently stored.</param>
    /// <param name="objectName">The name of the object to download.</param>
    /// <param name="filePath">The path, including filename, where the downloaded object will be stored.</param>
    /// <returns>A boolean value indicating the success or failure of the download process.</returns>
    Task<bool> DownloadObjectFromBucketAsync(string bucketName, string objectName, string filePath);
}