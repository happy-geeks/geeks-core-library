using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Interfaces;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Controllers
{
    [Route("/postal-services/post-nl")]
    public class PostNLController : Controller
    {
        private readonly IPostNLService postNlService;

        public PostNLController(IPostNLService postNLService)
        {
            postNlService = postNLService;
        }
        
        /// <summary>
        /// Generate shipping label for the given order
        /// </summary>
        /// <param name="model">model containing order data for which to generate the label</param>
        /// <returns>Ok response or BadRequest if no order ids are given</returns>
        [HttpGet, Route("shipping-label")]
        public async Task<IActionResult> GenerateShippingLabelAsync([FromQuery] ShippingLabelRequestModel model)
        {
            if (String.IsNullOrWhiteSpace(model.EncryptedOrderIds))
            {
                return BadRequest("No order id specified");
            }

            return Ok(await postNlService.GenerateShippingLabelAsync(model.EncryptedOrderIds, model.ParcelType));
        }
    }
}