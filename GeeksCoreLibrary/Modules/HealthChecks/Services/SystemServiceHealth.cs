using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.HealthChecks.Models;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services;

/// <summary>
/// A health check for the system's CPU, memory, and disk space.
/// </summary>
/// <param name="healthCheckSettings"></param>
public class SystemServiceHealth(IOptions<HealthCheckSettings> healthCheckSettings) : IHealthCheck, IDisposable
{
    private readonly HealthCheckSettings healthCheckSettings = healthCheckSettings.Value;

    private static PerformanceCounter cpuCounter;

    /// <summary>
    /// Checks overall system health by evaluating CPU usage, memory usage, and disk space.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that can be used to cancel the health check.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that completes when the health check has finished, yielding the status of the component being checked.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cpuUsage = OperatingSystem.IsWindows()
            ? await GetWindowsCpuUsageAsync(cancellationToken)
            : await GetCrossPlatformCpuUsageAsync(cancellationToken);
        var (currentMemoryUsage, totalAvailableMemory) = GetMemoryUsage();
        var diskSpace = GetDiskSpace();

        var memoryUsagePercentage = (currentMemoryUsage / totalAvailableMemory) * 100;
        cpuUsage = Math.Round(cpuUsage, 2);

        var memoryUsageFormatted = $"{NumberHelpers.FormatByteSize(currentMemoryUsage)} / {NumberHelpers.FormatByteSize(totalAvailableMemory)}";
        var diskSpaceFormatted = $"{diskSpace.FormattedUsedSpace} / {diskSpace.FormattedTotalSize}";

        var healthCheckMessage = $"CPU Usage: {cpuUsage}%, Memory: {memoryUsageFormatted}, Disk Space: {diskSpaceFormatted}";
        var isHealthy = cpuUsage < healthCheckSettings.CpuUsageThreshold && memoryUsagePercentage < healthCheckSettings.MemoryUsageThreshold;

        return isHealthy
            ? HealthCheckResult.Healthy(healthCheckMessage)
            : HealthCheckResult.Unhealthy(healthCheckMessage);
    }

    /// <summary>
    /// Cross-platform CPU usage approximation (used on non-Windows systems (Linux/MacOS).
    /// </summary>
    /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that can be used to cancel the health check.</param>
    /// <returns>The current CPU usage of the system.</returns>
    [UnsupportedOSPlatform("windows")]
    private static async Task<double> GetCrossPlatformCpuUsageAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        var processorCount = Environment.ProcessorCount;

        await Task.Delay(1000, cancellationToken);

        var endTime = DateTime.UtcNow;
        var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;

        var cpuUsageTotal = (cpuUsedMs / (totalMsPassed * processorCount)) * 100;
        return Math.Round(cpuUsageTotal, 2);
    }

    /// <summary>
    /// Gets CPU usage percentage for the current system using platform-specific methods.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that can be used to cancel the health check.</param>
    /// <returns>The current CPU usage of the system.</returns>
    [SupportedOSPlatform("windows")]
    private static async Task<double> GetWindowsCpuUsageAsync(CancellationToken cancellationToken)
    {
        cpuCounter ??= new PerformanceCounter("Processor", "% Processor Time", "_Total");

        cpuCounter.NextValue();
        await Task.Delay(1000, cancellationToken);
        return Math.Round(cpuCounter.NextValue(), 2);
    }

    /// <summary>
    /// Gets disk space usage info for the OS drive.
    /// </summary>
    /// <returns>Information about the current disk space usage.</returns>
    private static DiskSpaceModel GetDiskSpace()
    {
        var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name == Path.GetPathRoot(Environment.SystemDirectory));

        if (drive == null)
        {
            return new DiskSpaceModel { TotalSize = 0, AvailableSpace = 0, UsedSpace = 0 };
        }

        return new DiskSpaceModel
        {
            TotalSize = drive.TotalSize,
            AvailableSpace = drive.TotalFreeSpace,
            UsedSpace = drive.TotalSize - drive.TotalFreeSpace
        };
    }

    /// <summary>
    /// Gets current and maximum memory usage in GB.
    /// </summary>
    /// <returns>Information about the current memory usage.</returns>
    private static (double CurrentMemoryUsage, double TotalAvailableMemory) GetMemoryUsage()
    {
        var memoryInformation = GC.GetGCMemoryInfo();
        return (memoryInformation.MemoryLoadBytes, memoryInformation.TotalAvailableMemoryBytes);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (OperatingSystem.IsWindows())
        {
            cpuCounter?.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}