using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services
{
    public class SystemServiceHealth : IHealthCheck, IDisposable
    {
        private readonly HealthCheckSettings _healthCheckSettings;
        private static PerformanceCounter? _cpuCounter;
        private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public SystemServiceHealth(IOptions<HealthCheckSettings> healthCheckSettings)
        {
            _healthCheckSettings = healthCheckSettings.Value;
            // Uses CPU counter only if the platform is Windows.
            if (_isWindows)
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            double cpuUsage = await MeasureCpuUsageWithDelayAsync();
            var (currentMemoryUsage, maxMemory) = GetMemoryUsage();
            var diskSpace = GetDiskSpace();

            string memoryUsageFormatted = $"{currentMemoryUsage} / {maxMemory} GB";
            string diskSpaceFormatted = $"{Math.Round(diskSpace.UsedSpaceGB, 2)} / {Math.Round(diskSpace.TotalSizeGB, 2)} GB";

            // Calculate memory usage percentage
            double memoryUsagePercentage = (currentMemoryUsage / maxMemory) * 100;

            cpuUsage = Math.Round(cpuUsage, 2);

            // Now comparing cpuUsage with the threshold and memory usage percentage with the memory threshold
            if (cpuUsage < _healthCheckSettings.CpuUsageThreshold && memoryUsagePercentage < _healthCheckSettings.MemoryUsageThreshold)
            {
                return HealthCheckResult.Healthy(
                    $"CPU Usage: {cpuUsage}%, Memory: {memoryUsageFormatted}, Disk Space: {diskSpaceFormatted}");
            }
            else
            {
                return HealthCheckResult.Unhealthy(
                    $"CPU Usage: {cpuUsage}%, Memory: {memoryUsageFormatted}, Disk Space: {diskSpaceFormatted}");
            }
        }

        /// <summary>
        /// Measures CPU usage over a short period (1 second).
        /// This method requires an initial NextValue call, followed by a delay, to get an accurate reading.
        /// The delay is necessary because PerformanceCounter CPU usage readings rely on measuring usage over time.
        /// </summary>
        public async Task<double> MeasureCpuUsageWithDelayAsync()
        {
            if (!_isWindows || _cpuCounter == null)
            {
                return 0;
            }
            _cpuCounter.NextValue();
            await Task.Delay(1000); // Required delay for accurate CPU measurement
            return _cpuCounter.NextValue();
        }
        /// Gets the disk space of the OS drive.
        public DiskSpaceInfo GetDiskSpace()
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.IsReady && drive.Name == Path.GetPathRoot(Environment.SystemDirectory)) // Ensure it's the OS drive
                {
                    double totalSizeGB = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2);
                    double freeSpaceGB = Math.Round(drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0), 2);
                    double usedSpaceGB = totalSizeGB - freeSpaceGB;
                    return new DiskSpaceInfo { TotalSizeGB = totalSizeGB, FreeSpaceGB = freeSpaceGB, UsedSpaceGB = usedSpaceGB };
                }
            }
            return new DiskSpaceInfo { TotalSizeGB = 0, FreeSpaceGB = 0, UsedSpaceGB = 0 };
        }
        /// Gets the current memory usage and the maximum available memory.
        public (double CurrentMemoryUsage, double MaxMemory) GetMemoryUsage()
        {
            using (var process = Process.GetCurrentProcess())
            {
                double currentUsageGB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0), 2); // Direct calculation of GB
                double maxMemoryGB = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0), 2);

                return (currentUsageGB, maxMemoryGB);
            }
        }
        // Dispose of the CPU counter to release resources.
        public void Dispose()
        {
            _cpuCounter?.Dispose();
        }
    }
}
