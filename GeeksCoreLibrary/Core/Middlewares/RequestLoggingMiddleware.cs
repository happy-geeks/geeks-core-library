using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Middlewares;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class RequestLoggingMiddleware
{
    protected readonly RequestDelegate Next;
    protected readonly ILogger<RequestLoggingMiddleware> Logger;
    protected readonly GclSettings GclSettings;

    /// <summary>
    /// The table that should be used to save the logs in.
    /// Default is <see cref="WiserTableNames.GclRequestLog"/>, can be overwritten in overloads.
    /// </summary>
    protected virtual string LogTableName => WiserTableNames.GclRequestLog;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IOptions<GclSettings> gclSettings)
    {
        Next = next;
        Logger = logger;
        GclSettings = gclSettings.Value;
    }

    public async Task Invoke(HttpContext context, IServiceProvider serviceProvider)
    {
        var databaseConnection = (IDatabaseConnection)serviceProvider.GetService(typeof(IDatabaseConnection));
        var logId = await LogRequestAsync(context, databaseConnection, serviceProvider);

        if (!GclSettings.RequestLoggingOptions.LogResponseBody)
        {
            await Next.Invoke(context);
            await LogResponseAsync(logId, context, null, databaseConnection, serviceProvider);
            return;
        }

        // To read the response body, we need to set the response body to a new memory stream and then copy that memory stream back into the original stream.
        var originalResponseBody = context.Response.Body;
        using var responseBodyMemoryStream = new MemoryStream();
        context.Response.Body = responseBodyMemoryStream;

        await Next.Invoke(context);

        responseBodyMemoryStream.Position = 0;
        var content = await new StreamReader(responseBodyMemoryStream).ReadToEndAsync();
        responseBodyMemoryStream.Position = 0;
        await responseBodyMemoryStream.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody;

        await LogResponseAsync(logId, context, content, databaseConnection, serviceProvider);
    }

    /// <summary>
    /// Get the ID of the authenticated user.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> of the request.</param>
    /// <param name="serviceProvider">Service provider to get other services you might need.</param>
    /// <returns>The ID of the user or 0 if the user is not authenticated.</returns>
    protected virtual async Task<ulong> GetUserIdAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        var accountsService = (IAccountsService)serviceProvider.GetService(typeof(IAccountsService));
        var user = await accountsService!.GetUserDataFromCookieAsync();
        return user.UserId;
    }

    /// <summary>
    /// Log the request to the application in the database. This will return the ID of the new log row, so that the response can be logged to the same row later.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> of the request.</param>
    /// <param name="databaseConnection">The <see cref="IDatabaseConnection"/> for writing the log to the database.</param>
    /// <param name="serviceProvider">Service provider to get other services you might need. Is not used in the base class, but you can use it in overrides.</param>
    /// <returns>The ID of the newly added row in the log table.</returns>
    protected virtual async Task<ulong> LogRequestAsync(HttpContext context, IDatabaseConnection databaseConnection, IServiceProvider serviceProvider)
    {
        try
        {
            var currentOptions = GclSettings.RequestLoggingOptions;

            if (!currentOptions.Enabled || currentOptions.HttpMethods.All(method => !String.Equals(method.Method, context.Request.Method, StringComparison.OrdinalIgnoreCase)))
            {
                return 0;
            }

            var headers = new List<string>();
            foreach (var header in context.Request.Headers)
            {
                headers.Add($"{header.Key}: {(currentOptions.Headers.Any(h => String.Equals(h, header.Key, StringComparison.OrdinalIgnoreCase)) ? header.Value : "[Redacted]")}");
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("host", context.Request.Host.Host);
            databaseConnection.AddParameter("path", context.Request.Path.Value);
            databaseConnection.AddParameter("query_string", context.Request.QueryString.Value);
            databaseConnection.AddParameter("scheme", context.Request.Scheme);
            databaseConnection.AddParameter("method", context.Request.Method);
            databaseConnection.AddParameter("protocol", context.Request.Protocol);
            databaseConnection.AddParameter("request_headers", String.Join(Environment.NewLine, headers));
            databaseConnection.AddParameter("request_body", currentOptions.LogRequestBody ? await new StreamReader(context.Request.Body).ReadToEndAsync() : null);
            databaseConnection.AddParameter("environment", GclSettings.Environment.ToString());
            databaseConnection.AddParameter("start_datetime", DateTime.Now);
            databaseConnection.AddParameter("ip_address", HttpContextHelpers.GetUserIpAddress(context));
            return await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(LogTableName, 0UL);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Error while logging request.");
            return 0;
        }
    }

    /// <summary>
    /// Log the response of the application in the database.
    /// </summary>
    /// <param name="logId">The ID of the log row that logged the original request for this response.</param>
    /// <param name="context">The <see cref="HttpContext"/> of the request.</param>
    /// <param name="responseBody">The complete body of the response. Set to <see langword="null"/> to not log response body.</param>
    /// <param name="databaseConnection">The <see cref="IDatabaseConnection"/> for writing the log to the database.</param>
    /// <param name="serviceProvider">Service provider to get other services you might need. Is not used in the base class, but you can use it in overrides.</param>
    protected virtual async Task LogResponseAsync(ulong logId, HttpContext context, string responseBody, IDatabaseConnection databaseConnection, IServiceProvider serviceProvider)
    {
        try
        {
            var currentOptions = GclSettings.RequestLoggingOptions;

            if (!currentOptions.Enabled || logId == 0)
            {
                return;
            }

            var headers = new List<string>();
            foreach (var header in context.Response.Headers)
            {
                headers.Add($"{header.Key}: {(currentOptions.Headers.Any(h => String.Equals(h, header.Key, StringComparison.OrdinalIgnoreCase)) ? header.Value : "[Redacted]")}");
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("response_headers", String.Join(Environment.NewLine, headers));
            databaseConnection.AddParameter("response_body", responseBody);
            databaseConnection.AddParameter("status_code", context.Response.StatusCode);
            databaseConnection.AddParameter("user_id", await GetUserIdAsync(context, serviceProvider));
            databaseConnection.AddParameter("end_datetime", DateTime.Now);
            await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(LogTableName, logId);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Error while logging response.");
        }
    }
}