using System;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    public class NoPspService : IPaymentServiceProviderService, IScopedService
    {
        /// <inheritdoc />
        public bool LogPaymentActions { get; set; }

        private readonly IObjectsService objectsService;

        public NoPspService(IObjectsService objectsService)
        {
            this.objectsService = objectsService;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethods paymentMethod, string invoiceNumber)
        {
            return new()
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL")
            };
        }

        /// <inheritdoc />
        public Task<StatusUpdateResult> ProcessStatusUpdateAsync()
        {
            // There is no payment_in call for "No PSP".
            throw new NotImplementedException();
        }
    }
}
