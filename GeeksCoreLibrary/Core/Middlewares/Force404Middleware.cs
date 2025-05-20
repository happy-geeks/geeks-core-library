using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GeeksCoreLibrary.Core.Middlewares;

public class Force404Middleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        await next(context);

        if (context.Items.ContainsKey("Force404") && context.Response.StatusCode == 200)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}