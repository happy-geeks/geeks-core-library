using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.Payments.Controllers
{
    [Area("Payments")]
    public class PaymentsController : Controller
    {
        private readonly IPaymentsService paymentsService;

        public PaymentsController(IPaymentsService paymentsService)
        {
            this.paymentsService = paymentsService;
        }

        /// <summary>
        /// The route for handling payment requests.
        /// </summary>
        /// <returns></returns>
        /*[Route("payment_out.gcl")]
        [Route("payment_out.jcl")]
        [HttpPost, HttpGet]
        public async Task<IActionResult> PaymentOut()
        {
            var paymentRequestResult = await paymentsService.HandlePaymentRequestAsync();

            switch (paymentRequestResult.Action)
            {
                case PaymentRequestActions.Redirect:
                    Response.Redirect(paymentRequestResult.ActionData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paymentRequestResult.Action), paymentRequestResult.Action.ToString());
            }

            return Content("", "text/html");
        }*/

        /// <summary>
        /// The route for handling payment updates from a PSP.
        /// </summary>
        /// <returns></returns>
        [Route("payment_in.gcl")]
        [Route("payment_in.jcl")]
        [HttpPost, HttpGet, IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentIn()
        {
            var statusUpdateResult = await paymentsService.HandleStatusUpdateAsync();

            return Content("", "text/html");
        }

#region Rabo OmniKassa specific endpoints
        /// <summary>
        /// The route for handling the return to the website from Rabo OmniKassa to redirect the user to the correct url.
        /// Rabo OmniKassa only provides one return url containing information about the status of the order.
        /// </summary>
        /// <param name="raboOmniKassaService">The <see cref="RaboOmniKassaService"/> to handle the request. (Is provided by dependency injection.)</param>
        /// <returns>Returns a <see cref="RedirectResult"/> to the correct url.</returns>
        [Route("rabo_omnikassa_return.gcl")]
        [HttpGet]
        public async Task<IActionResult> RaboOmniKassaReturnUrl([FromServices] RaboOmniKassaService raboOmniKassaService)
        {
            return Redirect(await raboOmniKassaService.GetRedirectUrlOnReturnFromPSP());
        }

        /// <summary>
        /// The route for handling the notifications that are provided by Rabo OmniKassa.
        /// </summary>
        /// <param name="raboOmniKassaService">The <see cref="RaboOmniKassaService"/> to handle the request. (Is provided by dependency injection.)</param>
        /// <returns></returns>
        [Route("rabo_omnikassa_webhook.gcl")]
        [HttpPost]
        public async Task<IActionResult> RaboOmniKassaWebhook([FromServices] RaboOmniKassaService raboOmniKassaService)
        {
            await raboOmniKassaService.HandleNotification();
            return Ok();
        }
#endregion
    }
}