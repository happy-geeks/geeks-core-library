namespace GeeksCoreLibrary.Modules.Amazon.Models;

public class AwsSettings
{
    /// <summary>
    /// Gets or sets the access key.
    /// </summary>
    public string AccessKey { get; set; }

    /// <summary>
    /// Gets or sets the secret key.
    /// </summary>
    public string SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the region.
    /// For a list of available regions, see <see href="https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.RegionsAndAvailabilityZones.html#Concepts.RegionsAndAvailabilityZones.Regions">AWS Regions</see>.
    /// </summary>
    public string Region { get; set; }
}