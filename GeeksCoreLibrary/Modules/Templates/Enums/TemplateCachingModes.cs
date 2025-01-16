namespace GeeksCoreLibrary.Modules.Templates.Enums;

/// <summary>
/// The different ways a template can be cached.
/// </summary>
public enum TemplateCachingModes
{
    /// <summary>
    /// Template will not be cached and will always be rendered on-the-fly.
    /// </summary>
    NoCaching = 0,
    /// <summary>
    /// Template will be cached regardless of URL.
    /// </summary>
    ServerSideCaching = 2,
    /// <summary>
    /// Template will be cached based on the URL, excluding the query string.
    /// </summary>
    ServerSideCachingPerUrl = 3,
    /// <summary>
    /// Template will be cached based on the URL, including the query string.
    /// </summary>
    ServerSideCachingPerUrlAndQueryString = 4,
    /// <summary>
    /// Template will be cached based on the full URL, including domain and the query string.
    /// </summary>
    ServerSideCachingPerHostNameAndQueryString = 5,
    /// <summary>
    /// Template will be cached based on the cache regex. Every named group in this regex will be added to the cache key.
    /// </summary>
    ServerSideCachingBasedOnUrlRegex = 6
}