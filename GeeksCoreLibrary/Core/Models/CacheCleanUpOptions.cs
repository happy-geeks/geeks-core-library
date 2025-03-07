namespace GeeksCoreLibrary.Core.Models;

public class CacheCleanUpOptions
{
    /// <summary>
    /// The interval, in days, at which the cleanup process should run.
    /// </summary>
    public int CleanUpIntervalDays { get; set; }

    /// <summary>
    /// The maximum age, in days, a cached item can be before it is deleted.
    /// </summary>
    public int MaxCacheDurationDays { get; set; }
}