using System;
using System.Collections.Generic;
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

        var types = model.LabelType.Split(',');
        var coliAmounts = model.ColliAmount.Split(',');

        if (types.Length != coliAmounts.Length)
        {
            return BadRequest("Amount of types and coliNumbers are not equal");
        }

        var labelRules = new List<LabelRule>();
        
        for (var i = 0; i < types.Length; i++)
        {
            if (!Int32.TryParse(coliAmounts[i], out var coliAmount))
            {
                return BadRequest($"The coliAmount in position {i+1} is invalid.");
            }
            
            labelRules.Add(new LabelRule()
            {
                LabelType = types[i],
                ColiAmount = coliAmount
            });
        }

        try
        {
            return Ok(await NeDistriService.GenerateShippingLabelAsync(model.EncryptedOrderIds, labelRules, model.UserCode, model.OrderType));
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}