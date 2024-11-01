using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.PostalServices.NeDistri.Interfaces;
using GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Controllers;

[Route("/postal-services/ne-distri")]
public class NeDistriController : Controller
{
    private readonly INeDistriService neDistriService;
    private readonly ILogger<NeDistriController> logger;

    public NeDistriController(INeDistriService neDistriService, ILogger<NeDistriController> logger)
    {
        this.neDistriService = neDistriService;
        this.logger = logger;
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
            return Ok(await neDistriService.GenerateShippingLabelAsync(model.EncryptedOrderIds, labelRules, model.UserCode, model.OrderType));
        }
        catch (ArgumentException e)
        {
            logger.LogError(e, "Something went wrong when generating shipping label for order in NeDistriController");
            return BadRequest(e.Message);
        }
    }
}