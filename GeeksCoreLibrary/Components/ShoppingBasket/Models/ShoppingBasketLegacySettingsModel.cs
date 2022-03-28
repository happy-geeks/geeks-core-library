using GeeksCoreLibrary.Core.Cms;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    public class ShoppingBasketLegacySettingsModel : CmsSettingsLegacy
    {
        public string CookieName { get; set; }

        public int AantalDagenCookie { get; set; }

        public string BasketEntityName { get; set; }

        public string BasketLineEntityName { get; set; }

        public string PropertynameQuantity { get; set; }

        public string PropertynameFactor { get; set; }

        public string PropertynamePrice { get; set; }

        public string PropertynameIncludesVat { get; set; }

        public string PropertynameVatrate { get; set; }

        public string PropertynameDiscount { get; set; }

        public string ExtraMainFieldsQuery { get; set; }

        public string ExtraLineFieldsQuery { get; set; }

        public string AddToBasketQuery { get; set; }

        public string TemplateJavaScript { get; set; }

        public string Template { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property was named this way in the JCL.")]
        // ReSharper disable once InconsistentNaming
        public string vatPercentageTemplate { get; set; }

        public string Header { get; set; }

        public string Footer { get; set; }

        public bool AlwaysShowHeaderAndFooter { get; set; }

        public bool MultipleBasketsPossible { get; set; }

        public string PrintTemplate { get; set; }

        public string EmailBody { get; set; }

        public string EmailSubject { get; set; }

        public string EmptyWinkelmandje { get; set; }

        public string GetBasketQuery { get; set; }

        public bool DeleteProductWhenAmmountIsZero { get; set; }

        public int MaxCountProduct { get; set; }

        public bool ClearBasketOnLoad { get; set; }

        public bool EmptyBasket { get; set; }

        public bool BasketLineValidityCheck { get; set; }

        public ShoppingBasketCmsSettingsModel ToSettingsModel()
        {
            return new()
            {
                HandleRequest = HandleRequest,
                EvaluateIfElseInTemplates = EvaluateIfElseInTemplates,
                RemoveUnknownVariables = RemoveUnknownVariables,

                Description = VisibleDescription,
                Template = Template,
                Header = Header,
                Footer = Footer,
                TemplatePrint = PrintTemplate,
                TemplateJavaScript = TemplateJavaScript,
                EmailBody = EmailBody,
                EmailSubject = EmailSubject,
                AlwaysRenderHeaderAndFooter = AlwaysShowHeaderAndFooter,
                MultipleBasketsPossible = MultipleBasketsPossible,

                CookieName = CookieName,
                CookieAgeInDays = AantalDagenCookie,

                BasketEntityName = BasketEntityName,
                BasketLineEntityName = BasketLineEntityName,
                QuantityPropertyName = PropertynameQuantity,
                FactorPropertyName = PropertynameFactor,
                PricePropertyName = PropertynamePrice,
                IncludesVatPropertyName = PropertynameIncludesVat,
                VatRatePropertyName = PropertynameVatrate,
                DiscountPropertyName = PropertynameDiscount,

                ExtraMainFieldsQuery = ExtraMainFieldsQuery,
                ExtraLineFieldsQuery = ExtraLineFieldsQuery,
                AddToBasketQuery = AddToBasketQuery,

                SqlQuery = SQLQuery,
                GetBasketQuery = GetBasketQuery,

                TemplateEmpty = EmptyWinkelmandje,
                VatPercentageTemplate = vatPercentageTemplate,
                RemoveItemWhenQuantityIsZero = DeleteProductWhenAmmountIsZero,
                MaxItemQuantity = MaxCountProduct,

                ClearContentsOnLoad = EmptyBasket,
                ResetOnLoad = ClearBasketOnLoad,
                BasketLineValidityCheck = BasketLineValidityCheck
            };
        }

        /// <summary>
        /// Converts a normal settings model to legacy.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public ShoppingBasketLegacySettingsModel FromSettingsModel(ShoppingBasketCmsSettingsModel settings)
        {
            return new()
            {
                VisibleDescription = settings.Description,
                Template = settings.Template,
                Header = settings.Header,
                Footer = settings.Footer,
                PrintTemplate = settings.TemplatePrint,
                TemplateJavaScript = settings.TemplateJavaScript,
                EmailBody = settings.EmailBody,
                EmailSubject = settings.EmailSubject,
                AlwaysShowHeaderAndFooter = settings.AlwaysRenderHeaderAndFooter,
                MultipleBasketsPossible = settings.MultipleBasketsPossible,

                CookieName = settings.CookieName,
                AantalDagenCookie = settings.CookieAgeInDays,

                BasketEntityName = settings.BasketEntityName,
                BasketLineEntityName = settings.BasketLineEntityName,
                PropertynameQuantity = settings.QuantityPropertyName,
                PropertynameFactor = settings.FactorPropertyName,
                PropertynamePrice = settings.PricePropertyName,
                PropertynameIncludesVat = settings.IncludesVatPropertyName,
                PropertynameVatrate = settings.VatRatePropertyName,
                PropertynameDiscount = settings.DiscountPropertyName,

                ExtraMainFieldsQuery = settings.ExtraMainFieldsQuery,
                ExtraLineFieldsQuery = settings.ExtraLineFieldsQuery,
                AddToBasketQuery = settings.AddToBasketQuery,

                SQLQuery = settings.SqlQuery,
                GetBasketQuery = settings.GetBasketQuery,

                EmptyWinkelmandje = settings.TemplateEmpty,
                vatPercentageTemplate = settings.VatPercentageTemplate,
                DeleteProductWhenAmmountIsZero = settings.RemoveItemWhenQuantityIsZero,
                MaxCountProduct = System.Convert.ToInt32(settings.MaxItemQuantity),

                EmptyBasket = settings.ClearContentsOnLoad,
                ClearBasketOnLoad = settings.ResetOnLoad,
                BasketLineValidityCheck = settings.BasketLineValidityCheck
            };
        }
    }
}
