using System;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Seo.Controllers
{
    [Area("Seo")]
    public class SeoController : Controller
    {
        private readonly GclSettings gclSettings;
        private readonly IObjectsService objectsService;
        private readonly ISeoService seoService;

        public SeoController(IObjectsService objectsService, ISeoService seoService, IOptions<GclSettings> gclSettings)
        {
            this.objectsService = objectsService;
            this.seoService = seoService;
            this.gclSettings = gclSettings.Value;
        }

        [Route("robots.txt"), HttpGet]
        public async Task<IActionResult> Robots()
        {
            var robotsTxt = await objectsService.FindSystemObjectByDomainNameAsync("robotstxt");

            if (gclSettings.Environment != Environments.Live) // Do not index dev and test environments
            {
                robotsTxt = "User-agent: *" + Environment.NewLine + "Disallow: /";
            }            
            
            if (String.IsNullOrWhiteSpace(robotsTxt))
            {
                return NotFound();
            }

            Response.Headers.Add("Accept-Ranges", "bytes");
            return Content(robotsTxt, "text/plain", Encoding.UTF8);
        }

        [Route("googlesitemap.xml")]
        [Route("sitemap.xml")]
        [HttpGet]
        public async Task<IActionResult> Sitemap()
        {
            var siteMap = await seoService.GenerateSiteMap();
            if (siteMap == null)
            {
                return NotFound();
            }

            return Content(siteMap.ToString(), "text/xml", Encoding.UTF8);
        }

        [Route("googleimagesitemap.xml")]
        [Route("imagesitemap.xml")]
        [HttpGet]
        public async Task<IActionResult> ImageSitemap()
        {
            var siteMap = await seoService.GenerateImageSiteMap();
            if (siteMap == null)
            {
                return NotFound();
            }

            return Content(siteMap.ToString(), "text/xml", Encoding.UTF8);
        }
    }
}
