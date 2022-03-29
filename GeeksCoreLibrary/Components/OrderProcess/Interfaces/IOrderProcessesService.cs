using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;

namespace GeeksCoreLibrary.Components.OrderProcess.Interfaces
{
    /// <summary>
    /// Service for doing things for the OrderProcess component.
    /// </summary>
    public interface IOrderProcessesService
    {
        /// <summary>
        /// Gets order process data based on an URL path.
        /// </summary>
        /// <param name="fixedUrl">The path part of the URL.</param>
        /// <returns>An <see cref="OrderProcessSettingsModel"/> with the basic settings for the order process.</returns>
        Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(string fixedUrl);

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
        /// <returns>A list of <see cref="PaymentMethodSettingsModel"/>.</returns>
        Task<List<PaymentMethodSettingsModel>> GetPaymentMethodsAsync(ulong orderProcessId);
    }
}
