using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.Repeater.Models
{
    public class RepeaterCmsSettingsModel : CmsSettings
    {
        #region Properties hidden in CMS
        
        /// <summary>
        /// The mode of the component
        /// </summary>
        [CmsProperty(
            PrettyName = "Component mode",
            Description = "The current component mode the component should run in",
            DeveloperRemarks = "Legacy components (ML)Simplemenu/ProductModule is now just Repeater",
            DisplayOrder = 10,
            HideInCms = true,
            ReadOnlyInCms = true
        )]
        public Repeater.ComponentModes ComponentMode { get; set; } = Repeater.ComponentModes.Repeater;

        #endregion

        #region Tab DataSource properties
        
        /// <summary>
        /// The data source that should be used
        /// </summary>
        [CmsProperty(
            PrettyName = "Data source",
            Description = "The data source that should be used",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10,
            HideInCms = false,
            ReadOnlyInCms = false
        )]
        public Repeater.DataSource DataSource { get; set; } = Repeater.DataSource.Query;

        /// <summary>
        /// The Data Query (MySQL)
        /// </summary>
        [CmsProperty(
            PrettyName = "Data Query",
            Description = "Data query",
            DeveloperRemarks = "Format MS SQL/MySql Depending on data source (for now only MySql)",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 10,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            HideInCms = false,
            ReadOnlyInCms = false
        )]
        public string DataQuery { get; set; } = "";

        #endregion

        #region Tab Layout properties

        /// <summary>
        /// These are the used templates. For 1 layer only first index is used and key ignored.
        /// The key is the used index, for every layer there is one key(data column name) and HTML templates
        /// </summary>
        [CmsProperty(
            PrettyName = "HTML Templates",
            Description = "Templates are in layers, every layer has its data 'key' and HTML templates.",
            DeveloperRemarks = "Uses the sorting of the dataset but does not have to be in exact order, it can have gaps.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 10,
            ComponentMode = "NonLegacy",
            HideInCms = false,
            ReadOnlyInCms = false
        )]
        public SortedList<string, RepeaterTemplateModel> GroupingTemplates { get; set; } = new();

        /// <summary>
        /// These are banners that will be placed on certain positions in between the results of the Repeater.
        /// The positions to place the banners can be set in the module 'Productbanners'.
        /// </summary>
        [CmsProperty(
            PrettyName = "Place product banners",
            Description = "These are banners that will be placed on certain positions in between the results of the Repeater.",
            DeveloperRemarks = "This only works for single level repeaters. The positions to place the banners can be set in the module 'Productbanners'.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.AdvancedTemplates,
            DisplayOrder = 20
        )]
        public bool PlaceProductBanners { get; set; }

        /// <summary>
        /// The HTML template for a product banner.
        /// </summary>
        [CmsProperty(
            PrettyName = "Product banner template",
            Description = "The HTML template for a product banner.",
            DeveloperRemarks = @"<p>This only works for single level repeaters</p><p>You can use the following variables here:</p>
                                <ul>
                                    <li>{ItemId}</li>
                                    <li>{Name}</li>
                                    <li>{Content}</li>
                                    <li>{LanguageCode}</li>
                                    <li>{BannerSize}</li>
                                    <li>{BaseUrl}</li>
                                    <li>{UrlContains}</li>
                                    <li>{Position}</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.AdvancedTemplates,
            DisplayOrder = 30,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string ProductBannerTemplate { get; set; }

        /// <summary>
        /// If you want to create groups of items, set this to a number higher than 1. Use this in combination with 'Group header' and/or 'Group footer', then those headers and footers will be added before/after every N items.
        /// </summary>
        [CmsProperty(
            PrettyName = "Create groups of N items",
            Description = "If you want to create groups of items, set this to a number higher than 1. Use this in combination with 'Group header' and/or 'Group footer', then those headers and footers will be added before/after every N items.",
            DeveloperRemarks = "This only works for single level repeaters",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.AdvancedTemplates,
            DisplayOrder = 40
        )]
        public int CreateGroupsOfNItems { get; set; } = 1;
        
        /// <summary>
        /// The header of each group. Only used when 'Create groups of N items' is greater than 1.
        /// </summary>
        [CmsProperty(
            PrettyName = "Group header",
            Description = "The header of each group. Only used when 'Create groups of N items' is greater than 1.",
            DeveloperRemarks = "This only works for single level repeaters",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.AdvancedTemplates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 50
        )]
        public string GroupHeader { get; set; }

        /// <summary>
        /// Turn on to also show the group header for the first group. By default the first group header is not added, because the global header will already be there.
        /// </summary>
        [CmsProperty(
            PrettyName = "Show group header for first group",
            Description = "Turn on to also show the group header for the first group. By default the first group header is not added, because the global header will already be there.",
            DeveloperRemarks = "This only works for single level repeaters",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.AdvancedTemplates,
            DisplayOrder = 60
        )]
        public bool ShowGroupHeaderForFirstGroup { get; set; }
        
        /// <summary>
        /// The footer of each group. Only used when 'Create groups of N items' is greater than 1.
        /// </summary>
        [CmsProperty(
            PrettyName = "Group footer",
            Description = "The footer of each group. Only used when 'Create groups of N items' is greater than 1.",
            DeveloperRemarks = "This only works for single level repeaters",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.AdvancedTemplates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 70
        )]
        public string GroupFooter { get; set; }

        /// <summary>
        /// Turn on to also show the group footer for the last group. By default the last group footer is not added, because the global footer will already be there.
        /// </summary>
        [CmsProperty(
            PrettyName = "Show group footer for last group",
            Description = "Turn on to also show the group footer for the last group. By default the last group footer is not added, because the global footer will already be there.",
            DeveloperRemarks = "This only works for single level repeaters",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.AdvancedTemplates,
            DisplayOrder = 80
        )]
        public bool ShowGroupFooterForLastGroup { get; set; }
        
        /// <summary>
        /// The footer of each group. Only used when 'Create groups of N items' is greater than 1.
        /// </summary>
        [CmsProperty(
            PrettyName = "Empty group item HTML",
            Description = "HTML placed if there is an empty item in a group. For example, if you have 'Group items per' set to 4 and the last group only has 2 items, then the remaining 2 items will use this HTML.",
            DeveloperRemarks = "This only works for single level repeaters",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.AdvancedTemplates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 90
        )]
        public string EmptyGroupItemHtml { get; set; }

        #endregion

        #region Tab Behavior properties
        
        /// <summary>
        /// Setting this to true causes the Repeater to build the HTML the way that MLSimpleMenu in the JCL used to, instead of the more logical way.
        /// The JCL adds a header and footer around every single item, except for items in the deepest level. Since this makes no sense, we changed that in the GCL.
        /// The GCL will only add a header before the first item of every level and a footer after every last item of a level.
        /// </summary>
        [CmsProperty(
            PrettyName = "Legacy mode",
            Description = "You should only enable this setting for customers that have been converted from the JCL, not for new Wiser 3 customers. Setting this to true causes the Repeater to build the HTML the way that MLSimpleMenu in the JCL used to, instead of the more logical way.",
            DeveloperRemarks = "The JCL adds a header and footer around every single item, except for items in the deepest level. Since this makes no sense, we changed that in the GCL. The GCL will only add a header before the first item of every level and a footer after every last item of a level.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Misc,
            DisplayOrder = 10,
            HideInCms = false,
            ReadOnlyInCms = true
        )]
        public bool LegacyMode { get; set; }

        /// <summary>
        /// If there is no data then return 404 if this property is set to true
        /// </summary>
        [CmsProperty(
            PrettyName = "Return 404 on no data",
            Description = "Will set a 404 status for the page if no data was found",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            DisplayOrder = 10,
            HideInCms = false,
            ReadOnlyInCms = false
        )]
        public bool Return404OnNoData { get; set; } = false;
        
        /// <summary>
        /// Use the SEO Fields of the first item to set the Page SEO fields.
        /// These fields must be named: SEOtitle, SEOdescription, SEOkeywords, SEOcanonical, noindex, nofollow, relprev, relnext, SEOrobots hreflang1..n
        /// </summary>
        [CmsProperty(
            PrettyName = "Set SEO info from first item",
            Description = "Use the SEO Fields of the first item to set the Page SEO fields. These fields must be named: SEOtitle, SEOdescription, SEOkeywords, SEOcanonical, noindex, nofollow, relprev, relnext, SEOrobots hreflang1..n",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            DisplayOrder = 20,
            HideInCms = false,
            ReadOnlyInCms = false
        )]
        public bool SetSeoInformationFromFirstItem { get; set; }

        /// <summary>
        /// Use the Open Graph Fields of the first item to add Open Graph meta tags to the head section.
        /// These fields must start with "opengraph_". The part after the underscore will determine the
        /// name of the Open Graph item, e.g.: "opengraph_title" will create a meta tag called "og:title".
        /// </summary>
        [CmsProperty(
            PrettyName = "Set Open Graph info from first item",
            Description = "Use the Open Graph Fields of the first item to add Open Graph meta tags to the head section.",
            DeveloperRemarks = "These fields must start with \"opengraph_\" and the part that follows will be used as the name of the meta tag.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            DisplayOrder = 30,
            HideInCms = false,
            ReadOnlyInCms = false
        )]
        public bool SetOpenGraphInformationFromFirstItem { get; set; }

        /// <summary>
        /// The amount of items each page will show.
        /// </summary>
        [CmsProperty(
            PrettyName = "Items per page",
            Description = "The amount of items each page will show.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10
        )]
        public uint ItemsPerPage { get; set; }

        [CmsProperty(
            PrettyName = "Show base header and footer on no data",
            Description = "Whether the base header and footer will be rendered if no data was found.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 20
        )]
        public bool ShowBaseHeaderAndFooterOnNoData { get; set; }

        [CmsProperty(
            PrettyName = "Loads the items up to the specified page number",
            Description = "Use this property to load items up to the specified page number",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 30
        )]
        public bool LoadItemsUpToPageNumber { get; set; }

        [CmsProperty(
            PrettyName = "Banner uses product block space",
            Description = "When enabled the banner uses product blocks otherwise it will just be added to the output html",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 40
        )]
        public bool BannerUsesProductBlockSpace { get; set; }

        #endregion
    }
}
