using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Models;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.OrderProcess.Interfaces
{
    /// <summary>
    /// Service for doing things for the OrderProcess component.
    /// </summary>
    public interface IOrderProcessesService
    {
        /// <summary>
        /// Gets order process settings.
        /// </summary>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <returns>An <see cref="OrderProcessSettingsModel"/> with the basic settings for the order process.</returns>
        Task<OrderProcessSettingsModel> GetOrderProcessSettingsAsync(ulong orderProcessId);
        
        /// <summary>
        /// Gets order process settings based on an URL path.
        /// </summary>
        /// <param name="fixedUrl">The path part of the URL.</param>
        /// <returns>An <see cref="OrderProcessSettingsModel"/> with the basic settings for the order process.</returns>
        Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(string fixedUrl);

        /// <summary>
        /// Gets order process settings based on an URL path.
        /// </summary>
        /// <param name="orderProcessesService">The <see cref="IOrderProcessesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetOrderProcessSettingsAsync() in this method.</param>
        /// <param name="fixedUrl">The path part of the URL.</param>
        /// <returns>An <see cref="OrderProcessSettingsModel"/> with the basic settings for the order process.</returns>
        Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(IOrderProcessesService orderProcessesService, string fixedUrl);

        /// <summary>
        /// Get all steps, groups and fields from order process settings in Wiser.
        /// </summary>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <returns>A list of <see cref="OrderProcessStepModel"/>.</returns>
        Task<List<OrderProcessStepModel>> GetAllStepsGroupsAndFieldsAsync(ulong orderProcessId);

        /// <summary>
        /// Get all payment methods from order process settings in Wiser.
        /// </summary>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <param name="loggedInUser">Optional: Enter the data of the user here if you only want to get payment methods of that user. If it is an anonymous user, create an empty model with userId = 0. If you want to get all payment methods, don't enter a value here.</param>
        /// <returns>A list of <see cref="PaymentMethodSettingsModel"/>.</returns>
        Task<List<PaymentMethodSettingsModel>> GetPaymentMethodsAsync(ulong orderProcessId, UserCookieDataModel loggedInUser = null);

        /// <summary>
        /// Get all payment methods from order process settings in Wiser.
        /// </summary>
        /// <param name="paymentMethodId">The Wiser item ID that contains the settings for the payment method.</param>
        /// <returns>A <see cref="PaymentMethodSettingsModel"/>.</returns>
        Task<PaymentMethodSettingsModel> GetPaymentMethodAsync(ulong paymentMethodId);
        
        /// <summary>
        /// Handles a request to start a new payment for a basket/checkout.
        /// </summary>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <returns>A <see cref="PaymentRequestResult"/> with information about whether the request was successful or not and what to do after.</returns>
        Task<PaymentRequestResult> HandlePaymentRequestAsync(ulong orderProcessId);
        
        /// <summary>
        /// Handles a request to start a new payment for a basket/checkout.
        /// </summary>
        /// <param name="orderProcessesService">The <see cref="IOrderProcessesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetOrderProcessSettingsAsync() in this method.</param>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <returns>A <see cref="PaymentRequestResult"/> with information about whether the request was successful or not and what to do after.</returns>
        Task<PaymentRequestResult> HandlePaymentRequestAsync(IOrderProcessesService orderProcessesService, ulong orderProcessId);

        /// <summary>
        /// Handles a status update (usually done via a webhook) of a payment via an PSP.
        /// </summary>
        /// <param name="orderProcessSettings">The settings for the order process that is being used.</param>
        /// <param name="conceptOrders">The (concept) orders of the user that the payment update is for.</param>
        /// <param name="newStatus">The new status of the payment.</param>
        /// <param name="isSuccessfulStatus">Whether or not the new status means that the payment was successful.</param>
        /// <param name="convertConceptOrderToOrder">Optional: Whether or not to convert the concept order(s) to order(s). Default value is <see langword="true"/>.</param>
        Task<bool> HandlePaymentStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true);

        /// <summary>
        /// Handles a status update (usually done via a webhook) of a payment via an PSP.
        /// </summary>
        /// <param name="orderProcessesService">The <see cref="IOrderProcessesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetOrderProcessSettingsAsync() in this method.</param>
        /// <param name="orderProcessSettings">The settings for the order process that is being used.</param>
        /// <param name="conceptOrders">The (concept) orders of the user that the payment update is for.</param>
        /// <param name="newStatus">The new status of the payment.</param>
        /// <param name="isSuccessfulStatus">Whether or not the new status means that the payment was successful.</param>
        /// <param name="convertConceptOrderToOrder">Optional: Whether or not to convert the concept order(s) to order(s). Default value is <see langword="true"/>.</param>
        Task<bool> HandlePaymentStatusUpdateAsync(IOrderProcessesService orderProcessesService, OrderProcessSettingsModel orderProcessSettings, ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true);
    }
}
