using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task Invoke(HttpContext context, IDatabaseConnection databaseConnection)
    {
        var logId = await LogRequestAsync(context, databaseConnection);

        if (!gclSettings.RequestLoggingOptions.LogResponseBody)
        {
            await next.Invoke(context);
            await LogResponseAsync(context, logId, null, databaseConnection);
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

        await LogResponseAsync(context, logId, content, databaseConnection);
    }

    protected virtual async Task<ulong> LogRequestAsync(HttpContext context, IDatabaseConnection databaseConnection)
    {
        var currentOptions = gclSettings.RequestLoggingOptions;

        if (!currentOptions.Enabled || currentOptions.HttpMethods.All(method => !String.Equals(method.Method, context.Request.Method, StringComparison.OrdinalIgnoreCase)))
        {
            return 0;
        }

        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("host", context.Request.Host.Host);
        databaseConnection.AddParameter("path", context.Request.Path.Value);
        databaseConnection.AddParameter("scheme", context.Request.Scheme);
        databaseConnection.AddParameter("method", context.Request.Method);
        databaseConnection.AddParameter("protocol", context.Request.Protocol);
        databaseConnection.AddParameter("request_headers", String.Join(Environment.NewLine, context.Request.Headers.Select(header => $"{header.Key}: {(currentOptions.Headers.Any(h => String.Equals(h, header.Value, StringComparison.OrdinalIgnoreCase)) ? header.Value : "[Redacted]")}")));
        databaseConnection.AddParameter("request_body", currentOptions.LogRequestBody ? await new StreamReader(context.Request.Body).ReadToEndAsync() : null);
        databaseConnection.AddParameter("environment", gclSettings.Environment.ToString());
        return await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.GclRequestLog, 0UL);
    }

    protected virtual async Task LogResponseAsync(HttpContext context, ulong logId, string responseBody, IDatabaseConnection databaseConnection)
    {
        var currentOptions = gclSettings.RequestLoggingOptions;

        if (!currentOptions.Enabled || logId == 0)
        {
            return;
        }

        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("response_headers", String.Join(Environment.NewLine, context.Response.Headers.Select(header => $"{header.Key}: {(currentOptions.Headers.Any(h => String.Equals(h, header.Value, StringComparison.OrdinalIgnoreCase)) ? header.Value : "[Redacted]")}")));
        databaseConnection.AddParameter("response_body", responseBody);
        databaseConnection.AddParameter("status_code", context.Response.StatusCode);
        databaseConnection.AddParameter("user_id", 0); // TODO
        await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.GclRequestLog, logId);
    }
}