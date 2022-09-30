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
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class NoPspService : PaymentServiceProviderBaseService, IPaymentServiceProviderService, IScopedService
    {
        /// <inheritdoc />
        public bool LogPaymentActions { get; set; }

        public NoPspService(IDatabaseHelpersService databaseHelpersService, IDatabaseConnection databaseConnection, ILogger<NoPspService> logger, IHttpContextAccessor httpContextAccessor) : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
        {
        }

        /// <inheritdoc />
        public Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethodSettings, string invoiceNumber)
        {
            return Task.FromResult(new PaymentRequestResult
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = paymentMethodSettings.PaymentServiceProvider.SuccessUrl
            });
        }

        /// <inheritdoc />
        public Task<StatusUpdateResult> ProcessStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
        {
            // There is no payment_in call for "No PSP".
            throw new NotImplementedException();
        }
    }
}
