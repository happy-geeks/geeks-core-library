﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GeeksCoreLibrary.Modules.Redirect.Interfaces;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Redirect.Middlewares;

public class RedirectMiddleWare(RequestDelegate next, ILogger<RedirectMiddleWare> logger)
{
    /// <summary>
    /// Invoke the middleware.
    /// Services are added here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
    /// </summary>
    public async Task Invoke(HttpContext context, IRedirectService redirectService, IOptions<GclSettings> gclSettings, IObjectsService objectsService)
    {
        logger.LogDebug("Invoked RedirectMiddleWare");

        var redirectToUrl = "";
        var oldUrl = HttpContextHelpers.GetOriginalRequestUri(context);
        if (!String.IsNullOrEmpty(context.Request.Headers["gclredirect"]) || context.Request.Method != "GET" || oldUrl.AbsolutePath.Contains("/api/", StringComparison.CurrentCultureIgnoreCase))
        {
            await next.Invoke(context);
            return;
        }

        if (HttpContextHelpers.IsGclMiddleWarePage(context))
        {
            // If this happens, it means that another middleware has already found something and we don't need to do this again.
            await next.Invoke(context);
            return;
        }

        var redirectPermanent = true;

        // Redirect module.
        if (!PrecompiledRegexes.UrlsToSkipForMiddlewaresRegex.IsMatch(oldUrl.ToString()))
        {
            var redirectRule = await redirectService.GetRedirectAsync(oldUrl);
            if (!String.IsNullOrEmpty(redirectRule.NewUrl))
            {
                var baseUri = HttpContextHelpers.GetBaseUri(context);
                // If the NewUrl is relative the baseUri is used to turn it into an absolute url.
                redirectToUrl = new Uri(baseUri, redirectRule.NewUrl).AbsoluteUri;
                redirectPermanent = redirectRule.Permanent;
                logger.LogDebug($"Handle redirect module, redirect from  '{redirectRule.OldUrl}' to '{redirectRule.NewUrl}'.");
            }
        }

        // Redirect main domain.
        if (gclSettings.Value.Environment == Environments.Live)
        {
            var mainDomainForRedirect = await redirectService.GetMainDomainForRedirectAsync();
            if (!String.IsNullOrEmpty(mainDomainForRedirect))
            {
                var fullUri = String.IsNullOrEmpty(redirectToUrl) ? HttpContextHelpers.GetOriginalRequestUri(context) : new Uri(redirectToUrl);
                var currentDomain = fullUri.Host;
                var ignoreDomains = (await objectsService.FindSystemObjectByDomainNameAsync("noredirectonurlonhost")).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (currentDomain != mainDomainForRedirect && !ignoreDomains.Contains(currentDomain, StringComparer.OrdinalIgnoreCase))
                {
                    redirectToUrl = fullUri.AbsoluteUri.Replace(currentDomain, mainDomainForRedirect);
                }
            }
        }

        // Custom redirects from settings module.
        var customRedirects = (await objectsService.FindSystemObjectByDomainNameAsync("url_syntax_redirects"))?.Split('\r', '\n').Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
        if (customRedirects != null && customRedirects.Count != 0)
        {
            var fullUri = String.IsNullOrEmpty(redirectToUrl) ? HttpContextHelpers.GetOriginalRequestUri(context) : new Uri(redirectToUrl);
            var urlWithoutQuery = fullUri.AbsolutePath;

            foreach (var redirect in customRedirects)
            {
                var split = redirect.Split("|");
                if (split.Length < 2)
                {
                    continue;
                }

                var oldUrlRegex = split[0];
                var newUrlSplit = split[1].Split(":");
                if (newUrlSplit.Length < 2 || !Int32.TryParse(newUrlSplit[0], out var statusCode))
                {
                    continue;
                }

                var newUrl = newUrlSplit.Length > 2 ? newUrlSplit[2] : newUrlSplit[1];
                var urlCase = newUrlSplit.Length > 2 ? newUrlSplit[1] : "";
                var regex = new Regex(oldUrlRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                var match = regex.Match(urlWithoutQuery);
                if (!match.Success)
                {
                    continue;
                }

                var original = newUrl;

                // Group number/index matches.
                foreach (Match subMatch in Regex.Matches(original, @"\[(\d+?)\]"))
                {
                    newUrl = newUrl.Replace(subMatch.Value, match.Groups[Int32.Parse(subMatch.Groups[1].Value)].Value);
                }

                // Group name matches.
                foreach (Match subMatch in Regex.Matches(original, @"\[(.+?)\]"))
                {
                    newUrl = newUrl.Replace(subMatch.Value, match.Groups[subMatch.Groups[1].Value].Value);
                }

                newUrl = urlCase.ToUpperInvariant() switch
                {
                    "LOWER" => newUrl.ToLowerInvariant(),
                    "UPPER" => newUrl.ToUpperInvariant(),
                    _ => newUrl
                };

                redirectToUrl = $"{fullUri.Scheme}://{fullUri.Authority}{newUrl}{fullUri.Query}";
                redirectPermanent = statusCode == 301;
                break; // Stop the foreach once we found a result.
            }
        }

        // Redirect to URL with trailing slash.
        if (await redirectService.ShouldRedirectToUrlWithTrailingSlashAsync())
        {
            var fullUri = String.IsNullOrEmpty(redirectToUrl) ? HttpContextHelpers.GetOriginalRequestUri(context) : new Uri(redirectToUrl);
            var urlWithoutQuery = fullUri.AbsoluteUri;
            if (!String.IsNullOrEmpty(fullUri.Query))
            {
                urlWithoutQuery = urlWithoutQuery.Replace(fullUri.Query, "");
            }

            var urlExtension = System.IO.Path.GetExtension(urlWithoutQuery);
            if (String.IsNullOrEmpty(urlExtension))
            {
                if (!urlWithoutQuery.EndsWith("/"))
                {
                    redirectToUrl = $"{urlWithoutQuery}/{fullUri.Query}";
                }
            }
        }

        // Redirect to lower case URLs.
        if (await redirectService.ShouldRedirectToLowerCaseUrlAsync())
        {
            var fullUri = String.IsNullOrEmpty(redirectToUrl) ? HttpContextHelpers.GetOriginalRequestUri(context) : new Uri(redirectToUrl);
            var urlWithoutQuery = fullUri.AbsoluteUri;

            if (!String.IsNullOrEmpty(fullUri.Query))
            {
                urlWithoutQuery = urlWithoutQuery.Replace(fullUri.Query, "");
            }

            if (urlWithoutQuery.Any(Char.IsUpper))
            {
                redirectToUrl = urlWithoutQuery.ToLower() + fullUri.Query;
            }
        }

        // Redirect to https.
        if (gclSettings.Value.Environment == Environments.Live)
        {
            var fullUri = String.IsNullOrEmpty(redirectToUrl) ? HttpContextHelpers.GetOriginalRequestUri(context) : new Uri(redirectToUrl);
            if ((await redirectService.ShouldRedirectToHttpsAsync()) && (fullUri.Scheme != "https"))
            {
                redirectToUrl = fullUri.AbsoluteUri.Replace("http://", "https://");
            }
        }

        // Do the actual redirect.
        if (!String.IsNullOrEmpty(redirectToUrl))
        {
            context.Response.Headers["gclredirect"] = "true";
            context.Response.Redirect(redirectToUrl, redirectPermanent);
        }
        else
        {
            // Only proceed to next middleware if there's no redirect.
            await next.Invoke(context);
        }
    }
}