namespace GeeksCoreLibrary.Core.Models;

public class CacheControlRuleSettingsModel
{
    /// <summary>
    /// Gets or sets the file path that requires a specific cache-control header.
    /// </summary>
    public string FilePath { get; set; }
        
    /// <summary>
    /// Gets or sets the cache-control header value to be applied to the specified file path.
    /// </summary>
    public string HeaderValue { get; set; }
}