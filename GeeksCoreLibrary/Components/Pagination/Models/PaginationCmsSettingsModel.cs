using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.Pagination.Models
{
    public class PaginationCmsSettingsModel : CmsSettings
    {
        #region Properties hidden in CMS

        /// <summary>
        /// The mode of the component
        /// </summary>
        [CmsProperty(
            PrettyName = "Component mode",
            Description = "The current component mode the component should run in",
            DisplayOrder = 10,
            HideInCms = true,
            ReadOnlyInCms = true
        )]
        public Pagination.ComponentModes ComponentMode { get; set; } = Pagination.ComponentModes.Normal;

        #endregion

        #region Tab DataSource properties

        [CmsProperty(
            PrettyName = "Data Query",
            Description = "The data query used to count the amount of items.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor
        )]
        public string DataQuery { get; set; }

        #endregion

        #region Tab Behavior properties

        [CmsProperty(
            PrettyName = "Items per page",
            Description = "The amount of items each page will show.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10
        )]
        public uint ItemsPerPage { get; set; }

        [CmsProperty(
            PrettyName = "Max pages",
            Description = "The maximum amount of pages that are allowed to be rendered.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 20
        )]
        public uint MaxPages { get; set; }

        [CmsProperty(
            PrettyName = "Min pages at start",
            Description = "The minimum amount of pages that should be shown at the start.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 21
        )]
        public uint MinPagesAtStart { get; set; }

        [CmsProperty(
            PrettyName = "Min pages at end",
            Description = "The minimum amount of pages that should be shown at the end.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 22
        )]
        public uint MinPagesAtEnd { get; set; }

        [CmsProperty(
            PrettyName = "Max pages before current",
            Description = "How many pages can be shown in front of the current page.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 30
        )]
        public uint MaxPagesBeforeCurrent { get; set; }

        [CmsProperty(
            PrettyName = "Max pages after current",
            Description = "How many pages can be shown after of the current page.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 40
        )]
        public uint MaxPagesAfterCurrent { get; set; }

        [CmsProperty(
            PrettyName = "Combine max before and after",
            Description = "Whether to combine the amount of pages set to the max before and after.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 50
        )]
        public bool CombineMaxBeforeAndAfter { get; set; }

        [CmsProperty(
            PrettyName = "Add page query string to link format",
            Description = "Will add existing query string variables to the link format.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 60
        )]
        public bool AddPageQueryStringToLinkFormat { get; set; }

        [CmsProperty(
            PrettyName = "Remove first page from URL",
            Description = "Whether to remove the page number variable from the URL if it links to the first page.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 70
        )]
        public bool RemoveFirstPageFromUrl { get; set; }

        [CmsProperty(
            PrettyName = "Add dots to first and last",
            Description = "Whether dots should be placed after the first and next item, and the last and second-last item if the page numbers contain a leap. The dots can be replaced with something else using the dots template.",
            DeveloperRemarks = "This only works if the page number is part of the query string, otherwise there is no way to determine which part of the URL represents the page number.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 80
        )]
        public bool AddDotsToFirstAndLast { get; set; }

        [CmsProperty(
            PrettyName = "Dots offset",
            Description = "This setting will change the place the dots will start showing.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 90
        )]
        public int DotsOffset { get; set; }

        /// <summary>
        /// Whether the HTML should be rendered even if there's only one page.
        /// </summary>
        [CmsProperty(
            PrettyName = "Render for single page",
            Description = "Whether the HTML should be rendered even if there's only one page.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 100
        )]
        public bool RenderForSinglePage { get; set; }

        #endregion

        #region Tab Layout properties

        [CmsProperty(
            PrettyName = "Full template",
            Description = "The full template that will determine the containing HTML of the entire Pagination component.",
            DeveloperRemarks = @"<p>The following replacements can be used in this template:</p>
                                <ul>
                                    <li><strong{pagination}:</strong> This will be replaced with the generated pagination links.</li>
                                    <li><strong>{summary}:</strong> This will be replaced with the summary of the pagination, which is a collection of all selected filters.</li>
                                    <li><strong>{pagenr}:</strong> The current page number.</li>
                                    <li><strong>{lastpagenr}:</strong> The number of the last page. Can be used to determine how many pages there are total.</li>
                                    <li><strong>{totalitems}:</strong> The total number of items across all pages.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 10,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string FullTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Summary template",
            Description = "The summary template that will show the range of items being shown, like 'Showing items 101 to 110 out of 789'.",
            DeveloperRemarks = @"<p>The following variables can be used:</p>
                                <ul>
                                    <li><strong>{firstitemnr}:</strong> The number of the first item of the current page.</li>
                                    <li><strong>{lastitemnr}:</strong> The number of the last item of the current page.</li>
                                    <li><strong>{totalitems}:</strong> The total number of items across all pages.</li>
                                    <li><strong>{totalitemnrs}:</strong> Synonym for {totalitems} (made available for backward compatibility reasons).</li>
                                </ul>
                                <p>
                                    Example:<br />
                                    Showing items {firstitemnr} to {lastitemnr} out of {totalitems}.
                                </p>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 20,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string SummaryTemplate { get; set; }

        [CmsProperty(
            PrettyName = "First page template",
            Description = "The template for the first page link.",
            DeveloperRemarks = @"<p>The following variables can be used:</p>
                                <ul>
                                    <li><strong>{link}:</strong> The URL generated for the first page.</li>
                                    <li><strong>{pnr}:</strong> The page number of the first page (which is always 1).</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 30,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string FirstPageTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Previous page template",
            Description = "The template for the previous page link.",
            DeveloperRemarks = @"<p>The following variables can be used:</p>
                                <ul>
                                    <li><strong>{link}:</strong> The URL generated for the previous page.</li>
                                    <li><strong>{pnr}:</strong> The page number of the previous page.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 40,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string PreviousPageTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Page template",
            Description = "The template for a normal page link.",
            DeveloperRemarks = @"<p>The following variables can be used:</p>
                                <ul>
                                    <li><strong>{link}:</strong> The URL generated for the page.</li>
                                    <li><strong>{pnr}:</strong> The page number of the page.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 50,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string PageTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Current page template",
            Description = "The template for the current page link.",
            DeveloperRemarks = @"<p>The following variables can be used:</p>
                                <ul>
                                    <li><strong>{link}:</strong> The URL generated for the current page.</li>
                                    <li><strong>{pnr}:</strong> The page number of the current page.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 60,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string CurrentPageTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Next page template",
            Description = "The template for the next page link.",
            DeveloperRemarks = @"<p>The following variables can be used:</p>
                                <ul>
                                    <li><strong>{link}:</strong> The URL generated for the next page.</li>
                                    <li><strong>{pnr}:</strong> The page number of the next page.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 70,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string NextPageTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Last page template",
            Description = "The template for the last page link.",
            DeveloperRemarks = @"<p>The following variables can be used:</p>
                                <ul>
                                    <li><strong>{link}:</strong> The URL generated for the last page.</li>
                                    <li><strong>{pnr}:</strong> The page number of the last page.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 80,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string LastPageTemplate { get; set; }

        [CmsProperty(
            PrettyName = "In between template",
            Description = "The template that will be placed between two pages.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 90,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string InBetweenTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Link format",
            Description = "The URL format of the pagination links.",
            DeveloperRemarks = @"<p>The following variables can be used:</p>
                                <ul>
                                    <li><strong>{variablename}:</strong> The page variable name.</li>
                                    <li><strong>{pnr}:</strong> The page number that this URL will link to.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 100,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextEditor
        )]
        public string LinkFormat { get; set; }

        [CmsProperty(
            PrettyName = "Dots template",
            Description = "The template for the dots that will be placed when \"Add dots to first and last\" is enabled.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            DisplayOrder = 110,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor
        )]
        public string DotsTemplate { get; set; }

        #endregion

        #region Tab Developer properties

        [CmsProperty(
            PrettyName = "Page variable name",
            Description = "The name of the query string variable where the page number will be stored in.",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10
        )]
        public string PageNumberVariableName { get; set; }

        #endregion
    }
}
