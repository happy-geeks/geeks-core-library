using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.Templates.Controllers
{
    [Area("Templates")]
    public class JavascriptController : Controller
    {
        private readonly ITemplatesService templatesService;

        public JavascriptController(ITemplatesService templatesService)
        {
            this.templatesService = templatesService;
        }

        [Route("/scripts/gcl_general.js")]
        [HttpGet]
        public async Task<IActionResult> GeneralJavascript()
        {
            var lastChangedDate = await templatesService.GetGeneralTemplateLastChangedDateAsync(TemplateTypes.Js) ?? DateTime.Now;
            if (!this.IsModified(lastChangedDate))
            {
                return StatusCode((int) HttpStatusCode.NotModified);
            }

            var javascriptContent = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Js);
            Response.Headers.Add("Last-Modified", javascriptContent.LastChangeDate.ToUniversalTime().ToString("R"));
            Response.Headers.Add("Expires", DateTime.Now.AddDays(7).ToUniversalTime().ToString("R"));
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
            
            Response.Headers.Add("Last-Modified", javascriptContent.LastChangeDate.ToUniversalTime().ToString("R"));
            Response.Headers.Add("Expires", DateTime.Now.AddDays(7).ToUniversalTime().ToString("R"));
            return Content(javascriptContent.Content, "application/javascript", Encoding.UTF8);
        }
    }
}
