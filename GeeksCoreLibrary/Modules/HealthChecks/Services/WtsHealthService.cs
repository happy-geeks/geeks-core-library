using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services;

public class WtsHealthService : IHealthCheck
{
    private readonly IDatabaseConnection databaseConnection;
    private readonly HttpContext httpContext;

    public WtsHealthService(IDatabaseConnection databaseConnection, IHttpContextAccessor httpContextAccessor)
    {
        this.databaseConnection = databaseConnection;
        httpContext = httpContextAccessor.HttpContext;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        var configuration = httpContext.Request.Query["configuration"].ToString();
        int? timeId = String.IsNullOrWhiteSpace(httpContext.Request.Query["timeId"].ToString()) ? null : Convert.ToInt32(httpContext.Request.Query["timeId"]);
        var countWarningAsError = !String.IsNullOrWhiteSpace(httpContext.Request.Query["countWarningAsError"].ToString()) && Convert.ToBoolean(httpContext.Request.Query["countWarningAsError"]);
        int? runningLongerThanMinutes = String.IsNullOrWhiteSpace(httpContext.Request.Query["runningLongerThanMinutes"].ToString()) ? null : Convert.ToInt32(httpContext.Request.Query["runningLongerThanMinutes"]);

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

        var query = $"SELECT configuration, time_id, state, next_run FROM wts_services WHERE {String.Join(" AND ", conditions)}";
        var datatable = await databaseConnection.GetAsync(query);

        if (datatable.Rows.Count == 0)
        {
            return HealthCheckResult.Unhealthy("No data found");
        }

        var errors = new List<string>();

        foreach (DataRow row in datatable.Rows)
        {
            var state = row.Field<string>("state");
            switch (state)
            {
                case "paused":
                    // Do nothing
                    break;
                case "success":
                case "active":
                {
                    // If the service is active or was successful the last time it ran, check if it should have started more than 5 minutes ago.
                    var startRunTime = row.Field<DateTime>("next_run");
                    if (DateTime.Now > startRunTime.AddMinutes(5))
                    {
                        errors.Add($"Service {row.Field<string>("configuration")} with time ID {row.Field<int>("time_id")} should have started more than 5 minutes ago");
                    }
                    break;
                }
                case "running":
                    // If the service is running, check if it has been running for longer than the provided minutes.
                    if (runningLongerThanMinutes.HasValue)
                    {
                        var startRunTime = row.Field<DateTime>("next_run");
                        if (DateTime.Now.Subtract(startRunTime).TotalMinutes > runningLongerThanMinutes.Value)
                        {
                            errors.Add($"Service {row.Field<string>("configuration")} with time ID {row.Field<int>("time_id")} has been running for longer than {runningLongerThanMinutes.Value} minutes");
                        }
                    }
                    break;
                case "stopped":
                    errors.Add($"Service {row.Field<string>("configuration")} with time ID {row.Field<int>("time_id")} is stopped");
                    break;
                case "crashed":
                    errors.Add($"Service {row.Field<string>("configuration")} with time ID {row.Field<int>("time_id")} has crashed");
                    break;
                case "failed":
                    errors.Add($"Service {row.Field<string>("configuration")} with time ID {row.Field<int>("time_id")} has failed");
                    break;
                case "warning":
                {
                    if (countWarningAsError)
                    {
                        errors.Add($"Service {row.Field<string>("configuration")} with time ID {row.Field<int>("time_id")} is in warning state");
                    }

                    // If the service is in warning state, check if it should have started more than 5 minutes ago.
                    var startRunTime = row.Field<DateTime>("next_run");
                    if (DateTime.Now > startRunTime.AddMinutes(5))
                    {
                        errors.Add($"Service {row.Field<string>("configuration")} with time ID {row.Field<int>("time_id")} should have started more than 5 minutes ago");
                    }
                    break;
                }
                default:
                    errors.Add($"Service {row.Field<string>("configuration")} with time ID {row.Field<int>("time_id")} is in unknown state: {state}");
                    break;
            }
        }

        return errors.Any() ? HealthCheckResult.Unhealthy(String.Join(", ", errors)) : HealthCheckResult.Healthy();
    }
}