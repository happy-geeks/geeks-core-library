using System;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Branches.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.Branches.Services;

/// <inheritdoc cref="IBranchesService"/>
public class BranchesService(ILogger<BranchesService> logger, IHttpContextAccessor httpContextAccessor = null)
    : IBranchesService, ITransientService
{
    /// <inheritdoc />
    public string GetDatabaseNameFromCookie()
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            return null;
        }

        var cookieValue = httpContextAccessor.HttpContext.Request.Cookies[Constants.BranchCookieName];
        if (String.IsNullOrWhiteSpace(cookieValue))
        {
            return null;
        }

        try
        {
            return cookieValue.DecryptWithAesWithSalt(useSlowerButMoreSecureMethod: true);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, $"Could not decrypt cookie value '{cookieValue}'");
            return null;
        }
    }

    /// <inheritdoc />
    public void SaveDatabaseNameToCookie(string databaseName)
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            return;
        }

        HttpContextHelpers.WriteCookie(httpContextAccessor.HttpContext, Constants.BranchCookieName, databaseName.EncryptWithAesWithSalt(useSlowerButMoreSecureMethod: true), isEssential: true);
    }
}