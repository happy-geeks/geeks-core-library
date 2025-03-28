﻿using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Core.Extensions;

public static class ExceptionMiddlewareExtensions
{
    public static void ConfigureExceptionHandler<T>(this IApplicationBuilder app, ILogger<T> logger)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature != null)
                {
                    logger.LogCritical(contextFeature.Error, "An unhandled exception occurred");

                    var response = new {error = contextFeature.Error.Message};
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                }
            });
        });
    }
}