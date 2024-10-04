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
    public class CssController : Controller
    {
        private readonly ITemplatesService templatesService;

        public CssController(ITemplatesService templatesService)
        {
            this.templatesService = templatesService;
        }
        
        [Route("/css/gcl_general.css")]
        [HttpGet]
        public async Task<IActionResult> GeneralCss(ResourceInsertModes mode = ResourceInsertModes.Standard)
        {
            var lastChangedDate = await templatesService.GetGeneralTemplateLastChangedDateAsync(TemplateTypes.Css, mode) ?? DateTime.Now;
            if (!this.IsModified(lastChangedDate))
            {
                return StatusCode((int) HttpStatusCode.NotModified);
            }

            var cssContent = await templatesService.GetGeneralTemplateValueAsync(TemplateTypes.Css, mode);
            Response.Headers.LastModified = cssContent.LastChangeDate.ToUniversalTime().ToString("R");
            Response.Headers.Expires = DateTime.Now.AddDays(7).ToUniversalTime().ToString("R");
            return Content(cssContent.Content, "text/css", Encoding.UTF8);
        }
        
        [Route("/css/gclcss_{templateIds:regex(.*)}.css")]
        [HttpGet]
        public async Task<IActionResult> PageCss(string templateIds)
        {
            var templateIdsList = templateIds.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
            var cssContent = await templatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Css);
            if (!this.IsModified(cssContent.LastChangeDate))
            {
                return StatusCode((int) HttpStatusCode.NotModified);
            }
            
            Response.Headers.LastModified = cssContent.LastChangeDate.ToUniversalTime().ToString("R");
            Response.Headers.Expires = DateTime.Now.AddDays(7).ToUniversalTime().ToString("R");
            return Content(cssContent.Content, "text/css", Encoding.UTF8);
        }
    }
}
