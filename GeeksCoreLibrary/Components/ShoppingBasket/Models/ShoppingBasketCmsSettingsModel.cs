using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using System.ComponentModel;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    public class ShoppingBasketCmsSettingsModel : CmsSettings
    {
        public ShoppingBasket.ComponentModes ComponentMode { get; set; } = ShoppingBasket.ComponentModes.Render;

        #region Tab Layout properties

        /// <summary>
        /// The main HTML template for the selected mode.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template",
            Description = "The main HTML template for the selected mode.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 10
        )]
        public string Template { get; set; }

        [CmsProperty(
            PrettyName = "Header",
            Description = "The HTML that will be placed on the top of the basket.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 20
        )]
        public string Header { get; set; }

        [CmsProperty(
            PrettyName = "Footer",
            Description = "The HTML that will be placed at the bottom of the basket.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 30
        )]
        public string Footer { get; set; }

        [CmsProperty(
            PrettyName = "VAT percentage template",
            Description = "The HTML template to use to show the vat percentage in the shopping basket.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 40
        )]
        public string VatPercentageTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Template print",
            Description = "The HTML template used for printing.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 50
        )]
        public string TemplatePrint { get; set; }

        [CmsProperty(
            PrettyName = "Template JavaScript",
            Description = "If this component requires any JavaScript, you can write that here.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 60
        )]
        public string TemplateJavaScript { get; set; }

        [CmsProperty(
            PrettyName = "Always render header and footer",
            Description = "Whether the header and footer should also be rendered on empty templates.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            DisplayOrder = 70
        )]
        public bool AlwaysRenderHeaderAndFooter { get; set; }

        /// <summary>
        /// The main HTML template for the selected mode.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template empty",
            Description = "The HTML template for an empty shopping basket.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 10
        )]
        public string TemplateEmpty { get; set; }

        [CmsProperty(
            PrettyName = "Email body",
            Description = "The e-mail body when sending the basket by mail. When the basket will be send as attachment, then use PrintTemplate for the attachment.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 90
        )]
        public string EmailBody { get; set; }

        [CmsProperty(
            PrettyName = "Email subject",
            Description = "The e-mail subject when sending the basket by mail.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextBox,
            DisplayOrder = 100
        )]
        public string EmailSubject { get; set; }

        #endregion

        #region Tab Developer properties

        [CmsProperty(
            PrettyName = "Cookie name",
            Description = "The name of the cookie that will store the encrypted shopping basket item ID.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.SessionCookie,
            DisplayOrder = 10
        )]
        public string CookieName { get; set; }

        [CmsProperty(
            PrettyName = "Cookie age in days",
            Description = "The amount of days that the cookie should be valid.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.SessionCookie,
            DisplayOrder = 20
        )]
        public int CookieAgeInDays { get; set; }

        [CmsProperty(
            PrettyName = "Max item quantity",
            Description = "The maximum quantity any item is allowed to have in the shopping basket.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 10
        )]
        public decimal MaxItemQuantity { get; set; }

        #endregion

        #region Tab DataSource properties

        [CmsProperty(
            PrettyName = "Quantity property name",
            Description = "The name of the property for the basket line entity which is used for the quantity.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 50
        )]
        public string QuantityPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "Factor property name",
            Description = "The name of the property for the basket line entity which is used for the factor.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 60
        )]
        public string FactorPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "Price property name",
            Description = "The name of the property for the basket line entity which is used for the price.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 70
        )]
        public string PricePropertyName { get; set; }

        [CmsProperty(
            PrettyName = "Includes VAT property name",
            Description = "The name of the property for the basket line entity which is used for determining if VAT is included.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 80
        )]
        public string IncludesVatPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "VAT rate property name",
            Description = "The name of the property for the basket line entity which is used for the VAT rate.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 90
        )]
        public string VatRatePropertyName { get; set; }

        [CmsProperty(
            PrettyName = "Discount property name",
            Description = "The name of the property for the basket line entity which is used for the discount.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 100
        )]
        public string DiscountPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "Country property name",
            Description = "The name of the property for the basket line entity which is used for the country.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 110
        )]
        public string CountryPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "B2B property name",
            Description = "The name of the property for the basket line entity which is used for B2B.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 120
        )]
        public string B2BPropertyName { get; set; }

        [DefaultValue(false), CmsProperty(
            PrettyName = "Multiple baskets possible",
            Description = "Enable the option to have multiple baskets using the same cookie name.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 130
        )]
        public bool MultipleBasketsPossible { get; set; }
        
        [CmsProperty(
            PrettyName = "Item excluded from discount property name",
            Description = "Items that have this property set to 1 are excluded from discount calculation.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 140
        )]
        public string ItemExcludedFromDiscountPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "SQL query",
            Description = "The query used for getting data for a product.",
            DeveloperRemarks = "The variable '{itemid}' can be used for the ID of the item whose information should be retrieved.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 10
        )]
        public string SqlQuery { get; set; }

        [CmsProperty(
            PrettyName = "Get basket query",
            Description = "The query used for retrieving a basket. The query should always return an item ID. If the query returns no result, the default method of loading a basket will be used.",
            DeveloperRemarks = "This query will only be used when MultipleBasketsPossible is enabled.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 20
        )]
        public string GetBasketQuery { get; set; }

        [CmsProperty(
            PrettyName = "Extra main fields query",
            Description = "A query to get extra fields on shopping basket level, next to the fields that already exist with the shopping cart.",
            DeveloperRemarks = "All details of the basket can be used as variable in the query. Use {id} for the item-id of the basket. Select columns 'key' and 'value', optional columns are 'id' and 'readonly'. Readonly is default true, so extra fields are not saved to the basket. Use the column id to get extra details on basket and basket-line level in one query. Select the id of the basket or a basket line to set details of the basket or the basket line.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Advanced,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 10
        )]
        public string ExtraMainFieldsQuery { get; set; }

        [CmsProperty(
            PrettyName = "Extra line fields query",
            Description = "A query to get extra fields on line level, next to the fields that already exist with the shopping cart lines.",
            DeveloperRemarks = "All details of the basket can be used as variable in the query. Use {id} for the item-id of the basket. Use {linktype} for the type number of the link between basket and basketline. Select columns 'id', 'key' and 'value', optional column is 'readonly'. Readonly is default true, so extra fields are not saved to the basket. Select the id of the basket line into the column 'id'.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Advanced,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 20
        )]
        public string ExtraLineFieldsQuery { get; set; }

        [CmsProperty(
            PrettyName = "Add to basket query",
            Description = "A query that can be executed after an item is added to the basket. This query does not do anything with the basket contents, it only gets executed.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Advanced,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 30
        )]
        public string AddToBasketQuery { get; set; }

        #endregion

        #region Tab Behavior properties

        [DefaultValue(true), CmsProperty(
            PrettyName = "Remove item when quantity is zero",
            Description = "Will ensure items will be removed from the basket when the quantity reaches 0.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 10
        )]
        public bool RemoveItemWhenQuantityIsZero { get; set; }

        [DefaultValue(false), CmsProperty(
            PrettyName = "Basket line Custom Action",
            Description = "When enabled, the query template with the name 'BasketLineStockAction' will be executed. This query is executed once for a basket.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 20
        )]
        public bool BasketLineStockAction { get; set; }

        [DefaultValue(false), CmsProperty(
            PrettyName = "Add basket as PDF attachment",
            Description = "The basket will be converted to a PDF and added to the email as an attachment.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            DisplayOrder = 10
        )]
        public bool AddBasketAsPdfAttachment { get; set; }

        [DefaultValue(false), CmsProperty(
            PrettyName = "Basket line Validity Check",
            Description = "When enabled, the query template with the name 'BasketLineValidityCheck' will be executed. All lines returned by this query will be removed by the basket.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Validation,
            DisplayOrder = 10
        )]
        public bool BasketLineValidityCheck { get; set; }

        #endregion

        #region Properties for Legacy mode

        /// <summary>
        /// Gets or sets whether the basket should be cleared. This is different than <see cref="ClearContentsOnLoad"/> in that this will create a completely new basket instead of only clearing its contents.
        /// </summary>
        /// <remarks>
        /// This property is only for use with <see cref="ComponentMode"/> set to <see cref="ShoppingBasket.ComponentModes.Legacy"/>.
        /// </remarks>
        [CmsProperty(HideInCms = true)]
        public bool ResetOnLoad { get; set; }

        /// <summary>
        /// Gets or sets whether the basket should be cleared. This is different than <see cref="ResetOnLoad"/> in that this will only clear the basket's contents instead of creating a completely new basket.
        /// </summary>
        /// <remarks>
        /// This property is only for use with <see cref="ComponentMode"/> set to <see cref="ShoppingBasket.ComponentModes.Legacy"/>.
        /// </remarks>
        [CmsProperty(HideInCms = true)]
        public bool ClearContentsOnLoad { get; set; }

        #endregion
    }
}
