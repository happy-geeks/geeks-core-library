using System;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Core.Extensions;

public static class ControllerExtensions
{
    public static bool IsModified(this Controller controller, DateTime updatedAt)
    {
        var headerValue = controller.Request.Headers["If-Modified-Since"].ToString();
        if (String.IsNullOrWhiteSpace(headerValue))
        {
            return true;
        }

        var modifiedSince = DateTime.Parse(headerValue).ToLocalTime();
        return modifiedSince < updatedAt;
    }
}