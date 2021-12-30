using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Cms;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class ConfiguratorLegacySettingsModel : CmsSettingsLegacy
    {
        public string ConfiguratorName { get; set; }
        public bool ValuesCanContainDashes { get; set; }
        public string MainConfiguratorDataQuery { get; set; }
        public string ProductCategoriesQuery { get; set; }
        public string ProductsQuery { get; set; }
        public string ProductVariantsQuery { get; set; }
        public string ConnectedProductsOnProductQuery { get; set; }
        public string ConnectedProductsOnCategoryQuery { get; set; }
        public string ProductsApiBaseUrl { get; set; }
        public string ProductsApiGetProductsUrl { get; set; }
        public string ProductsApiSalesPriceProperty { get; set; }
        public string ProductsApiPurchasePriceProperty { get; set; }
        public string ProductsApiFromPriceProperty { get; set; }
        public string MainStepHtml { get; set; }
        public string StepHtml { get; set; }
        public string SubStepHtml { get; set; }
        public string SummaryHtml { get; set; }
        public string FinalSummaryHtml { get; set; }
        public string MobilePreProgressHtml { get; set; }
        public string MobilePostProgressHtml { get; set; }
        public ConfiguratorCmsSettingsModel ToSettingsModel()
        {

            return new ConfiguratorCmsSettingsModel()
            {
                MainConfiguratorDataQuery = this.MainConfiguratorDataQuery,
                ConfiguratorName = this.ConfiguratorName,
                ConnectedProductsOnCategoryQuery = this.ConnectedProductsOnCategoryQuery,
                ConnectedProductsOnProductQuery = this.ConnectedProductsOnProductQuery,
                ComponentMode = Configurator.ComponentModes.Default,
                Description = this.VisibleDescription,
                EvaluateIfElseInTemplates = this.EvaluateIfElseInTemplates,
                FinalSummaryHtml = this.FinalSummaryHtml,
                HandleRequest = this.HandleRequest,
                Html = this.Html,
                MainStepHtml = this.MainStepHtml,
                MobilePostProgressHtml = this.MobilePostProgressHtml,
                MobilePreProgressHtml = this.MobilePreProgressHtml,
                ProductCategoriesQuery = this.ProductCategoriesQuery,
                ProductVariantsQuery = this.ProductVariantsQuery,
                ProductsApiBaseUrl = this.ProductsApiBaseUrl,
                ProductsApiFromPriceProperty = this.ProductsApiFromPriceProperty,
                ProductsApiGetProductsUrl = this.ProductsApiGetProductsUrl,
                ProductsApiPurchasePriceProperty = this.ProductsApiPurchasePriceProperty,
                ProductsApiSalesPriceProperty = this.ProductsApiSalesPriceProperty,
                ProductsQuery = this.ProductsQuery,
                RemoveUnknownVariables = this.RemoveUnknownVariables,
                StepHtml = this.StepHtml,
                SubStepHtml = this.SubStepHtml,
                SummaryHtml = this.SummaryHtml,
                ValuesCanContainDashes = this.ValuesCanContainDashes,
                UserNeedsToBeLoggedIn = this.UserNeedsToBeLoggedIn
            };
        }

        public ConfiguratorLegacySettingsModel FromSettingModel(ConfiguratorCmsSettingsModel settings)
        {
            return this;
        }
    }
}
