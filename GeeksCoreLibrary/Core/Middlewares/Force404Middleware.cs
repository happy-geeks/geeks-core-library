using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;

namespace GeeksCoreLibrary.Core.Middlewares;

public class Force404Middleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        await next(context);

        if (context.Items.ContainsKey(Constants.ForceNotFoundStatusKey) && context.Response.StatusCode == 200)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}