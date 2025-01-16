using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.Templates.Controllers;

[Area("Templates")]
public class JavascriptController(ITemplatesService templatesService) : Controller
{
    [Route("/scripts/gcl_general.js")]
    [HttpGet]
    public async Task<IActionResult> GeneralJavascript(ResourceInsertModes mode = ResourceInsertModes.Standard)
    {
        var lastChangedDate = await templatesService.GetGeneralTemplateLastChangedDateAsync(TemplateTypes.Js, mode) ?? DateTime.Now;
        if (!this.IsModified(lastChangedDate))
        {
            return StatusCode((int) HttpStatusCode.NotModified);
        }

        var javascriptContent = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Js, mode);
        Response.Headers.LastModified = javascriptContent.LastChangeDate.ToUniversalTime().ToString("R");
        Response.Headers.Expires = DateTime.Now.AddDays(7).ToUniversalTime().ToString("R");
        return Content(javascriptContent.Content, "application/javascript", Encoding.UTF8);
    }

    [Route("/scripts/gcljs_{templateIds:regex(.*)}.js")]
    [HttpGet]
    public async Task<IActionResult> PageJavascript(string templateIds)
    {
        var templateIdsList = templateIds.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
        var javascriptContent = await templatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Js);
        if (!this.IsModified(javascriptContent.LastChangeDate))
        {
            return StatusCode((int) HttpStatusCode.NotModified);
        }

        Response.Headers.LastModified = javascriptContent.LastChangeDate.ToUniversalTime().ToString("R");
        Response.Headers.Expires = DateTime.Now.AddDays(7).ToUniversalTime().ToString("R");
        return Content(javascriptContent.Content, "application/javascript", Encoding.UTF8);
    }
}