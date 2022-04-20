using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class ConfiguratorCmsSettingsModel : CmsSettings
    {
        #region Tab Behavior properties
        
        /// <summary>
        /// <para>The mode this component should be in:</para>
        /// <para><see cref="Configurator.ComponentModes.Default"/>: A default configurator with main steps, steps, substeps and a summary.</para>
        /// </summary>
        [CmsProperty(Description = @"The mode this component should be in: 
            <ul>
                <li><strong>Default:</strong> A default configurator with main steps, steps, substeps and a summary.</li>
            </ul>")]
        public Configurator.ComponentModes ComponentMode { get; set; } = Configurator.ComponentModes.Default;

        #endregion
        
        #region Tab DataSource properties
        
        [CmsProperty(PrettyName = "Configurator Name",
                     Description = "The name of the configurator to render.",
                     DeveloperRemarks = "It's possible to use request variables here.",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.Common,
                     DisplayOrder = 10
        )]
        public string ConfiguratorName { get; set; }

        /// <summary>

        /// The name of the configurator to render.

        /// </summary>
        [CmsProperty(PrettyName = "Values can contain dashes",
                     Description = "By default all values will be split by '-'. Enable this option if you don't want that.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.Common,
                     DisplayOrder = 20
        )]
        public bool ValuesCanContainDashes { get; set; }

        /// <summary>
        /// The query that retreived the basic data for the configurator, such as the templates for all steps.
        /// If you want to customise this, make sure that your query uses the same column names and parameter names.
        /// </summary>
        [CmsProperty(PrettyName = "Main Configurator Data Query",
                     Description = "The query that retreives the basic data for the configurator, such as the templates for all steps.",
                     DeveloperRemarks = "If you want to customise this, make sure that your query uses the same column names and parameter names.",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.CustomSql,
                     TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
                     DisplayOrder = 10
        )]
        public string MainConfiguratorDataQuery { get; set; }

        /// <summary>
        /// The query that retreives the data from product categories.
        /// Use the variable '?connectedId' to get categories from a parent.
        /// </summary>
        [CmsProperty(PrettyName = "Product Categories Query",
                     Description = "The query that retreives the data from product categories.",
                     DeveloperRemarks = "Use the variable '?connectedId' to get categories from a parent.",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.CustomSql,
                     TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
                     DisplayOrder = 20
        )]
        public string ProductCategoriesQuery { get; set; }

        /// <summary>
        /// The query that retreives the data from products.
        /// Use the variable '?connectedId' to get products from a category.
        /// </summary>
        [CmsProperty(PrettyName = "Products Query",
                     Description = "The query that retreives the data from products.",
                     DeveloperRemarks = "Use the variable '?connectedId' to get products from a category.",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.CustomSql,
                     TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
                     DisplayOrder = 30
        )]
        public string ProductsQuery { get; set; }

        /// <summary>
        /// The query that retreives the data from product variants.
        /// Use the variable '{connectedId}' to get variants of a product.
        /// </summary>
        [CmsProperty(PrettyName = "Product Variants Query",
                     Description = "The query that retreives the data from product variants.",
                     DeveloperRemarks = "Use the variable '?connectedId' to get variants of a product.",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.CustomSql,
                     TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
                     DisplayOrder = 40
        )]
        public string ProductVariantsQuery { get; set; }

        /// <summary>
        /// The query that retreives the data from products that are connected to the current product.
        /// Use the variable '?connectedId' to get coupled products and the variable '?connectedType' to specify the connection type.
        /// </summary>
        [CmsProperty(PrettyName = "Connected Products On Product Query",
                     Description = "The query that retreives the data from products that are connected to the current product.",
                     DeveloperRemarks = "Use the variable '?connectedId' to get coupled products and the variable '?connectedType' to specify the connection type.",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.CustomSql,
                     TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
                     DisplayOrder = 50
        )]
        public string ConnectedProductsOnProductQuery { get; set; }

        /// <summary>
        /// The query that retrieves the data from products that are connected to the current category.
        /// Use the variable '?connectedId' to get coupled products and the variable '?connectedType' to specify the connection type.
        /// </summary>
        [CmsProperty(PrettyName = "Connected Products On Category Query",
                     Description = "The query that retrieves the data from products that are connected to the current category.",
                     DeveloperRemarks = "Use the variable '?connectedId' to get coupled products and the variable '?connectedType' to specify the connection type.",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.CustomSql,
                     TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
                     DisplayOrder = 60
        )]
        public string ConnectedProductsOnCategoryQuery { get; set; }

        /// <summary>
        /// If there is an (external) API for retrieving information about standard products, enter that URL here.
        /// </summary>
        [CmsProperty(PrettyName = "Products API base URL",
                     Description = "If there is an (external) API for retrieving information about standard products, enter that URL here.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.DataSource, GroupName = CmsAttributes.CmsGroupName.Json,
                     DisplayOrder = 10
        )]
        public string ProductsApiBaseUrl { get; set; }

        /// <summary>
        /// If there is an (external) API for retrieving information about standard products, enter the URL part for retrieving products here.
        /// </summary>
        [CmsProperty(PrettyName = "Products API get products url",
                     Description = "If there is an (external) API for retrieving information about standard products, enter the URL part for retrieving products here.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.Json,
                     DisplayOrder = 20
        )]
        public string ProductsApiGetProductsUrl { get; set; }

        /// <summary>
        /// If there is an (external) API for retrieving information about standard products, enter the name of the property that contains the sales price of a product.
        /// </summary>
        [CmsProperty(PrettyName = "Products API sales price property",
                     Description = "If there is an (external) API for retrieving information about standard products, enter the name of the property that contains the sales price of a product.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.Json,
                     DisplayOrder = 30
        )]
        public string ProductsApiSalesPriceProperty { get; set; }

        /// <summary>
        /// If there is an (external) API for retrieving information about standard products, enter the name of the property that contains the sales price of a product.
        /// </summary>
        [CmsProperty(PrettyName = "Products API purchase price property",
                     Description = "If there is an (external) API for retrieving information about standard products, enter the name of the property that contains the purchase price of a product.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.Json,
                     DisplayOrder = 40
        )]
        public string ProductsApiPurchasePriceProperty { get; set; }

        /// <summary>
        /// If there is an (external) API for retrieving information about standard products, enter the name of the property that contains the sales price of a product.
        /// </summary>
        [CmsProperty(PrettyName = "Products API from price property",
                     Description = "If there is an (external) API for retrieving information about standard products, enter the name of the property that contains the from price of a product.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.DataSource,
                     GroupName = CmsAttributes.CmsGroupName.Json,
                     DisplayOrder = 50
        )]
        public string ProductsApiFromPriceProperty { get; set; }

        #endregion
        
        #region Tab Layout properties
        
        /// <summary>
        ///  The HTML for a main step.
        ///  </summary>
        [CmsProperty(PrettyName = "Main Step HTML",
                     Description = "The HTML for a main step.",
                     DeveloperRemarks = @"You can use the custom variables '{mainStepContent}', '{mainStepCount}', '{currentMainStepName}' and '{contentId}' here.<br />
            ContentId is the ID of the dynamic content component in Wiser, you need to use that in all AJAX calls to jclcomponent.jcl.",
                     TabName = CmsAttributes.CmsTabName.Layout,
                     GroupName = CmsAttributes.CmsGroupName.Templates,
                     TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
                     DisplayOrder = 10
        )]
        public string MainStepHtml { get; set; }

        /// <summary>
        ///  The HTML for a step.
        ///  </summary>
        [CmsProperty(PrettyName = "Step HTML",
                     Description = "The HTML for a step.",
                     DeveloperRemarks = "You can use the custom variables '{stepContent}', '{style}', '{isrequired}', '{stepname}', '{mainStepNumber}', '{stepNumer}' and '{dependsOn}' here.",
                     TabName = CmsAttributes.CmsTabName.Layout,
                     GroupName = CmsAttributes.CmsGroupName.Templates,
                     TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
                     DisplayOrder = 20
        )]
        public string StepHtml { get; set; }

        /// <summary>
        ///  The HTML for a step.
        ///  </summary>
        [CmsProperty(PrettyName = "Sub Step HTML",
                     Description = "The HTML for a sub step.",
                     DeveloperRemarks = "You can use the custom variables '{subStepContent}', '{style}', '{isrequired}', '{stepname}', '{mainStepNumber}', '{stepNumer}', '{subStepNumber}' and '{dependsOn}' here.",
                     TabName = CmsAttributes.CmsTabName.Layout,
                     GroupName = CmsAttributes.CmsGroupName.Templates,
                     TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
                     DisplayOrder = 30
        )]
        public string SubStepHtml { get; set; }

        /// <summary>
        ///  The HTML for the summary of the configurator.
        ///  </summary>
        [CmsProperty(PrettyName = "Summary HTML",
                     Description = "The HTML for the summary of the configurator.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.Layout,
                     GroupName = CmsAttributes.CmsGroupName.Templates,
                     TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
                     DisplayOrder = 40
        )]

        public string SummaryHtml { get; set; }

        /// <summary>
        ///  The HTML for the final summary of the configurator, shown on the overview page at the end.
        ///  </summary>
        [CmsProperty(PrettyName = "Final Summary HTML",
                     Description = "The HTML for the final summary of the configurator, shown on the overview page at the end.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.Layout,
                     GroupName = CmsAttributes.CmsGroupName.Templates,
                     TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
                     DisplayOrder = 50
         )]
        public string FinalSummaryHtml { get; set; }

        /// <summary>
        ///  The first part of HTML for showing the progress on mobile devices.
        ///  </summary>
        [CmsProperty(PrettyName = "Mobile Pre Progress HTML",
                     Description = "The first part of HTML for showing the progress on mobile devices.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.Layout,
                     GroupName = CmsAttributes.CmsGroupName.TemplatesForMobileDevices,
                     TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
                     DisplayOrder = 10
        )]
        public string MobilePreProgressHtml { get; set; }

        /// <summary>
        ///  The last part of HTML for showing the progress on mobile devices.
        ///  </summary>
        [CmsProperty(PrettyName = "Mobile Post Progress HTML",
                     Description = "The last part of HTML for showing the progress on mobile devices.",
                     DeveloperRemarks = "",
                     TabName = CmsAttributes.CmsTabName.Layout,
                     GroupName = CmsAttributes.CmsGroupName.TemplatesForMobileDevices,
                     TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
                     DisplayOrder = 20
        )]
        public string MobilePostProgressHtml { get; set; }
        
        #endregion
    }
}
