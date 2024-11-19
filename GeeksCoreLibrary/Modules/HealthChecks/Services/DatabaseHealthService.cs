using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services;
public class DatabaseHealthService : IHealthCheck
{
    private readonly IDatabaseConnection databaseConnection;
    private readonly HttpContext httpContext;
    private readonly HealthChecksSettings healthChecksSettings;
    
    public DatabaseHealthService(IDatabaseConnection databaseConnection, IHttpContextAccessor httpContextAccessor, IOptions<HealthChecksSettings> healthChecksSettings)
    {
        this.databaseConnection = databaseConnection;
        httpContext = httpContextAccessor.HttpContext;
        this.healthChecksSettings = healthChecksSettings.Value;
    }
    
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        databaseConnection.ClearParameters();
       
        // A new query to check the max active connections from the DatabaseHealthCheck
        var query =
            $"SELECT COUNT(*) AS active_connections FROM information_schema.PROCESSLIST;";
        var datatable = await databaseConnection.GetAsync(query);

        if (datatable.Rows.Count == 0)
        {
            return await Task.FromResult(HealthCheckResult.Unhealthy("No data found"));
        }
         
        var healthCheckConnections = healthChecksSettings.MaximumDatabaseConnections;
        var healthCheckConnectionsTime = healthChecksSettings.MaximumConnectionsInTime;

        // If no value is set, we are skipping this test.
        if (healthCheckConnections > 0)
        {
            // Retrieve the count of active connections.
            var activeConnections = Convert.ToInt32(datatable.Rows[0]["active_connections"]); 
            
            // Check if the number of active connections exceeds the limit.
            if (activeConnections > healthCheckConnections) 
            {
                return await Task.FromResult(HealthCheckResult.Unhealthy(activeConnections +" databases connected, Too many database connections"));
            }
        }
        
        // If no value is set, we are skipping this test.
        if (healthCheckConnectionsTime > 0)
        {    
            // Query to check the max open connections in time from the DatabaseHealthCheck.
            query =  $"SELECT ID, USER, HOST, TIME AS connection_time_in_sec FROM information_schema.PROCESSLIST";
            datatable = await databaseConnection.GetAsync(query);

            if (datatable.Rows.Count == 0)
            {
                return await Task.FromResult(HealthCheckResult.Unhealthy("No data found"));
            }
            
            // Retrieve the count of open connections in time.
            var connectionTime = Convert.ToInt32(datatable.Rows[0]["connection_time_in_sec"]);

            // Check if the time from open connections exceeds the limit.
            if (connectionTime > healthCheckConnectionsTime)
            {
                return await Task.FromResult(HealthCheckResult.Unhealthy("To many seconds of connection, time is too long")); 
            }
        }

        return await Task.FromResult(HealthCheckResult.Healthy("status: Healthy"));
    }
}

