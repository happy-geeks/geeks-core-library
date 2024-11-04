using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Core.Middlewares
{
    public class AddAntiForgeryMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<AddAntiForgeryMiddleware> logger;

        public AddAntiForgeryMiddleware(RequestDelegate next, ILogger<AddAntiForgeryMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        /// <summary>
        /// Invoke the middleware.
        /// Services are added here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
        /// </summary>
        public async Task Invoke(HttpContext context, IAntiforgery antiForgery)
        {
            logger.LogDebug("Invoked AddAntiForgeryMiddleware");

            // Remember the original body.
            var originalBody = context.Response.Body;

            // Create a new body that will be used temporarily.
            await using var newBody = new MemoryStream();
            context.Response.Body = newBody;

            await next.Invoke(context);

            try
            {
                // The HTML should be generated at this point; read the entire response body as a string.
                newBody.Position = 0;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse <--- "context.Response.ContentType" actually is NULL in some cases, I don't know why ReSharper thinks that it can't be.
                if (context.Response.ContentType == null || !context.Response.ContentType.Contains("text/html"))
                {
                    // If the content type is not HTML, just copy the new stream back into the original stream.
                    await newBody.CopyToAsync(originalBody);
                }
                else
                {
                    // Add anti forgery tokens to all forms.
                    // This needs to be done AFTER content caching, to make sure everyone has a unique token.
                    var pageHtml = await new StreamReader(newBody).ReadToEndAsync();
                    pageHtml = AddAntiForgeryToForms(context, pageHtml, antiForgery);

                    // Turn the string back into a stream.
                    await using var newStream = new MemoryStream();
                    await using var writer = new StreamWriter(newStream);
                    await writer.WriteAsync(pageHtml);
                    await writer.FlushAsync();
                    newStream.Position = 0;

                    // Set the correct content length.
                    context.Response.ContentLength = newStream.Length;

                    // Copy the new body to the original body.
                    await newStream.CopyToAsync(originalBody);
                }
            }
            finally
            {
                // Put the original body back in the response.
                context.Response.Body = originalBody;
            }
        }

        /// <summary>
        /// Add hidden input with content id and anti forgery token in every form.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="html"></param>
        /// <param name="antiForgery"></param>
        /// <returns></returns>
        private string AddAntiForgeryToForms(HttpContext context, string html, IAntiforgery antiForgery)
        {
            var antiForgeryData = antiForgery.GetAndStoreTokens(context);
            var antiForgeryInput = $"<input type='hidden' name='{antiForgeryData.FormFieldName}' value='{antiForgeryData.RequestToken}' />";

            const int formTagLength = 7;
            var closeFormIndex = html.IndexOf("</form>", StringComparison.Ordinal);

            while (closeFormIndex > -1)
            {
                var startIndex = closeFormIndex + antiForgeryInput.Length + formTagLength;
                logger.LogDebug("Place hidden input with anti forgery token.");
                html = html.Insert(closeFormIndex, antiForgeryInput);

                closeFormIndex = html.IndexOf("</form>", startIndex, StringComparison.Ordinal);
            }

            return html;
        }
    }
}