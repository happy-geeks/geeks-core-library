using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Diagnostics;

using GeeksCoreLibrary.Modules.HealthChecks.Services;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services
{
    public class SystemServiceHealth : IHealthCheck
    {
        private readonly HealthCheckSettings _healthCheckSettings;
        private static PerformanceCounter _cpuCounter;

        public SystemServiceHealth(IOptions<HealthCheckSettings> healthCheckSettings)
        {
            _healthCheckSettings = healthCheckSettings.Value;
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            double cpuUsage = GetCpuUsage();
            var (currentMemoryUsage, maxMemory) = GetMemoryUsage();
            var diskSpace = GetDiskSpace();

            string memoryUsageFormatted = $"{currentMemoryUsage} / {maxMemory} GB";
            string diskSpaceFormatted = $"{Math.Round(diskSpace.UsedSpaceGB, 2)} / {Math.Round(diskSpace.TotalSizeGB, 2)} GB";


            Console.WriteLine(
                $"CPU Threshold: {_healthCheckSettings.CpuUsageThreshold}, Memory Threshold: {_healthCheckSettings.MemoryUsageThreshold}");

            cpuUsage = Math.Round(cpuUsage, 2);

            if (cpuUsage < _healthCheckSettings.CpuUsageThreshold && currentMemoryUsage < _healthCheckSettings.MemoryUsageThreshold)
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

        public double GetCpuUsage()
        {
            _cpuCounter.NextValue();
            Thread.Sleep(1000);
            return _cpuCounter.NextValue();
        }

        public DiskSpaceInfo GetDiskSpace()
        {
            var drive = DriveInfo.GetDrives()[0]; // Eerste beschikbare schijf
            double totalSizeGB = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2);
            double freeSpaceGB = Math.Round(drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0), 2);
            double usedSpaceGB = totalSizeGB - freeSpaceGB;

            return new DiskSpaceInfo
            {
                TotalSizeGB = totalSizeGB,
                FreeSpaceGB = freeSpaceGB,
                UsedSpaceGB = usedSpaceGB
            };
        }

        public (double CurrentMemoryUsage, double MaxMemory) GetMemoryUsage()
        {
            using (var process = Process.GetCurrentProcess())
            {
                double currentUsageMB = process.WorkingSet64 / (1024.0 * 1024.0);
                double maxMemoryGB =
                    Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0), 2);
                double currentUsageGB = Math.Round(currentUsageMB / 1024.0, 2);

                return (currentUsageGB, maxMemoryGB);
            }
        }
    }
}
