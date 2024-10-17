namespace GeeksCoreLibrary.Modules.Amazon.Models;

public class AmazonS3ObjectModel
{
    /// <summary>
    /// Gets or sets the key of the object. This will be the file name in most cases.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets the bucket name that the object is stored in.
    /// </summary>
    public string BucketName { get; set; }
}