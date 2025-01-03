using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GeeksCoreLibrary.Core.Controllers
{
    [Area("GeoLocation")]
    public class GeoLocationController(IGeoLocationService geoLocationService) : Controller
    {
        /// <summary>
        /// Will attempt to retrieve address information based on ZIP code and house number using the Pro6PP API. This only works for Dutch and Belgian addresses.
        /// </summary>
        /// <param name="zipCode">A valid Dutch or Belgian ZIP code.</param>
        /// <param name="houseNumber">A house number to search for.</param>
        /// <param name="houseNumberAddition">A house number addition.</param>
        /// <param name="country">A country code. Should be either 'nl' or 'be'.</param>
        /// <returns></returns>
        [Route("/addressinfo.gcl")]
        [Route("/addressinfo.jcl")]
        [HttpGet]
        public async Task<IActionResult> GetAddressInfoAsync(
            [Required(ErrorMessage = "ZIP code is required."), RegularExpression(@"^\d{4}\s*(?:[a-zA-Z]{2})?$", ErrorMessage = "Invalid ZIP code.")] string zipCode,
            [Required(ErrorMessage = "House number is required.")] string houseNumber,
            string houseNumberAddition = "",
            [RegularExpression("^(nl|be)$", ErrorMessage = "Country code must be either 'nl' or 'be'.")] string country = "nl")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            };

            var result = JsonConvert.SerializeObject(await geoLocationService.GetAddressInfoAsync(zipCode, houseNumber, houseNumberAddition, country), serializerSettings);
            return Content(result, "application/json");
        }
    }
}
