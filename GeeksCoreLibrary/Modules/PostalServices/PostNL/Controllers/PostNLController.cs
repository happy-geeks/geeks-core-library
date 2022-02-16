using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Interfaces;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Controllers
{
    [Route("/postal-services/post-nl")]
    public class PostNLController : Controller
    {
        private readonly IPostNLService postNlService;

        public PostNLController(IPostNLService postNLService)
        {
            this.postNlService = postNLService;
        }
        
        [HttpGet, Route("shipping-label")]
        public async Task<IActionResult> GenerateShippingLabelAsync([FromQuery] string encryptedOrderIds)
        {
            if (String.IsNullOrWhiteSpace(encryptedOrderIds))
            {
                BadRequest("No order id specified");
            }

            return Ok(await postNlService.GenerateShippingLabelAsync(encryptedOrderIds));
        }
    }
}