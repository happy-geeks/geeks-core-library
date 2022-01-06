using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Helpers;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OmniKassa.Exceptions;
using OmniKassa.Model;
using OmniKassa.Model.Enums;
using OmniKassa.Model.Order;
using OmniKassa.Model.Response;
using OmniKassa.Model.Response.Notification;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class RaboOmniKassaService : IPaymentServiceProviderService, ITransientService
    {
        /// <inheritdoc />
        public bool LogPaymentActions { get; set; }

        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IObjectsService objectsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;

        private OmniKassa.Environment environment = OmniKassa.Environment.SANDBOX;
        private string refreshToken = "";
        private string signingKey = "";

        public RaboOmniKassaService(IShoppingBasketsService shoppingBasketsService, IObjectsService objectsService, IHttpContextAccessor httpContextAccessor, IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings)
        {
            this.shoppingBasketsService = shoppingBasketsService;
            this.objectsService = objectsService;
            this.httpContextAccessor = httpContextAccessor;
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
        }

        /// <summary>
        /// Set the refresh token, signing key and environment based on the environment.
        /// </summary>
        /// <returns></returns>
        private async Task SetupEnvironment()
        {
            if (gclSettings.Environment.InList(Environments.Acceptance, Environments.Live))
            {
                environment = OmniKassa.Environment.PRODUCTION;
                refreshToken = await objectsService.FindSystemObjectByDomainNameAsync("ROK_refreshToken");
                signingKey = await objectsService.FindSystemObjectByDomainNameAsync("ROK_signingKey");
            }
            else
            {
                environment = OmniKassa.Environment.SANDBOX;
                refreshToken = await objectsService.FindSystemObjectByDomainNameAsync("ROK_refreshToken_test");
                signingKey = await objectsService.FindSystemObjectByDomainNameAsync("ROK_signingKey_test");
            }
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethods paymentMethod, string invoiceNumber)
        {
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            var orderBuilder = new MerchantOrder.Builder()
                .WithMerchantOrderId(invoiceNumber)
                .WithAmount(Money.FromDecimal(Currency.EUR, totalPrice))
                .WithMerchantReturnURL(await objectsService.FindSystemObjectByDomainNameAsync("PSP_returnURL"))
                .WithOrderItems(CreateOrderItems(shoppingBaskets));

            try
            {
                var billingAddress = CreateAddress(userDetails);
                var shippingDetails = CreateAddress(userDetails, "shipping_") ?? billingAddress; //If no shipping address has been provided use billing address.
                var paymentBrand = ConvertPaymentMethodToPaymentBrand(paymentMethod);

                orderBuilder.WithBillingDetail(billingAddress)
                    .WithShippingDetail(shippingDetails)
                    .WithPaymentBrand(paymentBrand);
            }
            //Converting the country code throws an argument exception if the code is not supported.
            //Converting payment method throws an argument exception if the method is not supported.
            catch (ArgumentException)
            {
                return new PaymentRequestResult
                {
                    Action = PaymentRequestActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                    Successful = false,
                    ErrorMessage = $"Unknown or unsupported payment method '{paymentMethod:G}'"
                };
            }

            orderBuilder.WithPaymentBrandForce(PaymentBrandForce.FORCE_ALWAYS); //Don't allow customers to change payment method on the Rabo OmniKassa website.

            var merchantOrder = orderBuilder.Build();

            await SetupEnvironment();

            var endpoint = OmniKassa.Endpoint.Create(environment, signingKey, refreshToken);

            MerchantOrderResponse response;

            try
            {
                response = await endpoint.Announce(merchantOrder);
            }
            catch (InvalidAccessTokenException)
            {
                return new PaymentRequestResult
                {
                    Action = PaymentRequestActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                    Successful = false,
                    ErrorMessage = "Failed to authenticate with Rabo omni kassa API"
                };
            }

            return new PaymentRequestResult()
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = response.RedirectUrl
            };
        }

        /// <summary>
        /// Convert the order lines in the shopping baskets to a <see cref="OrderItem"/>.
        /// </summary>
        /// <param name="shoppingBaskets">The shopping baskets to convert.</param>
        /// <returns>A collection of <see cref="OrderItem"/>s.</returns>
        private List<OrderItem> CreateOrderItems(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets)
        {
            var orderItems = new List<OrderItem>();

            foreach (var (main, lines) in shoppingBaskets)
            {
                foreach (var line in lines)
                {
                    // Get the title of the product. If it is a coupon no title is provided and description will be used instead.
                    var name = line.GetDetailValue("title");
                    if (String.IsNullOrWhiteSpace(name))
                    {
                        name = line.GetDetailValue("description");
                    }

                    var orderItem = new OrderItem.Builder()
                        .WithId(line.GetDetailValue("connecteditemid"))
                        .WithName(name)
                        .WithDescription(name)
                        .WithQuantity(line.GetDetailValue<int>("quantity"))
                        .WithAmount(Money.FromDecimal(Currency.EUR, line.GetDetailValue<decimal>("price")))
                        .Build();
                    
                    orderItems.Add(orderItem);
                }
            }

            return orderItems;
        }

        /// <summary>
        /// Convert the user details to an <see cref="Address"/>.
        /// Throws <see cref="ArgumentException"/> when the provided country code does not correspond with a country supported by Rabo OmniKassa.
        /// </summary>
        /// <param name="userDetails">The <see cref="WiserItemModel"/> containing the user details.</param>
        /// <param name="detailKeyPrefix">Additional string as a prefix for "street", "zipcode", "city", "country", "housenumber" and "housenumber_suffix". For example for shipping.</param>
        /// <returns>Returns an <see cref="Address"/> with the required information.</returns>
        private Address CreateAddress(WiserItemModel userDetails, string detailKeyPrefix = "")
        {
            //If a prefix is given but any of the required values doesn't contain a value return null.
            if (!String.IsNullOrWhiteSpace(detailKeyPrefix) && 
                (String.IsNullOrWhiteSpace(userDetails.GetDetailValue($"{detailKeyPrefix}street")))
                 || String.IsNullOrWhiteSpace(userDetails.GetDetailValue($"{detailKeyPrefix}zipcode"))
                 || String.IsNullOrWhiteSpace(userDetails.GetDetailValue($"{detailKeyPrefix}city"))
                 || String.IsNullOrWhiteSpace(userDetails.GetDetailValue($"{detailKeyPrefix}country")))
            {
                return null;
            }

            var addressBuilder = new Address.Builder()
                .WithFirstName(userDetails.GetDetailValue("firstname"))
                .WithLastName(userDetails.GetDetailValue("lastname"))
                .WithStreet(userDetails.GetDetailValue($"{detailKeyPrefix}street"))
                .WithPostalCode(userDetails.GetDetailValue($"{detailKeyPrefix}zipcode"))
                .WithCity(userDetails.GetDetailValue($"{detailKeyPrefix}city"))
                .WithCountryCode(ConvertCountryCodeStringToCountryCode(userDetails.GetDetailValue($"{detailKeyPrefix}country")));

            //Add house number if provided to Wiser.
            var houseNumber = userDetails.GetDetailValue($"{detailKeyPrefix}housenumber");
            if (String.IsNullOrWhiteSpace(houseNumber))
            {
                return addressBuilder.Build();
            }

            addressBuilder.WithHouseNumber(houseNumber);

            //Add house number addition if an addition has been provided to Wiser.
            var houseNumberAddition = userDetails.GetDetailValue($"{detailKeyPrefix}housenumber_suffix");
            if (!String.IsNullOrWhiteSpace(houseNumberAddition))
            {
                addressBuilder.WithHouseNumberAddition(houseNumberAddition);
            }

            return addressBuilder.Build();
        }

        /// <summary>
        /// Convert country code in wiser to <see cref="CountryCode"/> enum value.
        /// Throws <see cref="ArgumentException"/> when the provided country code does not correspond with a country supported by Rabo OmniKassa.
        /// </summary>
        /// <param name="countryCode">The country code to use. (Will be lowercased inside the function.)</param>
        /// <returns>Returns the <see cref="CountryCode"/> value of the corresponding country.</returns>
        private CountryCode ConvertCountryCodeStringToCountryCode(string countryCode)
        {
            var countyCodeLowered = countryCode.ToLower();

            switch (countyCodeLowered)
            {
                case "ad":
                    return CountryCode.AD;
                case "ae":
                    return CountryCode.AE;
                case "af":
                    return CountryCode.AF;
                case "ag":
                    return CountryCode.AG;
                case "ai":
                    return CountryCode.AI;
                case "al":
                    return CountryCode.AL;
                case "am":
                    return CountryCode.AM;
                case "ao":
                    return CountryCode.AO;
                case "aq":
                    return CountryCode.AQ;
                case "ar":
                    return CountryCode.AR;
                case "as":
                    return CountryCode.AS;
                case "at":
                    return CountryCode.AT;
                case "au":
                    return CountryCode.AU;
                case "aw":
                    return CountryCode.AW;
                case "ax":
                    return CountryCode.AX;
                case "az":
                    return CountryCode.AZ;
                case "ba":
                    return CountryCode.BA;
                case "bb":
                    return CountryCode.BB;
                case "bd":
                    return CountryCode.BD;
                case "be":
                    return CountryCode.BE;
                case "bf":
                    return CountryCode.BF;
                case "bg":
                    return CountryCode.BG;
                case "bh":
                    return CountryCode.BH;
                case "bi":
                    return CountryCode.BI;
                case "bj":
                    return CountryCode.BJ;
                case "bl":
                    return CountryCode.BL;
                case "bm":
                    return CountryCode.BM;
                case "bn":
                    return CountryCode.BN;
                case "bo":
                    return CountryCode.BO;
                case "bq":
                    return CountryCode.BQ;
                case "br":
                    return CountryCode.BR;
                case "bs":
                    return CountryCode.BS;
                case "bt":
                    return CountryCode.BT;
                case "bv":
                    return CountryCode.BV;
                case "bw":
                    return CountryCode.BW;
                case "by":
                    return CountryCode.BY;
                case "bz":
                    return CountryCode.BZ;
                case "ca":
                    return CountryCode.CA;
                case "cc":
                    return CountryCode.CC;
                case "cd":
                    return CountryCode.CD;
                case "cf":
                    return CountryCode.CF;
                case "cg":
                    return CountryCode.CG;
                case "ch":
                    return CountryCode.CH;
                case "ci":
                    return CountryCode.CI;
                case "ck":
                    return CountryCode.CK;
                case "cl":
                    return CountryCode.CL;
                case "cm":
                    return CountryCode.CM;
                case "cn":
                    return CountryCode.CN;
                case "co":
                    return CountryCode.CO;
                case "cr":
                    return CountryCode.CR;
                case "cu":
                    return CountryCode.CU;
                case "cv":
                    return CountryCode.CV;
                case "cw":
                    return CountryCode.CW;
                case "cx":
                    return CountryCode.CX;
                case "cy":
                    return CountryCode.CY;
                case "cz":
                    return CountryCode.CZ;
                case "de":
                    return CountryCode.DE;
                case "dj":
                    return CountryCode.DJ;
                case "dk":
                    return CountryCode.DK;
                case "dm":
                    return CountryCode.DM;
                case "do":
                    return CountryCode.DO;
                case "dz":
                    return CountryCode.DZ;
                case "ec":
                    return CountryCode.EC;
                case "ee":
                    return CountryCode.EE;
                case "eg":
                    return CountryCode.EG;
                case "eh":
                    return CountryCode.EH;
                case "er":
                    return CountryCode.ER;
                case "es":
                    return CountryCode.ES;
                case "et":
                    return CountryCode.ET;
                case "fi":
                    return CountryCode.FI;
                case "fj":
                    return CountryCode.FJ;
                case "fk":
                    return CountryCode.FK;
                case "fm":
                    return CountryCode.FM;
                case "fo":
                    return CountryCode.FO;
                case "fr":
                    return CountryCode.FR;
                case "ga":
                    return CountryCode.GA;
                case "gb":
                    return CountryCode.GB;
                case "gd":
                    return CountryCode.GD;
                case "ge":
                    return CountryCode.GE;
                case "gf":
                    return CountryCode.GF;
                case "gg":
                    return CountryCode.GG;
                case "gh":
                    return CountryCode.GH;
                case "gi":
                    return CountryCode.GI;
                case "gl":
                    return CountryCode.GL;
                case "gm":
                    return CountryCode.GM;
                case "gn":
                    return CountryCode.GN;
                case "gp":
                    return CountryCode.GP;
                case "gq":
                    return CountryCode.GQ;
                case "gr":
                    return CountryCode.GR;
                case "gs":
                    return CountryCode.GS;
                case "gt":
                    return CountryCode.GT;
                case "gu":
                    return CountryCode.GU;
                case "gw":
                    return CountryCode.GW;
                case "gy":
                    return CountryCode.GY;
                case "hk":
                    return CountryCode.HK;
                case "hm":
                    return CountryCode.HM;
                case "hn":
                    return CountryCode.HN;
                case "hr":
                    return CountryCode.HR;
                case "ht":
                    return CountryCode.HT;
                case "hu":
                    return CountryCode.HU;
                case "id":
                    return CountryCode.ID;
                case "ie":
                    return CountryCode.IE;
                case "il":
                    return CountryCode.IL;
                case "im":
                    return CountryCode.IM;
                case "in":
                    return CountryCode.IN;
                case "io":
                    return CountryCode.IO;
                case "iq":
                    return CountryCode.IQ;
                case "ir":
                    return CountryCode.IR;
                case "is":
                    return CountryCode.IS;
                case "it":
                    return CountryCode.IT;
                case "je":
                    return CountryCode.JE;
                case "jm":
                    return CountryCode.JM;
                case "jo":
                    return CountryCode.JO;
                case "jp":
                    return CountryCode.JP;
                case "ke":
                    return CountryCode.KE;
                case "kg":
                    return CountryCode.KG;
                case "kh":
                    return CountryCode.KH;
                case "ki":
                    return CountryCode.KI;
                case "km":
                    return CountryCode.KM;
                case "kn":
                    return CountryCode.KN;
                case "kp":
                    return CountryCode.KP;
                case "kr":
                    return CountryCode.KR;
                case "kw":
                    return CountryCode.KW;
                case "ky":
                    return CountryCode.KY;
                case "kz":
                    return CountryCode.KZ;
                case "la":
                    return CountryCode.LA;
                case "lb":
                    return CountryCode.LB;
                case "lc":
                    return CountryCode.LC;
                case "li":
                    return CountryCode.LI;
                case "lk":
                    return CountryCode.LK;
                case "lr":
                    return CountryCode.LR;
                case "ls":
                    return CountryCode.LS;
                case "lt":
                    return CountryCode.LT;
                case "lu":
                    return CountryCode.LU;
                case "lv":
                    return CountryCode.LV;
                case "ly":
                    return CountryCode.LY;
                case "ma":
                    return CountryCode.MA;
                case "mc":
                    return CountryCode.MC;
                case "md":
                    return CountryCode.MD;
                case "me":
                    return CountryCode.ME;
                case "mf":
                    return CountryCode.MF;
                case "mg":
                    return CountryCode.MG;
                case "mh":
                    return CountryCode.MH;
                case "mk":
                    return CountryCode.MK;
                case "ml":
                    return CountryCode.ML;
                case "mm":
                    return CountryCode.MM;
                case "mn":
                    return CountryCode.MN;
                case "mo":
                    return CountryCode.MO;
                case "mp":
                    return CountryCode.MP;
                case "mq":
                    return CountryCode.MQ;
                case "mr":
                    return CountryCode.MR;
                case "ms":
                    return CountryCode.MS;
                case "mt":
                    return CountryCode.MT;
                case "mu":
                    return CountryCode.MU;
                case "mv":
                    return CountryCode.MV;
                case "mw":
                    return CountryCode.MW;
                case "mx":
                    return CountryCode.MX;
                case "my":
                    return CountryCode.MY;
                case "mz":
                    return CountryCode.MZ;
                case "na":
                    return CountryCode.NA;
                case "nc":
                    return CountryCode.NC;
                case "ne":
                    return CountryCode.NE;
                case "nf":
                    return CountryCode.NF;
                case "ng":
                    return CountryCode.NG;
                case "ni":
                    return CountryCode.NI;
                case "nl":
                    return CountryCode.NL;
                case "no":
                    return CountryCode.NO;
                case "np":
                    return CountryCode.NP;
                case "nr":
                    return CountryCode.NR;
                case "nu":
                    return CountryCode.NU;
                case "nz":
                    return CountryCode.NZ;
                case "om":
                    return CountryCode.OM;
                case "pa":
                    return CountryCode.PA;
                case "pe":
                    return CountryCode.PE;
                case "pf":
                    return CountryCode.PF;
                case "pg":
                    return CountryCode.PG;
                case "ph":
                    return CountryCode.PH;
                case "pk":
                    return CountryCode.PK;
                case "pl":
                    return CountryCode.PL;
                case "pm":
                    return CountryCode.PM;
                case "pn":
                    return CountryCode.PN;
                case "pr":
                    return CountryCode.PR;
                case "ps":
                    return CountryCode.PS;
                case "pt":
                    return CountryCode.PT;
                case "pw":
                    return CountryCode.PW;
                case "py":
                    return CountryCode.PY;
                case "qa":
                    return CountryCode.QA;
                case "re":
                    return CountryCode.RE;
                case "ro":
                    return CountryCode.RO;
                case "rs":
                    return CountryCode.RS;
                case "ru":
                    return CountryCode.RU;
                case "rw":
                    return CountryCode.RW;
                case "sa":
                    return CountryCode.SA;
                case "sb":
                    return CountryCode.SB;
                case "sc":
                    return CountryCode.SC;
                case "sd":
                    return CountryCode.SD;
                case "se":
                    return CountryCode.SE;
                case "sg":
                    return CountryCode.SG;
                case "sh":
                    return CountryCode.SH;
                case "si":
                    return CountryCode.SI;
                case "sj":
                    return CountryCode.SJ;
                case "sk":
                    return CountryCode.SK;
                case "sl":
                    return CountryCode.SL;
                case "sm":
                    return CountryCode.SM;
                case "sn":
                    return CountryCode.SN;
                case "so":
                    return CountryCode.SO;
                case "sr":
                    return CountryCode.SR;
                case "ss":
                    return CountryCode.SS;
                case "st":
                    return CountryCode.ST;
                case "sv":
                    return CountryCode.SV;
                case "sx":
                    return CountryCode.SX;
                case "sy":
                    return CountryCode.SY;
                case "sz":
                    return CountryCode.SZ;
                case "tc":
                    return CountryCode.TC;
                case "td":
                    return CountryCode.TD;
                case "tf":
                    return CountryCode.TF;
                case "tg":
                    return CountryCode.TG;
                case "th":
                    return CountryCode.TH;
                case "tj":
                    return CountryCode.TJ;
                case "tk":
                    return CountryCode.TK;
                case "tl":
                    return CountryCode.TL;
                case "tm":
                    return CountryCode.TM;
                case "tn":
                    return CountryCode.TN;
                case "to":
                    return CountryCode.TO;
                case "tr":
                    return CountryCode.TR;
                case "tt":
                    return CountryCode.TT;
                case "tv":
                    return CountryCode.TV;
                case "tw":
                    return CountryCode.TW;
                case "tz":
                    return CountryCode.TZ;
                case "ua":
                    return CountryCode.UA;
                case "ug":
                    return CountryCode.UG;
                case "um":
                    return CountryCode.UM;
                case "us":
                    return CountryCode.US;
                case "uy":
                    return CountryCode.UY;
                case "uz":
                    return CountryCode.UZ;
                case "va":
                    return CountryCode.VA;
                case "vc":
                    return CountryCode.VC;
                case "ve":
                    return CountryCode.VE;
                case "vg":
                    return CountryCode.VG;
                case "vi":
                    return CountryCode.VI;
                case "vn":
                    return CountryCode.VN;
                case "vu":
                    return CountryCode.VU;
                case "wf":
                    return CountryCode.WF;
                case "ws":
                    return CountryCode.WS;
                case "ye":
                    return CountryCode.YE;
                case "yt":
                    return CountryCode.YT;
                case "za":
                    return CountryCode.ZA;
                case "zm":
                    return CountryCode.ZM;
                case "zw":
                    return CountryCode.ZW;
                default:
                    throw new ArgumentException($"Provided country: \"{countryCode}\" does not match a supported country code.");
            }
        }

        /// <summary>
        /// Convert <see cref="PaymentMethods"/> to <see cref="PaymentBrand"/>.
        /// Throws <see cref="ArgumentException"/> when the provided <see cref="PaymentMethods"/> is not supported by Rabo OmniKassa.
        /// </summary>
        /// <param name="paymentMethod">The <see cref="PaymentMethods"/> to convert.</param>
        /// <returns>Returns the <see cref="PaymentBrand"/> of the corresponding brand.</returns>
        private PaymentBrand ConvertPaymentMethodToPaymentBrand(PaymentMethods paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethods.Ideal:
                   return PaymentBrand.IDEAL;
                case PaymentMethods.Afterpay:
                    return PaymentBrand.AFTERPAY;
                case PaymentMethods.PayPal:
                    return PaymentBrand.PAYPAL;
                case PaymentMethods.Mastercard:
                    return PaymentBrand.MASTERCARD;
                case PaymentMethods.Visa:
                    return PaymentBrand.VISA;
                case PaymentMethods.Bancontact:
                    return PaymentBrand.BANCONTACT;
                case PaymentMethods.Maestro:
                    return PaymentBrand.MAESTRO;
                case PaymentMethods.Vpay:
                   return PaymentBrand.V_PAY;
                default:
                    throw new ArgumentException("Provided payment method can't be converted to payment brand.");
            }
        }

        /// <inheritdoc />
        public async Task<StatusUpdateResult> ProcessStatusUpdateAsync()
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return new StatusUpdateResult
                {
                    Status = "Request not available; unable to process status update.",
                    Successful = false
                };
            }

            await SetupEnvironment();
            
            PaymentCompletedResponse response;
            try
            {
                response = CreatePaymentCompletedResponse();
            }
            catch(IllegalSignatureException)
            {
                return new StatusUpdateResult
                {
                    Status = "Illegal signature received; unable to process status update.",
                    Successful = false
                };
            }
            
            await LogPaymentAction(response.OrderId, Convert.ToInt32(response.Status));

            switch (response.Status)
            {
                case PaymentStatus.COMPLETED:
                    return new StatusUpdateResult()
                    {
                        Successful = true
                    };
                case PaymentStatus.CANCELLED:
                    return new StatusUpdateResult()
                    {
                        Status = "User cancelled the order at the PSP.",
                        Successful = false
                    };
                case PaymentStatus.EXPIRED:
                    return new StatusUpdateResult()
                    {
                        Status = "The order expired at the PSP.",
                        Successful = false
                    };
                default:
                    return new StatusUpdateResult()
                    {
                        Status = "Unknown status; unable to process status update.",
                        Successful = false
                    };
            }
        }

        /// <summary>
        /// Create a <see cref="PaymentCompletedResponse"/> based on the request query.
        /// Throws <see cref="IllegalSignatureException"/> when the provided information by the query is not signed by the correct key.
        /// </summary>
        /// <returns>Returns a valid <see cref="PaymentCompletedResponse"/> object.</returns>
        private PaymentCompletedResponse CreatePaymentCompletedResponse()
        {
            var orderId = httpContextAccessor.HttpContext.Request.Query["order_id"].ToString(); //Invoice number as provided by us.
            var status = httpContextAccessor.HttpContext.Request.Query["status"].ToString();
            var signature = httpContextAccessor.HttpContext.Request.Query["signature"].ToString();

            return PaymentCompletedResponse.Create(orderId, status, signature, signingKey);
        }

        private async Task<bool> LogPaymentAction(string invoiceNumber, int status)
        {
            if (!LogPaymentActions || httpContextAccessor?.HttpContext == null)
            {
                return false;
            }

            var headers = new StringBuilder();
            var queryString = new StringBuilder();
            var formValues = new StringBuilder();

            foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Headers)
            {
                headers.AppendLine($"{key}: {value}");
            }

            foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Query)
            {
                queryString.AppendLine($"{key}: {value}");
            }

            if (httpContextAccessor.HttpContext.Request.HasFormContentType)
            {
                foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Form)
                {
                    formValues.AppendLine($"{key}: {value}");
                }
            }

            using var reader = new StreamReader(httpContextAccessor.HttpContext.Request.Body);
            var bodyJson = await reader.ReadToEndAsync();

            return await LoggingHelpers.AddLogEntryAsync(databaseConnection, PaymentServiceProviders.RaboOmniKassa, invoiceNumber, status, headers.ToString(), queryString.ToString(), formValues.ToString(), bodyJson);
        }

        /// <summary>
        /// Get the corresponding redirect URL after the return from the PSP website.
        /// Rabo OmniKassa only provides one return url containing information about the status of the order.
        /// </summary>
        /// <returns>Returns the url to redirect the user to.</returns>
        public async Task<string> GetRedirectUrlOnReturnFromPSP()
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL");
            }

            await SetupEnvironment();
            
            PaymentCompletedResponse response;
            try
            {
                response = CreatePaymentCompletedResponse();
            }
            catch (IllegalSignatureException)
            {
                return await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL");
            }

            switch (response.Status)
            {
                case PaymentStatus.COMPLETED:
                    return await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL");
                case PaymentStatus.IN_PROGRESS:
                    var pendingUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_pendingURL");

                    //Redirect to success url if no specific pending url has been provided.
                    if (String.IsNullOrWhiteSpace(pendingUrl))
                    {
                        pendingUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL");
                    }

                    return pendingUrl;
                case PaymentStatus.CANCELLED:
                    return await objectsService.FindSystemObjectByDomainNameAsync("PSP_cancelURL");
                case PaymentStatus.EXPIRED:
                    return await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL");
                default:
                    return await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL");
            }
        }

        /// <summary>
        /// Handle the notifications that are provided by Rabo OmniKassa by means of a webhook.
        /// </summary>
        /// <returns></returns>
        public async Task HandleNotification()
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return;
            }

            await SetupEnvironment();

            //Get the notification from the body.
            using var reader = new StreamReader(httpContextAccessor.HttpContext.Request.Body);
            var bodyJson = await reader.ReadToEndAsync();
            var notification = JsonConvert.DeserializeObject<ApiNotification>(bodyJson);

            if (notification == null)
            {
                return;
            }
            
            try
            {
                notification.ValidateSignature(signingKey);
            }
            catch (IllegalSignatureException)
            {
                return;
            }

            var notifyUrlBase = await objectsService.FindSystemObjectByDomainNameAsync("PSP_notifyurl");
            var endpoint = OmniKassa.Endpoint.Create(environment, signingKey, refreshToken);

            //Retrieve all MerchantOrderStatusResponses that are available.
            MerchantOrderStatusResponse response;
            do
            {
                response = await endpoint.RetrieveAnnouncement(notification);
                try
                {
                    response.ValidateSignature(signingKey);
                }
                catch (IllegalSignatureException)
                {
                    return;
                }
                
                //Handle each MerchantOrderResult separately to comply with the operation of the PaymentService.
                foreach (MerchantOrderResult result in response.OrderResults)
                {
                    //Ignore updates with the status of "IN_PROGRESS" in case those are given, only handle definitive states.
                    if (result.OrderStatus == PaymentStatus.IN_PROGRESS)
                    {
                        await LogPaymentAction(result.MerchantOrderId, Convert.ToInt32(result.OrderStatus));
                        continue;
                    }

                    //Prepare the signature data. The information and order needs to be the same as in PaymentCompletedResponse.
                    var signatureData = new List<string>()
                    {
                        result.MerchantOrderId,
                        result.OrderStatus.ToString()
                    };
                    var signature = Signable.CalculateSignature(signatureData, Convert.FromBase64String(signingKey));

                    var notifyUrl = $"{notifyUrlBase}&order_id={result.MerchantOrderId}&status={result.OrderStatus}&signature={signature}";

                    var request = (HttpWebRequest) WebRequest.Create(notifyUrl);
                    _ = request.GetResponseAsync(); //Ignore the return value.
                }
            } while (response.MoreOrderResultsAvailable);
        }
    }
}
