using System;
using System.Collections.Generic;
using System.Web;
using GeeksCoreLibrary.Core.Extensions;

namespace GeeksCoreLibrary.Core.Helpers;

public class UriHelpers
{
    /// <summary>
    /// Add parameters to the query string of an URL.
    /// </summary>
    /// <param name="url">The URL to add the parameters to.</param>
    /// <param name="parameters">The parameters to add.</param>
    /// <returns>The finished URL.</returns>
    public static string AddToQueryString(string url, IDictionary<string, string> parameters)
    {
        var uri = new Uri(url);
        return AddToQueryString(uri, parameters);
    }

    /// <summary>
    /// Add parameters to the query string of an URL.
    /// </summary>
    /// <param name="uri">The URI to add the parameters to.</param>
    /// <param name="parameters">The parameters to add.</param>
    /// <returns>The finished URL.</returns>
    public static string AddToQueryString(Uri uri, IDictionary<string, string> parameters)
    {
        var uriBuilder = new UriBuilder(uri);
        var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);

        foreach (var parameter in parameters)
        {
            queryString.Add(parameter.Key, parameter.Value);
        }

        uriBuilder.Query = queryString.ToQueryString();
        return uriBuilder.ToString();
    }
}