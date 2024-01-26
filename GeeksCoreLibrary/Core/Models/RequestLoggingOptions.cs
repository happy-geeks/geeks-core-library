using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Net.Http.Headers;

namespace GeeksCoreLibrary.Core.Models;

public class RequestLoggingOptions
{
    /// <summary>
    /// Whether or not the request logging is enabled. Default is false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The HTTP methods that should be logged.
    /// </summary>
    public List<HttpMethod> HttpMethods = new() { HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete };

    /// <summary>
    /// The HTTP headers that should be logged. Any headers not in this list will be redacted.
    /// </summary>
    public List<string> Headers = new()
    {
        HeaderNames.Accept,
        HeaderNames.AcceptCharset,
        HeaderNames.AcceptEncoding,
        HeaderNames.AcceptLanguage,
        HeaderNames.Allow,
        HeaderNames.CacheControl,
        HeaderNames.Connection,
        HeaderNames.ContentEncoding,
        HeaderNames.ContentLength,
        HeaderNames.ContentType,
        HeaderNames.Date,
        HeaderNames.DNT,
        HeaderNames.Expect,
        HeaderNames.Host,
        HeaderNames.MaxForwards,
        HeaderNames.Range,
        HeaderNames.SecWebSocketExtensions,
        HeaderNames.SecWebSocketVersion,
        HeaderNames.TE,
        HeaderNames.Trailer,
        HeaderNames.TransferEncoding,
        HeaderNames.Upgrade,
        HeaderNames.UserAgent,
        HeaderNames.Warning,
        HeaderNames.XRequestedWith,
        HeaderNames.XUACompatible
    };

    /// <summary>
    /// Whether or not the request should be logged. Default is false.
    /// </summary>
    public bool LogRequestBody { get; set; }

    /// <summary>
    /// Whether or not the response should be logged. Default is false.
    /// </summary>
    public bool LogResponseBody { get; set; }
}