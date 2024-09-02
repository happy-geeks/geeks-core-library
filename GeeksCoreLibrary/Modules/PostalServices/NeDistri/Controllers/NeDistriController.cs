using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.PostalServices.NeDistri.Interfaces;
using GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Controllers;

[Route("/postal-services/ne-distri")]
public class NeDistriController : Controller
{
    private INeDistriService NeDistriService;

    public NeDistriController(INeDistriService neDistriService)
    {
        NeDistriService = neDistriService;
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

        try
        {
            return Ok(await NeDistriService.GenerateShippingLabelAsync(model.EncryptedOrderIds, model.LabelType,
                model.ColliAmount, model.UserCode, model.OrderType));
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}