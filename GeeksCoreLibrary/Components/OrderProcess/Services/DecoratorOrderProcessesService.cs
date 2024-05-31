using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Models;
using GeeksCoreLibrary.Components.OrderProcess.Enums;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.OrderProcess.Services;

/// <summary>
/// An abstract decorator for the default <see cref="OrderProcessesService"/>.
/// Create a new class that inherits this class to add custom code to the order process in your project.
/// This way you only need to overwrite the functions that you want to add custom code,
/// instead of having to implement all functions if you would directly inherit from <see cref="IOrderProcessesService"/>.
/// Don't forget to add your override as a decorator in the startup class!
/// </summary>
public abstract class DecoratorOrderProcessesService : IOrderProcessesService
{
    private readonly IOrderProcessesService orderProcessesService;

    /// <summary>
    /// Creates a new instance of <see cref="DecoratorOrderProcessesService"/>.
    /// </summary>
    protected DecoratorOrderProcessesService(IOrderProcessesService orderProcessesService)
    {
        this.orderProcessesService = orderProcessesService;
    }

    /// <inheritdoc />
    public virtual async Task<OrderProcessSettingsModel> GetOrderProcessSettingsAsync(ulong orderProcessId)
    {
        return await orderProcessesService.GetOrderProcessSettingsAsync(orderProcessId);
    }

    /// <inheritdoc />
    public virtual async Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(string fixedUrl)
    {
        return await orderProcessesService.GetOrderProcessViaFixedUrlAsync(this, fixedUrl);
    }

    /// <inheritdoc />
    public virtual async Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(IOrderProcessesService service, string fixedUrl)
    {
        return await orderProcessesService.GetOrderProcessViaFixedUrlAsync(service, fixedUrl);
    }

    /// <inheritdoc />
    public virtual async Task<List<OrderProcessStepModel>> GetAllStepsGroupsAndFieldsAsync(ulong orderProcessId)
    {
        return await orderProcessesService.GetAllStepsGroupsAndFieldsAsync(orderProcessId);
    }

    /// <inheritdoc />
    public virtual async Task<List<PaymentMethodSettingsModel>> GetPaymentMethodsAsync(ulong orderProcessId, UserCookieDataModel loggedInUser = null)
    {
        return await orderProcessesService.GetPaymentMethodsAsync(orderProcessId, loggedInUser);
    }

    /// <inheritdoc />
    public virtual async Task<PaymentMethodSettingsModel> GetPaymentMethodAsync(ulong paymentMethodId)
    {
        return await orderProcessesService.GetPaymentMethodAsync(paymentMethodId);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ValidateFieldValueAsync(OrderProcessFieldModel field, List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> currentItems)
    {
        return await orderProcessesService.ValidateFieldValueAsync(field, currentItems);
    }

    /// <inheritdoc />
    public virtual async Task<PaymentRequestResult> HandlePaymentRequestAsync(ulong orderProcessId)
    {
        return await orderProcessesService.HandlePaymentRequestAsync(this, orderProcessId);
    }

    /// <inheritdoc />
    public virtual async Task<PaymentRequestResult> HandlePaymentRequestAsync(IOrderProcessesService service, ulong orderProcessId)
    {
        return await orderProcessesService.HandlePaymentRequestAsync(service, orderProcessId);
    }
    
    /// <inheritdoc />
    public virtual async Task<PaymentRequestResult> HandlePaymentRequestAsync(IOrderProcessesService service, ulong orderProcessId, string failUrl, string successUrl, string pendingUrl, OrderProcessBasketToConceptOrderMethods basketToConceptOrderMethod, OrderProcessSettingsModel orderProcessSettings)
    {
        return await orderProcessesService.HandlePaymentRequestAsync(service, orderProcessId, failUrl, successUrl, pendingUrl, basketToConceptOrderMethod, orderProcessSettings);
    }

    /// <inheritdoc />
    public virtual async Task<bool> HandlePaymentStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true)
    {
        return await orderProcessesService.HandlePaymentStatusUpdateAsync(this, orderProcessSettings, conceptOrders, newStatus, isSuccessfulStatus, convertConceptOrderToOrder);
    }

    /// <inheritdoc />
    public virtual async Task<bool> HandlePaymentStatusUpdateAsync(IOrderProcessesService service, OrderProcessSettingsModel orderProcessSettings, ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true)
    {
        return await orderProcessesService.HandlePaymentStatusUpdateAsync(service, orderProcessSettings, conceptOrders, newStatus, isSuccessfulStatus, convertConceptOrderToOrder);
    }

    /// <inheritdoc />
    public virtual async Task<bool> HandlePaymentServiceProviderWebhookAsync(ulong orderProcessId, ulong paymentMethodId)
    {
        return await orderProcessesService.HandlePaymentServiceProviderWebhookAsync(this, orderProcessId, paymentMethodId);
    }

    /// <inheritdoc />
    public virtual async Task<bool> HandlePaymentServiceProviderWebhookAsync(IOrderProcessesService service, ulong orderProcessId, ulong paymentMethodId)
    {
        return await orderProcessesService.HandlePaymentServiceProviderWebhookAsync(service, orderProcessId, paymentMethodId);
    }

    /// <inheritdoc />
    public virtual async Task<PaymentReturnResult> HandlePaymentReturnAsync(ulong orderProcessId, ulong paymentMethodId)
    {
        return await orderProcessesService.HandlePaymentReturnAsync(this, orderProcessId, paymentMethodId);
    }

    /// <inheritdoc />
    public virtual async Task<PaymentReturnResult> HandlePaymentReturnAsync(IOrderProcessesService service, ulong orderProcessId, ulong paymentMethodId)
    {
        return await orderProcessesService.HandlePaymentReturnAsync(service, orderProcessId, paymentMethodId);
    }
    
    /// <inheritdoc />
    public virtual async Task<PaymentReturnResult> HandlePaymentReturnAsync(IOrderProcessesService service, ulong orderProcessId, ulong paymentMethodId, string failUrl, string successUrl, string pendingUrl, OrderProcessSettingsModel orderProcessSettings)
    {
        return await orderProcessesService.HandlePaymentReturnAsync(service, orderProcessId, paymentMethodId, failUrl, successUrl, pendingUrl, orderProcessSettings);
    }

    /// <inheritdoc />
    public virtual async Task<WiserItemFileModel> GetInvoicePdfAsync(ulong orderId)
    {
        return await orderProcessesService.GetInvoicePdfAsync(orderId);
    }

    /// <inheritdoc />
    public virtual async Task<PaymentRequestResult> PaymentRequestBeforeOutAsync(List<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
    {
        return await orderProcessesService.PaymentRequestBeforeOutAsync(conceptOrders, orderProcessSettings, paymentMethodSettings);
    }
    
    /// <inheritdoc />
    public virtual async Task<PaymentRequestResult> PaymentRequestBeforeOutAsync(List<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, PaymentMethodSettingsModel paymentMethodSettings)
    {
        return await orderProcessesService.PaymentRequestBeforeOutAsync(conceptOrders, paymentMethodSettings);
    }

    /// <inheritdoc />
    public virtual async Task<bool> PaymentStatusUpdateBeforeCommunicationAsync(WiserItemModel main, List<WiserItemModel> lines, OrderProcessSettingsModel orderProcessSettings, bool wasHandledBefore, bool isSuccessfulStatus)
    {
        return await orderProcessesService.PaymentStatusUpdateBeforeCommunicationAsync(main, lines, orderProcessSettings, wasHandledBefore, isSuccessfulStatus);
    }
    
    /// <inheritdoc />
    public virtual async Task<bool> PaymentStatusUpdateBeforeCommunicationAsync(WiserItemModel main, List<WiserItemModel> lines, bool wasHandledBefore, bool isSuccessfulStatus)
    {
        return await orderProcessesService.PaymentStatusUpdateBeforeCommunicationAsync(main, lines, wasHandledBefore, isSuccessfulStatus);
    }
}