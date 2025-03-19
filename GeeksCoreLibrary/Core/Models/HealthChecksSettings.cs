namespace GeeksCoreLibrary.Core.Models;

public class HealthChecksSettings
{
    public bool DatabaseHealthCheckEnabled { get; set; } = true;
    public int MaximumDatabaseConnections { get; set; }
    public int MaximumConnectionsInTime { get; set; }
}