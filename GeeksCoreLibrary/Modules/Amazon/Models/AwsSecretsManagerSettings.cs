namespace GeeksCoreLibrary.Modules.Amazon.Models;

public class AwsSecretsManagerSettings
{
    /// <summary>
    /// Gets or sets the base directory from which secrets will get retrieved.
    /// If you want to use AWS secrets manager, this value is always required.
    /// </summary>
    public string BaseDirectory { get; set; }

    /// <summary>
    /// Gets or sets the access key. This can be left empty if you use the AWS CLI or when the application is running on an EC2 instance on AWS.
    /// </summary>
    public string AccessKey { get; set; }

    /// <summary>
    /// Gets or sets the secret key. This can be left empty if you use the AWS CLI or when the application is running on an EC2 instance on AWS.
    /// </summary>
    public string SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the region. This can be left empty if you use the AWS CLI or when the application is running on an EC2 instance on AWS.
    /// For a list of available regions, see <see href="https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.RegionsAndAvailabilityZones.html#Concepts.RegionsAndAvailabilityZones.Regions">AWS Regions</see>.
    /// </summary>
    public string Region { get; set; }
}