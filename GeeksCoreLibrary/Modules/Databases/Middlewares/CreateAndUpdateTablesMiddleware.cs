using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.Databases.Middlewares;

public class CreateAndUpdateTablesMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<CreateAndUpdateTablesMiddleware> logger;

    public CreateAndUpdateTablesMiddleware(RequestDelegate next, ILogger<CreateAndUpdateTablesMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    /// <summary>
    /// Invoke the middleware.
    /// IDatabaseHelpersService is here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
    /// </summary>
    public async Task Invoke(HttpContext context, IDatabaseHelpersService databaseHelpersService)
    {
        try
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {Models.Constants.DatabaseConnectionLogTableName});
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while trying to create/update tables.");
        }

        await this.next.Invoke(context);
    }
}