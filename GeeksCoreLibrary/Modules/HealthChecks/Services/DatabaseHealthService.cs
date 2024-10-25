using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
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
            
        var configuration = httpContext.Request.Query["configuration"].ToString();
        int? timeId = String.IsNullOrWhiteSpace(httpContext.Request.Query["timeId"].ToString())
            ? null
            : Convert.ToInt32(httpContext.Request.Query["timeId"]);
        var countWarningAsError =
            !String.IsNullOrWhiteSpace(httpContext.Request.Query["countWarningAsError"].ToString()) &&
            Convert.ToBoolean(httpContext.Request.Query["countWarningAsError"]);
        int? runningLongerThanMinutes =
            String.IsNullOrWhiteSpace(httpContext.Request.Query["runningLongerThanMinutes"].ToString())
                ? null
                : Convert.ToInt32(httpContext.Request.Query["runningLongerThanMinutes"]);

        var conditions = new List<string>()
        {
            "TRUE" // Always add true to the conditions to avoid empty conditions
        };

        // If a configuration name has been provided only check that specific configuration.
        if (!String.IsNullOrWhiteSpace(configuration))
        {
            conditions.Add("configuration = ?configuration");
            databaseConnection.AddParameter("configuration", configuration);
        }

        // If a timeId has been provided only check that specific timeId.
        if (timeId.HasValue)
        {
            conditions.Add("timeId = ?timeId");
            databaseConnection.AddParameter("timeId", timeId.Value);
        }
        
        var query =
            "SELECT COUNT(*) AS active_connections FROM information_schema.PROCESSLIST;";
        var datatable = await databaseConnection.GetAsync(query);

        if (datatable.Rows.Count == 0)
        {
            return await Task.FromResult(HealthCheckResult.Unhealthy("No data found"));
        }
         
        var healthCheckConnections = healthChecksSettings.MaximumDatabaseConnections;
        var activeConnections = Convert.ToInt32(datatable.Rows[0]["active_connections"]); // Retrieve the count of active connections
        
        if (activeConnections > healthCheckConnections) // Check if the number of active connections exceeds the limit
        {
            return await Task.FromResult(HealthCheckResult.Unhealthy(activeConnections +" databases connected, Too many database connections"));
        }
        
         query =
            "SELECT ID, USER, HOST, TIME AS connection_time_in_sec FROM information_schema.PROCESSLIST";
         datatable = await databaseConnection.GetAsync(query);

        if (datatable.Rows.Count == 0)
        {
            return await Task.FromResult(HealthCheckResult.Unhealthy("No data found"));
        }
        
        var healthCheckConnectionsTime = healthChecksSettings.MaximumConnectionsInTime;
        var connectionTime = Convert.ToInt32(datatable.Rows[0]["connection_time_in_sec"]);

        if (connectionTime > healthCheckConnectionsTime)
        {
            return await Task.FromResult(HealthCheckResult.Unhealthy("To many seconds of connection, time is too long"));
        }
        
        return await Task.FromResult(HealthCheckResult.Healthy("status: Healthy"));
    }
}




