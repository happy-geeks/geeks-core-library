using System;

namespace GeeksCoreLibrary.Core.Models;

public class CacheCleanUpOptions
{
    /// <summary>
    /// The interval at which the cleanup process should run.
    /// </summary>
    public TimeSpan CleanUpInterval { get; set; }

    /// <summary>
    /// The maximum duration a cached item can exist before it is deleted.
    /// </summary>
    public TimeSpan MaxCacheDuration { get; set; }
}