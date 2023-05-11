namespace GeeksCoreLibrary.Modules.Payments.Enums.Buckaroo;

/// <summary>
/// The content type of the push request from Buckaroo.
/// </summary>
public enum PushContentTypes
{
    /// <summary>
    /// The push request is in JSON format.
    /// </summary>
    Json = 1,
    /// <summary>
    /// The push request is comprised of form data.
    /// </summary>
    HttpPost = 2
}