using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
    
namespace GeeksCoreLibrary.Modules.HealthChecks.Services;

public class DatabaseHealthService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings)
    : IHealthCheck
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var messages = new List<string>(); 
        var isHealthy = true; 

        try
        {
            // ✅ Check if database service is available
            if (databaseConnection == null)
            {
                return HealthCheckResult.Unhealthy("Database connection service is not available.");
            }

            // ✅ Ensure health check is enabled
            if (gclSettings.HealthChecks?.DatabaseHealthCheckEnabled != true)
            {
                return HealthCheckResult.Unhealthy("Database health check settings are missing or misconfigured.");
            }

            //  Basic Database Connectivity Check
            var connectionTestQuery = "SELECT 1;";
            var connectionTestTable = await databaseConnection.GetAsync(connectionTestQuery);
            if (connectionTestTable.Rows.Count == 0)
            {
                isHealthy = false;
                messages.Add("⚠ Database is unreachable.");
            }
            else
            {
                messages.Add("✅ Database is reachable.");
                messages.Add("✅ Database is healthy and running.");
            }

            // ✅ Database Version Check
            var versionQuery = "SELECT VERSION();";
            var versionTable = await databaseConnection.GetAsync(versionQuery);
            var dbVersion = versionTable.Rows[0][0]?.ToString();
            if (string.IsNullOrEmpty(dbVersion))
            {
                isHealthy = false;
                messages.Add("⚠ Database version check failed.");
            }
            else
            {
                messages.Add($"✅ Database version: {dbVersion}");
            }

            // ✅ Query to check active connections
            var query = "SELECT COUNT(*) AS active_connections FROM information_schema.PROCESSLIST;";
            var datatable = await databaseConnection.GetAsync(query);

            var healthCheckConnections = gclSettings.HealthChecks.MaximumDatabaseConnections;
            var healthCheckConnectionsTime = gclSettings.HealthChecks.MaximumConnectionsInTime;

            if (healthCheckConnections > 0)
            {
                var activeConnections = Convert.ToInt32(datatable.Rows[0]["active_connections"]);
                if (activeConnections > healthCheckConnections)
                {
                    isHealthy = false;
                    messages.Add($"⚠ Too many database connections: {activeConnections} (Threshold: {healthCheckConnections}).");
                }
                else
                {
                    messages.Add($"✅ Active connections: {activeConnections} (Threshold: {healthCheckConnections}).");
                }
            }

            // ✅ Query to check long-running connections
            if (healthCheckConnectionsTime > 0)
            {
                query = "SELECT TIME AS connection_time_in_sec FROM information_schema.PROCESSLIST WHERE USER != 'repluser' AND HOST != 'localhost' ORDER BY connection_time_in_sec DESC";
                datatable = await databaseConnection.GetAsync(query);

                if (datatable.Rows.Count > 0)
                {
                    var connectionTime = Convert.ToInt32(datatable.Rows[0]["connection_time_in_sec"]);
                    if (connectionTime > healthCheckConnectionsTime)
                    {
                        isHealthy = false;
                        messages.Add($"⚠ Long-running connection detected: {connectionTime} sec (Threshold: {healthCheckConnectionsTime} sec).");
                    }
                    else
                    {
                        messages.Add($"✅ Longest connection time: {connectionTime} sec (Threshold: {healthCheckConnectionsTime} sec).");
                    }
                }
            }

        
            return isHealthy
                ? HealthCheckResult.Healthy(string.Join(" | ", messages)) // Put all the messages together.
                : HealthCheckResult.Unhealthy(string.Join(" | ", messages));
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"❌ Database health check failed: {ex.Message}", ex);
        }
    }
}
