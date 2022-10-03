using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.Payments.Services;

public class PaymentServiceProviderBaseService
{
    public bool LogPaymentActions { get; set; }

    private readonly IDatabaseHelpersService databaseHelpersService;
    private readonly IDatabaseConnection databaseConnection;
    private readonly ILogger<PaymentServiceProviderBaseService> logger;
    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// Create a new instance of <see cref="PaymentServiceProviderBaseService"/>.
    /// </summary>
    protected PaymentServiceProviderBaseService(IDatabaseHelpersService databaseHelpersService, IDatabaseConnection databaseConnection, ILogger<PaymentServiceProviderBaseService> logger, IHttpContextAccessor httpContextAccessor)
    {
        this.databaseHelpersService = databaseHelpersService;
        this.databaseConnection = databaseConnection;
        this.logger = logger;
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Logs an incoming request from a PSP to our webhook for handling payment results.
    /// </summary>
    /// <param name="paymentServiceProvider">The PSP that called our webhook.</param>
    /// <param name="invoiceNumber">The invoice / order number.</param>
    /// <param name="status">The status of the payment.</param>
    /// <param name="requestBody">Optional: The request body. If empty, we will read the body from the request stream.</param>
    /// <param name="responseBody">Optional: The response body.</param>
    /// <param name="error">Optional: Any error that occurred.</param>
    protected async Task LogIncomingPaymentActionAsync(PaymentServiceProviders paymentServiceProvider, string invoiceNumber, int status, string requestBody = "", string responseBody = "", string error = "")
    {
        if (!LogPaymentActions || httpContextAccessor?.HttpContext == null)
        {
            return;
        }

        var headers = new StringBuilder();
        var queryString = new StringBuilder();
        var formValues = new StringBuilder();

        foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Headers)
        {
            headers.AppendLine($"{key}: {value}");
        }

        foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Query)
        {
            queryString.AppendLine($"{key}: {value}");
        }

        if (httpContextAccessor.HttpContext.Request.HasFormContentType)
        {
            foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Form)
            {
                formValues.AppendLine($"{key}: {value}");
            }
        }

        if (String.IsNullOrWhiteSpace(requestBody))
        {
            using var reader = new StreamReader(httpContextAccessor.HttpContext.Request.Body);
            requestBody = await reader.ReadToEndAsync();
        }

        var uri = HttpContextHelpers.GetOriginalRequestUriBuilder(httpContextAccessor.HttpContext);

        await AddLogEntryAsync(paymentServiceProvider, invoiceNumber, status, headers.ToString(), queryString.ToString(), formValues.ToString(), requestBody, url: uri.ToString(), responseBody: responseBody, error: error);
    }

    /// <summary>
    /// Add an entry to the database log table for PSP actions.
    /// This will automatically create the logging table if it doesn't exist yet.
    /// </summary>
    /// <param name="paymentServiceProvider">The <see cref="PaymentServiceProviders"/> with the data of the used PSP.</param>
    /// <param name="uniquePaymentNumber">Optional: The unique payment number of the order.</param>
    /// <param name="status">Optional: The status of the payment.</param>
    /// <param name="requestHeaders">Optional: The request headers.</param>
    /// <param name="requestQueryString">Optional: The query string of the URL.</param>
    /// <param name="requestFormValues">Optional: The form values.</param>
    /// <param name="requestBody">Optional: The request body.</param>
    /// <param name="responseBody">Optional: The response body.</param>
    /// <param name="error">Optional: Any error that occurred.</param>
    /// <param name="url">Optional: The URL that was called.</param>
    /// <param name="isIncomingRequest">Optional: Whether the request was to our webhook (true) or it was a request to the API of the PSP (false).</param>
    protected async Task AddLogEntryAsync(PaymentServiceProviders paymentServiceProvider, string uniquePaymentNumber = "", int status = 0, string requestHeaders = "", string requestQueryString = "", string requestFormValues = "", string requestBody = "", string responseBody = "", string error = "", string url = "", bool isIncomingRequest = true)
    {
        try
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {Constants.PaymentServiceProviderLogTableName});

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("payment_service_provider", paymentServiceProvider.ToString("G"));
            databaseConnection.AddParameter("unique_payment_number", uniquePaymentNumber);
            databaseConnection.AddParameter("status", status);
            databaseConnection.AddParameter("request_headers", requestHeaders);
            databaseConnection.AddParameter("request_query_string", requestQueryString);
            databaseConnection.AddParameter("request_form_values", requestFormValues);
            databaseConnection.AddParameter("request_body", requestBody);
            databaseConnection.AddParameter("response_body", responseBody);
            databaseConnection.AddParameter("error", error);
            databaseConnection.AddParameter("url", url);
            databaseConnection.AddParameter("type", isIncomingRequest ? "incoming" : "outgoing");

            await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(Constants.PaymentServiceProviderLogTableName, 0UL);
        }
        catch (Exception exception)
        {
            // Make sure the application can't crash because of logging errors.
            logger.LogWarning(exception, $"Error while trying to log message to {Constants.PaymentServiceProviderLogTableName}");
        }
    }
}