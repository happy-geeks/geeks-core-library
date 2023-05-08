using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Models;
using Mollie.Api.Models.Order;

namespace GeeksCoreLibrary.Modules.Payments.Helpers.Mollie;

public interface IOrderRequestBuilder
{
    Task<OrderRequest> CreateOrderRequestAsync(
        string invoiceNumber,
        ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets,
        WiserItemModel userDetails, 
        MollieSettingsModel mollieSettingsModel, 
        PaymentMethodSettingsModel paymentMethodSettingsModel);
}