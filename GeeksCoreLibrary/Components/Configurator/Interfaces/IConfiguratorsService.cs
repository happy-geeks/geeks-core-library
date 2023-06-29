using System;
using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Models;

namespace GeeksCoreLibrary.Components.Configurator.Interfaces;

public interface IConfiguratorsService
{
    /// <summary>
    /// Get configurator data from cache or database.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<DataTable> GetConfiguratorDataAsync(string name);

    /// <summary>
    /// Get configurator data as used for the Vue configurator.
    /// </summary>
    /// <param name="name">The name of the configurator.</param>
    /// <param name="includeStepsData">Optional: Whether the data for the steps should also be retrieved. Default value is true.</param>
    /// <returns>A <see cref="DataTable"/> object.</returns>
    Task<VueConfiguratorDataModel> GetVueConfiguratorDataAsync(string name, bool includeStepsData = true);

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
    /// Replace configurator items in a template or query.
    /// </summary>
    /// <param name="templateOrQuery">The template that will be updated.</param>
    /// <param name="configuration">A <see cref="ConfigurationsModel"/> object.</param>
    /// <param name="isQuery">Whether the </param>
    /// <returns>The <paramref name="templateOrQuery"/> with any items from the configuration replaced.</returns>
    Task<string> ReplaceConfiguratorItemsAsync(string templateOrQuery, ConfigurationsModel configuration, bool isQuery);

    /// <summary>
    /// Replace configurator items in a template. This method is meant for the Vue version of the configurator.
    /// </summary>
    /// <param name="template">The template that will be updated.</param>
    /// <param name="configuration">A <see cref="VueConfigurationsModel"/> object.</param>
    /// <param name="isDataQuery">Whether the <paramref name="template"/> is a data query.</param>
    /// <returns>The <paramref name="template"/> with any items from the configuration replaced.</returns>
    Task<string> ReplaceConfiguratorItemsAsync(string template, VueConfigurationsModel configuration, bool isDataQuery);

    /// <summary>
    /// Start the configuration at an external API.
    /// </summary>
    /// <param name="vueConfiguration">TA <see cref="VueConfigurationsModel"/> object.</param>
    /// <returns>The ID of the configuration in the external API.</returns>
    Task<string> StartConfigurationExternallyAsync(VueConfigurationsModel vueConfiguration);
    
    /// <summary>
    /// <para>Calculates the price and purchase price of a product.</para>
    /// <para>Returns a <see cref="Tuple"/> where Item1 is the purchase price, Item2 is the customer price, and Item3 is the from price.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A <see cref="Tuple"/> where Item1 is the purchase price, Item2 is the customer price, and Item3 is the from price.</returns>
    Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(ConfigurationsModel input);

    /// <summary>
    /// <para>Calculates the price and purchase price of a product.</para>
    /// <para>Returns a <see cref="Tuple"/> where Item1 is the purchase price, Item2 is the customer price, and Item3 is the from price.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A <see cref="Tuple"/> where Item1 is the purchase price, Item2 is the customer price, and Item3 is the from price.</returns>
    Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(VueConfigurationsModel input);
}