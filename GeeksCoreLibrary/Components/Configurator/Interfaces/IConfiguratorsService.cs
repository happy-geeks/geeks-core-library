using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Models;

namespace GeeksCoreLibrary.Components.Configurator.Interfaces
{
    public interface IConfiguratorsService
    {
        /// <summary>
        /// Get configurator data from cache or database.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<DataTable> GetConfiguratorDataAsync(string name);

        /// <summary>
        /// save configuration to database
        /// </summary>
        /// <param name="input"></param>
        /// <param name="parentId">Optional: If the configuration should be added as a child to something else, enter the ID of the parent here.</param>
        /// <returns></returns>
        Task<ulong> SaveConfigurationAsync(ConfigurationsModel input, ulong? parentId = null);

        /// <summary>
        /// Calculates the deliveryTime
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        Task<(string deliveryTime, string deliveryExtra)> GetDeliveryTimeAsync(ConfigurationsModel configuration);

        /// <summary>
        /// replace configurator items in template or query
        /// </summary>
        /// <param name="templateOrQuery"></param>
        /// <param name="configuration"></param>
        /// <param name="isQuery"></param>
        /// <returns></returns>
        Task<string> ReplaceConfiguratorItemsAsync(string templateOrQuery, ConfigurationsModel configuration, bool isQuery);

        /// <summary>
        ///  <para>Calculates the price and purchase price of a product.</para>
        ///  <para>Returns a <see cref="Tuple"/> where Item1 is the purchase price and Item2 is the customer price.</para>
        ///  </summary>
        ///  <param name="input"></param>
        ///  <returns>A <see cref="Tuple"/> where Item1 is the purchase price and Item2 is the customer price.</returns>
        Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(ConfigurationsModel input);
    }
}
