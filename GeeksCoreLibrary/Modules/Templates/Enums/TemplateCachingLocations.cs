namespace GeeksCoreLibrary.Modules.Templates.Enums;

/// <summary>
/// The different locations where the caching can be done.
/// </summary>
public enum TemplateCachingLocations
{
    /// <summary>
    /// Cache the template in memory. This is much faster than caching it on disk, but caching will be lost if the site is restarted and could cause high memory usage on sites with a lot of pages.
    /// </summary>
    InMemory,

    /// <summary>
    /// Cache the template on disk. This is much slower than caching it in memory, but it will not be lost if the site is restarted and will not use (much) memory.
    /// </summary>
    OnDisk
}