using System;
using System.Collections.Generic;
using System.Linq;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Constants = GeeksCoreLibrary.Modules.Templates.Models.Constants;

namespace GeeksCoreLibrary.Core.Helpers;

public static class HttpContextHelpers
{
    public static readonly List<string> GclMiddleWarePages =
    [
        "/webpage.gcl",
        "/template.gcl",
        "/wiser-image.gcl",
        "/wiser-file.gcl",
        "/webpage.jcl",
        "/template.jcl",
        $"/{Components.OrderProcess.Models.Constants.CheckoutPage}",
        $"/{Components.OrderProcess.Models.Constants.PaymentInPage}",
        $"/{Components.OrderProcess.Models.Constants.PaymentOutPage}",
        "/health",
        "/health/wts",
        "/health/database"
    ];

    /// <summary>
    /// Get the hostname from the current request. Some examples:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Domain</term>
    ///         <description>Result</description>
    ///     </listheader>
    ///     <item>
    ///         <term>www.[testdomain]</term>
    ///         <description>testdomain</description>
    ///     </item>
    ///     <item>
    ///         <term>domain.nl</term>
    ///         <description>domain.nl</description>
    ///     </item>
    ///     <item>
    ///         <term>www.domain.nl</term>
    ///         <description>domain.nl</description>
    ///     </item>
    ///     <item>
    ///         <term>domain.[testdomain]</term>
    ///         <description>domain</description>
    ///     </item>
    ///     <item>
    ///         <term>domain.nl.[testdomain]</term>
    ///         <description>domain.nl</description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> object whose request should be handled.</param>
    /// <param name="testDomains">Optional: Which domains should be considered as test domains. These domain names will be stripped from the hostname.</param>
    /// <param name="includingTestWww">Optional: Whether "www." and "test." should be added to the result.</param>
    /// <param name="includePort">Optional: Whether to also return the port number in the URL (but only if it's a custom port, not with 80 and 443). Default is true.</param>
    /// <returns>The hostname as a string.</returns>
    public static string GetHostName(HttpContext httpContext, List<string> testDomains = null, bool includingTestWww = false, bool includePort = true)
    {
        if (httpContext == null)
        {
            return String.Empty;
        }

        testDomains ??= GclSettings.Current.TestDomains?.ToList() ?? [];

        string hostname;
        if (httpContext.Items.ContainsKey(Constants.WiserUriOverrideForReplacements) && httpContext.Items[Constants.WiserUriOverrideForReplacements] is Uri wiserUriOverride)
        {
            hostname = wiserUriOverride.Host;
        }
        else
        {
            hostname = httpContext.Request.Host.Host.ToLower();
        }

        if (includingTestWww == false && hostname.StartsWith("www", StringComparison.OrdinalIgnoreCase))
        {
            return hostname.Remove(0, hostname.IndexOf(".", StringComparison.OrdinalIgnoreCase) + 1);
        }

        foreach (var testDomain in testDomains.Where(testDomain => hostname.EndsWith("." + testDomain, StringComparison.OrdinalIgnoreCase)))
        {
            return hostname.Replace("." + testDomain, "");
        }

        if (includingTestWww == false && hostname.StartsWith("test.", StringComparison.OrdinalIgnoreCase))
        {
            return hostname.Remove(0, 5);
        }

        if (hostname.StartsWith("dev.", StringComparison.OrdinalIgnoreCase))
        {
            return hostname.Remove(0, 4);
        }

        // Will only return a port if the request URL contains a port (e.g.: "site.com:1234").
        var port = httpContext.Request.Host.Port;

        return port.HasValue && includePort ? $"{hostname}:{port}" : hostname;
    }

    /// <summary>
    /// /nl/ retuns nl / returns default document name
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="indexOfLanguagePartInUrl">Optional: The index of URL segments that contains </param>
    /// <returns></returns>
    public static string GetUrlPrefix(HttpContext httpContext, int indexOfLanguagePartInUrl = 0)
    {
        if (httpContext == null)
        {
            return String.Empty;
        }

        var pathSegments = GetOriginalRequestUri(httpContext).Segments;
        if (pathSegments.Length < indexOfLanguagePartInUrl)
        {
            return String.Empty;
        }

        if (pathSegments[indexOfLanguagePartInUrl].Equals("/") && pathSegments.Length > indexOfLanguagePartInUrl + 1)
        {
            return pathSegments[indexOfLanguagePartInUrl + 1].Trim('/');
        }

        return pathSegments[indexOfLanguagePartInUrl].Trim('/');
    }

    /// <summary>
    /// Gets the specified object from the <see cref="P:HttpContext.Request.Query" />, <see cref="P:HttpContext.Request.Form" />, <see cref="P:HttpContext.Request.Cookies" />, or <see cref="P:HttpContext.Request.ServerVariables" /> collections.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="key">The name of the collection member to get.</param>
    /// <param name="includeServerVariables">Optional: Whether the server variables collection should also be checked. Default is <see langword="true"/>.</param>
    /// <returns>The <see cref="P:HttpContext.Request.Query" />, <see cref="P:HttpContext.Request.Form" />, <see cref="P:HttpContext.Request.Cookies" />, or <see cref="P:HttpContext.Request.ServerVariables" /> collection member specified in the <paramref name="key" /> parameter. If the specified <paramref name="key" /> is not found, then <see langword="null" /> or <see langword="empty" /> is returned.</returns>
    public static string GetRequestValue(HttpContext httpContext, string key, bool includeServerVariables = true)
    {
        if (httpContext?.Request == null)
        {
            return null;
        }

        if (String.IsNullOrEmpty(key))
        {
            return null;
        }

        var result = httpContext.Request.Query[key].ToString();
        if (!String.IsNullOrEmpty(result))
        {
            return result;
        }

        if (httpContext.Request.HasFormContentType)
        {
            result = httpContext.Request.Form[key];

            if (!String.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        result = httpContext.Request.Cookies[key];

        if (!String.IsNullOrEmpty(result))
        {
            return result;
        }

        // Finally check if server variables should also be checked.
        if (includeServerVariables)
        {
            result = httpContext.GetServerVariable(key);
        }

        return result;
    }

    /// <summary>
    /// Checks if the specified key is present in <see cref="P:HttpContext.Request.Query" />, <see cref="P:HttpContext.Request.Form" />, <see cref="P:HttpContext.Request.Cookies" />, or <see cref="P:HttpContext.Request.ServerVariables" /> collection.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="key">The name of the collection member to get.</param>
    /// <param name="includeServerVariables">Optional: Whether the server variables collection should also be checked. Default is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if one of the collections (<see cref="P:HttpContext.Request.Query" />, <see cref="P:HttpContext.Request.Form" />, <see cref="P:HttpContext.Request.Cookies" />, and <see cref="P:HttpContext.Request.ServerVariables" />) contains an element with the key; otherwise, <see langword="false"/>.</returns>
    public static bool RequestContainsKey(HttpContext httpContext, string key, bool includeServerVariables = true)
    {
        if (httpContext?.Request == null)
        {
            return false;
        }

        if (String.IsNullOrEmpty(key))
        {
            return false;
        }

        return httpContext.Request.Query.ContainsKey(key)
               || (httpContext.Request.HasFormContentType && httpContext.Request.Form.ContainsKey(key))
               || httpContext.Request.Cookies.ContainsKey(key)
               || (includeServerVariables && httpContext.GetServerVariable(key) != null);
    }

    /// <summary>
    /// Adds a new cookie to the response.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="key">Name of the cookie</param>
    /// <param name="value">Value of the cookie</param>
    /// <param name="expires">Optional: The expiration date and time of the cookie. Use null to create a session cookie that expires when the user closes the browser. Default value is <see langword="null"/>.</param>
    /// <param name="domain">Optional: The domain to associate the cookie with. Default is <see langword="null"/>.</param>
    /// <param name="httpOnly">Optional: Set to true if the cookie must not be accessible by client-side script. Default value is true. </param>
    /// <param name="isEssential">Indicates if this cookie is essential for the application to function correctly. If true then consent policy checks may be bypassed. The default value is false.</param>
    public static void WriteCookie(HttpContext httpContext, string key, string value, DateTimeOffset? expires = null, string domain = null, bool httpOnly = true, bool isEssential = false)
    {
        var newCookieOptions = new CookieOptions
        {
            Expires = expires,
            Domain = domain,
            HttpOnly = httpOnly,
            Secure = httpContext.Request.IsHttps,
            IsEssential = isEssential,
            SameSite = GclSettings.Current.CookieSameSiteMode
        };

        httpContext.Response.Cookies.Append(key, value, newCookieOptions);
    }

    /// <summary>
    /// Reads a cookie's value from the request. If the cookie cannot be found a default value will be returned (which is an empty string by default).
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="key">Name of the cookie</param>
    /// <param name="defaultValueIfEmpty">An optional default value that will be returned if the cookie could not be found.</param>
    /// <returns>The value of the cookie, or <paramref name="defaultValueIfEmpty"/> if the cookie doesn't exist in the request.</returns>
    public static string ReadCookie(HttpContext httpContext, string key, string defaultValueIfEmpty = "")
    {
        return httpContext.Request.Cookies.ContainsKey(key) ? httpContext.Request.Cookies[key] : defaultValueIfEmpty;
    }

    /// <summary>
    /// Check if there is a cookie consent
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="consentLevel">Level of the cookie consent to be checked</param>
    /// <returns>True or false if the level is accepted or not</returns>
    public static bool CheckCookieConsentLevel(HttpContext httpContext, Enums.CookieConsentLevels consentLevel)
    {
        if (consentLevel == Enums.CookieConsentLevels.Necessary)
        {
            return true;
        }

        var currentUserConsent = httpContext.Request.Cookies["CookieConsent"];

        if (String.IsNullOrEmpty(currentUserConsent))
        {
            return false;
        }

        if (currentUserConsent == "-1")
        {
            // The user is not within a region that requires consent - all cookies are accepted
            return true;
        }

        // Read current user consent in encoded JavaScript format
        dynamic cookieConsent = Newtonsoft.Json.JsonConvert.DeserializeObject(System.Web.HttpUtility.UrlDecode(currentUserConsent));

        switch (consentLevel)
        {
            case Enums.CookieConsentLevels.Preferences:
                return Convert.ToBoolean(cookieConsent.preferences);
            case Enums.CookieConsentLevels.Statistics:
                return Convert.ToBoolean(cookieConsent.statistics);
            case Enums.CookieConsentLevels.Marketing:
                return Convert.ToBoolean(cookieConsent.preferences);
        }

        return false;
    }

    /// <summary>
    /// Gets header value as specified type.
    /// If the header exists multiple times, a comma separated value will be returned.
    /// If no such header exists, it returns the <see langword="default"/> of <see cref="T"/>.
    /// </summary>
    /// <typeparam name="T">Convert the header value to this type.</typeparam>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="headerName">The name of the header to get the value of.</param>
    /// <returns></returns>
    public static T GetHeaderValueAs<T>(HttpContext httpContext, string headerName)
    {
        if (httpContext == null || !httpContext.Request.Headers.TryGetValue(headerName, out var values))
        {
            return default;
        }

        var rawValues = values.ToString(); // writes out as Csv when there are multiple.

        if (String.IsNullOrWhiteSpace(rawValues))
        {
            return default;
        }

        return (T) Convert.ChangeType(rawValues, typeof(T));
    }

    /// <summary>
    /// Get the real remote IP of the client.
    /// This will also check if there is a header from Cloud Flare or a load balancer in the request headers.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>The remote IP address of the client.</returns>
    public static string GetUserIpAddress(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            return String.Empty;
        }

        var result = GetHeaderValueAs<string>(httpContext, "CF_CONNECTING_IP"); // Cloud Flare IP address.
        if (String.IsNullOrWhiteSpace(result))
        {
            result = GetHeaderValueAs<string>(httpContext, "True-Client-IP"); // Also Cloud Flare IP address.
        }

        if (String.IsNullOrWhiteSpace(result))
        {
            result = GetHeaderValueAs<string>(httpContext, "X_FORWARDED_FOR"); // TransIP and other providers use this.
        }

        if (String.IsNullOrWhiteSpace(result))
        {
            result = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        return result;
    }

    /// <summary>
    /// Get whether the current request is from localhost.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>True if the current request is from localhost, false if it's not.</returns>
    public static bool IsLocalhost(HttpContext httpContext)
    {
        var ip = httpContext?.Connection.RemoteIpAddress?.ToString();
        return ip is "::1" or "127.0.0.1";
    }

    /// <summary>
    /// Get the URL as the user sees it in their browser, before any rewrites have been done.
    /// This returns an URI builder, so you can still edit values in the URI.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>An <see cref="UriBuilder"/> containing the full URL.</returns>
    public static UriBuilder GetOriginalRequestUriBuilder(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            return new UriBuilder();
        }

        var result = new UriBuilder
        {
            // When the host is empty, go back to localhost.
            Host = String.IsNullOrWhiteSpace(httpContext.Request.Host.Host) ? "localhost" : httpContext.Request.Host.Host,
            Scheme = httpContext.Request.Scheme,
            Port = httpContext.Request.Host.Port ?? (httpContext.Request.IsHttps ? 443 : 80)
        };

        if (httpContext.Items.ContainsKey(Constants.OriginalPathKey) && httpContext.Items[Constants.OriginalPathKey] != null)
        {
            result.Path = ((PathString) httpContext.Items[Constants.OriginalPathKey]).ToString();
        }
        else
        {
            result.Path = httpContext.Request.Path.ToString();
        }

        if (httpContext.Items.ContainsKey(Constants.OriginalQueryStringKey) && httpContext.Items[Constants.OriginalQueryStringKey] != null)
        {
            result.Query = ((QueryString) httpContext.Items[Constants.OriginalQueryStringKey]).ToString();
        }
        else
        {
            result.Query = httpContext.Request.QueryString.ToString();
        }

        return result;
    }

    /// <summary>
    /// Get the URL as the user sees it in their browser, before any rewrites have been done.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>An <see cref="Uri"/> containing the full URL.</returns>
    public static Uri GetOriginalRequestUri(HttpContext httpContext)
    {
        return GetOriginalRequestUriBuilder(httpContext).Uri;
    }

    /// <summary>
    /// <para>
    /// Returns the base URL of the request URL, which is in this format:
    /// {scheme}://{host}{pathbase}
    /// </para>
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="alwaysHttps">Give true to always return the https scheme.</param>
    /// <returns>A <see cref="Uri"/> containing the base URL.</returns>
    public static Uri GetBaseUri(HttpContext httpContext, bool alwaysHttps = false)
    {
        return httpContext?.Request == null
            ? new Uri("https://localhost/")
            : new Uri($"{(alwaysHttps ? "https" : httpContext.Request.Scheme)}://{httpContext.Request.Host.Value}{httpContext.Request.PathBase.Value}");
    }

    public static ActionContext CreateActionContext(HttpContext httpContext)
    {
        var routeData = httpContext.GetRouteData();
        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        return actionContext;
    }

    /// <summary>
    /// Returns a 404
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    public static void Return404(HttpContext httpContext)
    {
        // when 404 is thrown in wiser loading of template is aborted.
        if (httpContext.Request.Host.ToString().Contains("wiser.nl", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        httpContext.Response.StatusCode = 404;
    }

    /// <summary>
    /// Is the current page a default page for one of our middlewares?
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    public static bool IsGclMiddleWarePage(HttpContext httpContext)
    {
        if (httpContext?.Request.Path == null)
        {
            return false;
        }

        var path = httpContext.Request.Path.ToString();

        if (path.EndsWith('/'))
        {
            return GclMiddleWarePages.Contains(path) || GclMiddleWarePages.Contains(path[..^1]);
        }

        return GclMiddleWarePages.Contains(path) || GclMiddleWarePages.Contains($"{path}/");
    }
}