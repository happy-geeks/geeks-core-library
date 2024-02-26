using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestLoggingMiddleware> logger;
    private readonly GclSettings gclSettings;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IOptions<GclSettings> gclSettings)
    {
        this.next = next;
        this.logger = logger;
        this.gclSettings = gclSettings.Value;
    }

    public async Task Invoke(HttpContext context, IDatabaseConnection databaseConnection, IAccountsService accountsService)
    {
        var logId = await LogRequestAsync(context, databaseConnection);
        var user = await accountsService.GetUserDataFromCookieAsync();

        if (!gclSettings.RequestLoggingOptions.LogResponseBody)
        {
            await next.Invoke(context);
            await LogResponseAsync(logId, context, null, databaseConnection, user.UserId);
            return;
        }

        // To read the response body, we need to set the response body to a new memory stream and then copy that memory stream back into the original stream.
        var originalResponseBody = context.Response.Body;
        using var responseBodyMemoryStream = new MemoryStream();
        context.Response.Body = responseBodyMemoryStream;

        await next.Invoke(context);

        responseBodyMemoryStream.Position = 0;
        var content = await new StreamReader(responseBodyMemoryStream).ReadToEndAsync();
        responseBodyMemoryStream.Position = 0;
        await responseBodyMemoryStream.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody;

        await LogResponseAsync(logId, context, content, databaseConnection, user.UserId);
    }

    /// <summary>
    /// Log the request to the application in the database. This will return the ID of the new log row, so that the response can be logged to the same row later.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> of the request.</param>
    /// <param name="databaseConnection">The <see cref="IDatabaseConnection"/> for writing the log to the database.</param>
    /// <returns>The ID of the newly added row in the log table.</returns>
    protected virtual async Task<ulong> LogRequestAsync(HttpContext context, IDatabaseConnection databaseConnection)
    {
        try
        {
            var currentOptions = gclSettings.RequestLoggingOptions;

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
            databaseConnection.AddParameter("environment", gclSettings.Environment.ToString());
            databaseConnection.AddParameter("start_datetime", DateTime.Now);
            return await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.GclRequestLog, 0UL);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while logging request.");
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
    /// <param name="userId">The ID of the logged in user.</param>
    protected virtual async Task LogResponseAsync(ulong logId, HttpContext context, string responseBody, IDatabaseConnection databaseConnection, ulong userId)
    {
        try
        {
            var currentOptions = gclSettings.RequestLoggingOptions;

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
            databaseConnection.AddParameter("user_id", userId);
            databaseConnection.AddParameter("end_datetime", DateTime.Now);
            await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.GclRequestLog, logId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while logging response.");
        }
    }
}