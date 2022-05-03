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
        /// Validates whether a value for a field is valid.
        /// This checks if the field is mandatory, if the regex pattern matches and if the value is valid for the type of field (eg if an email field contains a valid e-mail address).
        /// </summary>
        /// <param name="field">The settings for the field.</param>
        /// <param name="currentItems">Any items that the user already has in the database. This will be used to make sure that the user won't get an error if they enter the same value that is already saved for their own item. For example, if the user is logged in they will have an account item, add that item to this list.</param>
        /// <returns>A <see cref="bool"/> indicating whether the value is valid or not.</returns>
        Task<bool> ValidateFieldValueAsync(OrderProcessFieldModel field, List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> currentItems);
        
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

        /// <summary>
        /// Handles the webhook of a PSP for payment status updates.
        /// </summary>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <param name="paymentMethodId">The Wiser item ID that contains the settings for the payment method that the user selected during the checkout.</param>
        Task<bool> HandlePaymentServiceProviderWebhookAsync(ulong orderProcessId, ulong paymentMethodId);

        /// <summary>
        /// Handles the webhook of a PSP for payment status updates.
        /// </summary>
        /// <param name="orderProcessesService">The <see cref="IOrderProcessesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetOrderProcessSettingsAsync() in this method.</param>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <param name="paymentMethodId">The Wiser item ID that contains the settings for the payment method that the user selected during the checkout.</param>
        Task<bool> HandlePaymentServiceProviderWebhookAsync(IOrderProcessesService orderProcessesService, ulong orderProcessId, ulong paymentMethodId);
        
        /// <summary>
        /// Determines what to do after a user is returned to the web shop after a payment.
        /// This is used for payment service providers that do not offer specific return URLs for multiple states, like successful state, error state, cancel state, etc.
        /// </summary>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <param name="paymentMethodId">The Wiser item ID that contains the settings for the payment method that the user selected during the checkout.</param>
        Task<PaymentReturnResult> HandlePaymentReturnAsync(ulong orderProcessId, ulong paymentMethodId);
        
        /// <summary>
        /// Determines what to do after a user is returned to the web shop after a payment.
        /// This is used for payment service providers that do not offer specific return URLs for multiple states, like successful state, error state, cancel state, etc.
        /// </summary>
        /// <param name="orderProcessesService">The <see cref="IOrderProcessesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetOrderProcessSettingsAsync() in this method.</param>
        /// <param name="orderProcessId">The Wiser item ID that contains the settings for the order process.</param>
        /// <param name="paymentMethodId">The Wiser item ID that contains the settings for the payment method that the user selected during the checkout.</param>
        Task<PaymentReturnResult> HandlePaymentReturnAsync(IOrderProcessesService orderProcessesService, ulong orderProcessId, ulong paymentMethodId);
    }
}
