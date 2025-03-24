using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using GeeksCoreLibrary.Modules.HealthChecks.Services;  // Ensure this namespace is correct

namespace GeeksCoreLibrary.Modules.HealthChecks.Services
{
    public class SystemServiceHealth : IHealthCheck
    {
        private readonly HealthCheckSettings _healthCheckSettings;
        private readonly DiskSpaceInfo _diskSpaceInfo;

        public SystemServiceHealth(IOptions<HealthCheckSettings> healthCheckSettings, DiskSpaceInfo diskSpaceInfo)
        {
            _healthCheckSettings = healthCheckSettings.Value;
            _diskSpaceInfo = diskSpaceInfo;
        }

        private static DateTime _lastSampleTime = DateTime.MinValue;
        private static TimeSpan _lastProcessorTime = TimeSpan.Zero;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            double cpuUsage = GetCpuUsage();
            double memoryUsage = GetMemoryUsage();

            double totalSizeGB = _diskSpaceInfo.TotalSizeGB;
            double freeSpaceGB = _diskSpaceInfo.FreeSpaceGB;

            if (cpuUsage < _healthCheckSettings.CpuUsageThreshold && memoryUsage < _healthCheckSettings.MemoryUsageThreshold)
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
                DateTime currentTime = DateTime.UtcNow;

                if (_lastSampleTime == DateTime.MinValue)
                {
                    _lastSampleTime = currentTime;
                    _lastProcessorTime = process.TotalProcessorTime;
                    return 0;
                }

                TimeSpan timeDifference = currentTime - _lastSampleTime;
                TimeSpan processorTimeDifference = process.TotalProcessorTime - _lastProcessorTime;

                double cpuUsage = (processorTimeDifference.TotalMilliseconds / timeDifference.TotalMilliseconds) * 100;
                cpuUsage = Math.Min(cpuUsage, 100);

                _lastSampleTime = currentTime;
                _lastProcessorTime = process.TotalProcessorTime;

                return cpuUsage;
            }
        }

        public double GetMemoryUsage()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return process.WorkingSet64 / (1024.0 * 1024.0); // Convert to MB
            }
        }
    }
}
