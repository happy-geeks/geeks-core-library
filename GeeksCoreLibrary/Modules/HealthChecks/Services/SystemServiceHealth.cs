using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;
using JetBrains.Annotations;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services
{
    public class SystemServiceHealth : IHealthCheck, IDisposable
    {
        private readonly HealthCheckSettings _healthCheckSettings;
        
         [CanBeNull] private static PerformanceCounter _cpuCounter;
        
        public SystemServiceHealth(IOptions<HealthCheckSettings> healthCheckSettings)
        {
            if (healthCheckSettings == null) throw new ArgumentNullException(nameof(healthCheckSettings));
            _healthCheckSettings = healthCheckSettings.Value;
        }
       
        // Checks overall system health by evaluating CPU usage, memory usage, and disk space.
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            double cpuUsage = OperatingSystem.IsWindows() ? await GetCpuUsageAsync(cancellationToken) : await GetCrossPlatformCpuUsageAsync(cancellationToken);
            var (currentMemoryUsage, maxMemory) = GetMemoryUsage();
            var diskSpace = GetDiskSpace();

            double memoryUsagePercentage = (currentMemoryUsage / maxMemory) * 100;
            cpuUsage = Math.Round(cpuUsage, 2);

            string memoryUsageFormatted = $"{Math.Round(currentMemoryUsage, 2)} / {Math.Round(maxMemory, 2)} GB";
            string diskSpaceFormatted = $"{Math.Round(diskSpace.UsedSpaceGB, 2)} / {Math.Round(diskSpace.TotalSizeGB, 2)} GB";

            string healthCheckMessage = $"CPU Usage: {cpuUsage}%, Memory: {memoryUsageFormatted}, Disk Space: {diskSpaceFormatted}";
            bool isHealthy = cpuUsage < _healthCheckSettings.CpuUsageThreshold && memoryUsagePercentage < _healthCheckSettings.MemoryUsageThreshold;

            return isHealthy
                ? HealthCheckResult.Healthy(healthCheckMessage)
                : HealthCheckResult.Unhealthy(healthCheckMessage);
        }

        // Cross-platform CPU usage approximation (used on non-Windows systems (Linux/MacOS).
        [UnsupportedOSPlatform("windows")]
        private async Task<double> GetCrossPlatformCpuUsageAsync(CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            int processorCount = Environment.ProcessorCount;

            await Task.Delay(1000, cancellationToken);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

            double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            double totalMsPassed = (endTime - startTime).TotalMilliseconds;

            double cpuUsageTotal = (cpuUsedMs / (totalMsPassed * processorCount)) * 100;
            return Math.Round(cpuUsageTotal, 2);
        }
        
        // Gets CPU usage percentage for the current system using platform-specific methods.
        [SupportedOSPlatform("windows")]
        private async Task<double> GetCpuUsageAsync(CancellationToken cancellationToken)
        {
            if (_cpuCounter == null)
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
            
            _cpuCounter.NextValue(); // Warm-up
            await Task.Delay(1000, cancellationToken);
            return Math.Round(_cpuCounter.NextValue(), 2);
        }
        
        // Gets disk space usage info for the OS drive.
        private DiskSpaceInfo GetDiskSpace()
        {
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.IsReady && d.Name == Path.GetPathRoot(Environment.SystemDirectory));

            if (drive == null)
            {
                return new DiskSpaceInfo { TotalSizeGB = 0, FreeSpaceGB = 0, UsedSpaceGB = 0 };
            }

            double totalSizeGb = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
            double freeSpaceGb = drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0);
            double usedSpaceGb = totalSizeGb - freeSpaceGb;

            return new DiskSpaceInfo
            {
                TotalSizeGB = totalSizeGb,
                FreeSpaceGB = freeSpaceGb,
                UsedSpaceGB = usedSpaceGb
            };
        }
    
        // Gets current and maximum memory usage in GB.
        private (double CurrentMemoryUsage, double MaxMemory) GetMemoryUsage()
        {
            using var process = Process.GetCurrentProcess();

            double currentUsageGb = process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0);
            double maxMemoryGb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0);

            return (currentUsageGb, maxMemoryGb);
        }

        // Disposes the CPU counter to release system resources.
        public void Dispose()
        {
            if (OperatingSystem.IsWindows())
            {
                _cpuCounter?.Dispose();
            }
        }
    }
}
