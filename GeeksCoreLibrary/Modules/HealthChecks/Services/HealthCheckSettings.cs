namespace GeeksCoreLibrary.Modules.HealthChecks.Services
{
    public class HealthCheckSettings
    {
        public int CpuUsageThreshold { get; set; } = 90; 
        public int MemoryUsageThreshold { get; set; } = 90; 
    }
}