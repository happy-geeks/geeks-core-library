using System;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
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
        public Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethod, string invoiceNumber)
        {
            return Task.FromResult<PaymentRequestResult>(new()
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = paymentMethod.PaymentServiceProvider.SuccessUrl
            });
        }

        /// <inheritdoc />
        public Task<StatusUpdateResult> ProcessStatusUpdateAsync()
        {
            // There is no payment_in call for "No PSP".
            throw new NotImplementedException();
        }
    }
}
