using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace GeeksCoreLibrary.Modules.HealthChecks.Services
{
    public class DatabaseHealthService : IHealthCheck
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;

        public DatabaseHealthService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings)
        {
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
        }

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
                try
                {
                    var connectionTestQuery = "SELECT 1;";
                    // Use MySqlConnector for the connection and execution
                    using (var connection = new MySqlConnection(gclSettings.ConnectionString))
                    {
                        await connection.OpenAsync(cancellationToken);
                        using (var command = new MySqlCommand(connectionTestQuery, connection))
                        {
                            var result = await command.ExecuteScalarAsync(cancellationToken);
                            if (result == null || Convert.ToInt32(result) == 0)
                            {
                                isHealthy = false;
                                messages.Add("⚠ Database is unreachable.");
                            }
                            else
                            {
                                messages.Add("✅ Database is reachable.");
                                messages.Add("✅ Database is healthy and running.");
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    // If the query fails, handle the exception and return appropriate message
                    isHealthy = false;
                    messages.Add($"⚠ Database connectivity failed: {ex.Message}");
                    return HealthCheckResult.Unhealthy($"Database connectivity failed: {ex.Message}");
                }

                // ✅ Database Version Check
                try
                {
                    var versionQuery = "SELECT VERSION();";
                    // Again using MySqlConnector for querying
                    using (var connection = new MySqlConnection(gclSettings.ConnectionString))
                    {
                        await connection.OpenAsync(cancellationToken);
                        using (var command = new MySqlCommand(versionQuery, connection))
                        {
                            var dbVersion = await command.ExecuteScalarAsync(cancellationToken);
                            if (dbVersion == null)
                            {
                                isHealthy = false;
                                messages.Add("⚠ Database version check failed.");
                            }
                            else
                            {
                                messages.Add($"✅ Database version: {dbVersion}");
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    messages.Add($"⚠ Database version check failed: {ex.Message}");
                    isHealthy = false;
                }

                // ✅ Query to check active connections
                try
                {
                    var query = "SELECT COUNT(*) AS active_connections FROM information_schema.PROCESSLIST;";
                    // Use MySqlConnector for querying
                    using (var connection = new MySqlConnection(gclSettings.ConnectionString))
                    {
                        await connection.OpenAsync(cancellationToken);
                        using (var command = new MySqlCommand(query, connection))
                        {
                            var result = await command.ExecuteScalarAsync(cancellationToken);
                            var activeConnections = Convert.ToInt32(result);

                            var healthCheckConnections = gclSettings.HealthChecks.MaximumDatabaseConnections;
                            if (healthCheckConnections > 0)
                            {
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
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    messages.Add($"⚠ Active connections check failed: {ex.Message}");
                    isHealthy = false;
                }

                // ✅ Query to check long-running connections
                try
                {
                    var healthCheckConnectionsTime = gclSettings.HealthChecks.MaximumConnectionsInTime;
                    if (healthCheckConnectionsTime > 0)
                    {
                        var query = "SELECT TIME AS connection_time_in_sec FROM information_schema.PROCESSLIST WHERE USER != 'repluser' AND HOST != 'localhost' ORDER BY connection_time_in_sec DESC";
                        // Use MySqlConnector for querying
                        using (var connection = new MySqlConnection(gclSettings.ConnectionString))
                        {
                            await connection.OpenAsync(cancellationToken);
                            using (var command = new MySqlCommand(query, connection))
                            {
                                var reader = await command.ExecuteReaderAsync(cancellationToken);
                                if (await reader.ReadAsync(cancellationToken))
                                {
                                    var connectionTime = reader.GetInt32("connection_time_in_sec");
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
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    messages.Add($"⚠ Long-running connections check failed: {ex.Message}");
                    isHealthy = false;
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
}
