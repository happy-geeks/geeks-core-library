using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Interfaces;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Services;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Controllers
{
    [Route("api/post-nl")]
    public class PostNLController : Controller
    {
        private readonly IPostNLService postNlService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly ICommunicationsService communicationService;
        private readonly IStringReplacementsService stringReplacementsService;

        public PostNLController(IPostNLService postNLService, IWiserItemsService wiserItemsService, ICommunicationsService communicationsService, IStringReplacementsService stringReplacementsService)
        {
            this.postNlService = postNLService;
            this.wiserItemsService = wiserItemsService;
            this.communicationService = communicationsService;
            this.stringReplacementsService = stringReplacementsService;
        }

        private async Task<string> GetCountryCode(WiserItemModel orderDetails, string entityName)
        {
            var orderCountryCode = orderDetails.GetDetailValue(entityName);
            string countryCode;
            ulong result;

            if (UInt64.TryParse(orderCountryCode, out result))
            {
                ulong countryId = UInt64.Parse(orderCountryCode);
                var countryItem = await wiserItemsService.GetItemDetailsAsync(countryId);

                countryCode = countryItem.GetDetailValue("name_short")?.ToUpper();
            }
            else
            {
                countryCode = orderCountryCode;
            }

            return countryCode;
        }

        [HttpGet, Route("shipping-label")]
        public async Task<IActionResult> GenerateShippingLabel([FromQuery] string encryptedOrderIds)
        {
            if (String.IsNullOrWhiteSpace(encryptedOrderIds))
            {
                return BadRequest("Geen order ID meegegeven.");
            }

            var result = new List<string>();

            foreach (var encryptedId in encryptedOrderIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var orderId = encryptedId.DecryptWithAes(withDateTime: true, minutesValidOverride: 30);
                var postNLDetails = await wiserItemsService.GetItemDetailsAsync(1118);
                var orderDetails = await wiserItemsService.GetItemDetailsAsync(UInt64.Parse(orderId));
                
                if (orderDetails == null || orderDetails.Id == 0)
                {
                    result.Add($"Order met ID '{orderId}' niet gevonden!");
                    continue;
                }

                var barcode = orderDetails.GetDetailValue("postnl_barcode");
                var shippingAddress = new AddressModel
                {
                    City = orderDetails.GetDetailValue("shipping_city"),
                    Countrycode = await GetCountryCode(orderDetails, "shipping_country"),
                    FirstName = orderDetails.GetDetailValue("firstname"),
                    Name = orderDetails.GetDetailValue("lastname"),
                    HouseNr = orderDetails.GetDetailValue("shipping_housenumber"),
                    HouseNrExt = orderDetails.GetDetailValue("shipping_housenumber_suffix"),
                    Street = orderDetails.GetDetailValue("shipping_street"),
                    Zipcode = orderDetails.GetDetailValue("shipping_zipcode"),
                    AddressType = "01"
                };
                if (String.IsNullOrWhiteSpace(shippingAddress.Zipcode))
                {
                    shippingAddress.Zipcode = orderDetails.GetDetailValue("zipcode");
                    shippingAddress.City = orderDetails.GetDetailValue("city");
                    shippingAddress.Street = orderDetails.GetDetailValue("street");
                    shippingAddress.HouseNr = orderDetails.GetDetailValue("housenumber");
                    shippingAddress.HouseNrExt = orderDetails.GetDetailValue("housenumber_suffix");
                    shippingAddress.Countrycode = await GetCountryCode(orderDetails, "country");
                }

                PostNLService.ShippingLocations shippingLocation;
                if (String.Equals("NL", shippingAddress.Countrycode ?? "NL", StringComparison.OrdinalIgnoreCase))
                {
                    shippingLocation = PostNLService.ShippingLocations.Netherlands;
                }
                else if (PostNLService.EuropeanCountries.Any(c => c.Equals(shippingAddress.Countrycode, StringComparison.OrdinalIgnoreCase)))
                {
                    shippingLocation = PostNLService.ShippingLocations.Europe;
                }
                else
                {
                    shippingLocation = PostNLService.ShippingLocations.Global;
                }

                barcode = (await postNlService.CreateNewBarcode(orderId, shippingLocation))?.Barcode;

                var settings = await postNlService.GetSettings(shippingLocation);
                var postNlRequest = new ShipmentRequestModel
                {
                    Customer = new CustomerModel
                    {
                        CustomerCode = settings.CustomerCode,
                        CustomerNumber = settings.CustomerNumber,
                        Address = new AddressModel
                        {
                            AddressType = "02",
                            CompanyName = postNLDetails.GetDetailValue("company_name"),
                            City = postNLDetails.GetDetailValue("city"),
                            Countrycode = postNLDetails.GetDetailValue("country"),
                            HouseNrExt = postNLDetails.GetDetailValue("number_ex"),
                            HouseNr = postNLDetails.GetDetailValue("number"),
                            Street = postNLDetails.GetDetailValue("street"),
                            Zipcode = postNLDetails.GetDetailValue("zipcode")
                        },
                        Email = orderDetails.GetDetailValue("email")
                    },
                    Message = new MessageModel
                    {
                        MessageID = orderId,
                        MessageTimeStamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                        Printertype = "GraphicFile|PDF"
                    },
                    Shipments = new List<ShipmentModel>
                        {
                            new ShipmentModel
                            {
                                ProductCodeDelivery = settings.ProductCode,
                                Addresses = new List<AddressModel> { shippingAddress },
                                Barcode = barcode,
                                Contacts = new List<ContactModel>
                                {
                                    new ContactModel
                                    {
                                        Email = orderDetails.GetDetailValue("email"),
                                        SmsNumber = orderDetails.GetDetailValue("phone")
                                    }
                                },
                                Remark = orderId
                            }
                        }
                };
                if (shippingLocation == PostNLService.ShippingLocations.Global)
                {
                    postNlRequest.Shipments.First().Customs = new CustomsModel
                    {
                        Content = new List<CustomsContentModel>(),
                        Currency = "EUR",
                        HandleAsNonDeliverable = "false",
                        Invoice = "true",
                        InvoiceNr = orderId,
                        ShipmentType = "Commercial Goods"
                    };
                    var orderLines = await wiserItemsService.GetLinkedItemDetailsAsync(UInt64.Parse(orderId), 5002, "orderline");
                    foreach (WiserItemModel orderLine in orderLines)
                    {
                        postNlRequest.Shipments.First()
                            .Customs.Content.Add(new CustomsContentModel
                            {
                                Description = orderLine.GetDetailValue("title"),
                                CountryOfOrigin = "NL",
                                HSTariffNr = "621112",
                                Quantity = orderLine.GetDetailValue("quantity"),
                                Value = orderLine.GetDetailValue("price").Replace(",", "."),
                                Weight = "500"
                            });
                    }
                }

                var postNlResponse = await postNlService.CreateTrackTraceLabel(orderId, postNlRequest);
                if (postNlResponse?.ResponseShipments == null || !postNlResponse.ResponseShipments.Any())
                {
                    result.Add($"Order {orderId}: Er is iets fout gegaan met de koppeling met de PostNL API.");
                    continue;
                }

                barcode = postNlResponse.ResponseShipments.First().Barcode;

                orderDetails.SetDetail("postnl_barcode", barcode);
                orderDetails.SetDetail("country_code", GetCountryCode(orderDetails, "country"));

                await wiserItemsService.UpdateAsync(orderDetails.Id, orderDetails);
                foreach (var labelResponseShipment in postNlResponse.ResponseShipments)
                {
                    if (labelResponseShipment.Errors.Any())
                    {
                        result.Add($"Order {orderId}: De PostNL API heeft een of meer fouten gegeven: {String.Join(", ", labelResponseShipment.Errors.Select(x => x.Description))}");
                        continue;
                    }

                    foreach (var label in labelResponseShipment.Labels)
                    {
                        await wiserItemsService.AddItemFileAsync(new WiserItemFileModel
                        {
                            ItemId = ulong.Parse(orderId),
                            Content = Convert.FromBase64String(label.Content),
                            Extension = ".pdf",
                            FileName = $"{label.Labeltype}.pdf",
                            ContentType = "application/pdf",
                            PropertyName = "postnl_label",
                            Title = barcode
                        });
                    }
                }
                
                result.Add($"Order {orderId}: Er is succesvol een verzendlabel gegenereerd en verstuurd naar de klant, deze kan gevonden worden op de tab 'PostNL' van deze order. De track&trace code is: {barcode}");
            }

            return Ok(new StringContent($"<ul><li>{String.Join("</li><li>", result)}</li></ul>", Encoding.UTF8, "text/html"));
        }
    }
}