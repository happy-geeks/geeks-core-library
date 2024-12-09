using System;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services;
public class DatabaseHealthService : IHealthCheck
{
    private readonly IDatabaseConnection databaseConnection;
    private readonly GclSettings gclSettings;

    public DatabaseHealthService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings)
    {
        this.databaseConnection = databaseConnection;
        this.gclSettings = gclSettings.Value;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        // A new query to check the max active connections from the DatabaseHealthCheck.
        var query = "SELECT COUNT(*) AS active_connections FROM information_schema.PROCESSLIST;";
        var datatable = await databaseConnection.GetAsync(query);

        var healthCheckConnections = gclSettings.HealthChecks.MaximumDatabaseConnections;
        var healthCheckConnectionsTime = gclSettings.HealthChecks.MaximumConnectionsInTime;

        // If no value is set, we are skipping this test.
        if (healthCheckConnections > 0)
        {
            // Retrieve the count of active connections.
            var activeConnections = Convert.ToInt32(datatable.Rows[0]["active_connections"]);

            // Check if the number of active connections exceeds the limit.
            if (activeConnections > healthCheckConnections)
            {
                return HealthCheckResult.Unhealthy($"Too many database connections. There are currently {activeConnections} connections active, which is more than the threshold {healthCheckConnections} that is set in the appSettings.");
            }
        }

        // If no value is set, we are skipping this test.
        if (healthCheckConnectionsTime > 0)
        {
            // Query to check the max open connections in time from the DatabaseHealthCheck.
            query = "SELECT ID, USER, HOST, TIME AS connection_time_in_sec FROM information_schema.PROCESSLIST WHERE USER != 'repluser' AND HOST != 'localhost' ORDER BY connection_time_in_sec DESC";
            datatable = await databaseConnection.GetAsync(query);
            if (datatable.Rows.Count == 0)
            {
                // If the query returns no results, it means that there are no open connections.
                // But we can connect to the database, otherwise we would get an exception. So this means the database is healthy.
                return HealthCheckResult.Healthy();
            }

            // Retrieve the count of open connections in time.
            var connectionTime = Convert.ToInt32(datatable.Rows[0]["connection_time_in_sec"]);

            // Check if the time from open connections exceeds the limit.
            if (connectionTime > healthCheckConnectionsTime)
            {
                return HealthCheckResult.Unhealthy($"There is at least one opened connection that has been open for more than {healthCheckConnectionsTime} seconds. There most likely is an issue with this connection.");
            }
        }

        return HealthCheckResult.Healthy();
    }
}