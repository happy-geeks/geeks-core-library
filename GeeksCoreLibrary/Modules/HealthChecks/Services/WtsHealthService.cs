using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services;

public class WtsHealthService : IHealthCheck
{
    private readonly IDatabaseConnection databaseConnection;
    
    public WtsHealthService(IDatabaseConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }
    
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return await Task.FromResult(HealthCheckResult.Healthy());
    }
}