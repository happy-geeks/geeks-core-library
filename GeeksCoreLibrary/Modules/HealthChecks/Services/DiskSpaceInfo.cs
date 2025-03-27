
namespace GeeksCoreLibrary.Modules.HealthChecks.Services
{
    public class DiskSpaceInfo
    {
        public double TotalSizeGB { get; set; }
        public double FreeSpaceGB { get; set; }
        public double UsedSpaceGB { get; set; }
    }
}