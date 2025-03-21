using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using GeeksCoreLibrary.Modules.HealthChecks.Services;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services
{
    public class SystemServiceHealth : IHealthCheck
    {
     
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
        
            double cpuUsage = GetCpuUsage();
            double memoryUsage = GetMemoryUsage();
            var diskSpace = GetDiskSpace(); 

            // Extract de waarden uit DiskSpaceInfo
            double totalSizeGB = diskSpace.TotalSizeGB;
            double freeSpaceGB = diskSpace.FreeSpaceGB;

           
            if (cpuUsage < 90 && memoryUsage < 90)  // Bijvoorbeeld, als CPU en geheugen onder de 90% blijven
            {
                return HealthCheckResult.Healthy($"CPU Usage: {cpuUsage}%, Memory Usage: {memoryUsage}MB, Disk Space: {totalSizeGB}GB total, {freeSpaceGB}GB free");
            }
            else
            {
                return HealthCheckResult.Unhealthy($"CPU Usage: {cpuUsage}%, Memory Usage: {memoryUsage}MB, Disk Space: {totalSizeGB}GB total, {freeSpaceGB}GB free");
            }
        }

      
        public double GetCpuUsage()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount;
            }
        }

      
        public double GetMemoryUsage()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return process.WorkingSet64 / (1024.0 * 1024.0); // in MB
            }
        }

        // Bepaal schijfruimte
        public DiskSpaceInfo GetDiskSpace()
        {
            var drive = DriveInfo.GetDrives()[0]; 
            return new DiskSpaceInfo
            {
                TotalSizeGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0), // GB
                FreeSpaceGB = drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0) // GB
            };
        }
    }
}
