using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.WebPage.Models
{
    public class WebPageCmsSettingsModel : CmsSettings
    {
        public WebPage.ComponentModes ComponentMode { get; set; } = WebPage.ComponentModes.Render;

        #region Tab Behavior properties

        [CmsProperty(
            PrettyName = "Set SEO info",
            Description = "Whether the web page is responsible for setting SEO-related information, such as the page title, description, etc.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            DisplayOrder = 10
        )]
        public bool SetSeoInfo { get; set; }

        [CmsProperty(
            PrettyName = "Return not found status code on no data",
            Description = "Will set the not found status code (404) if the SQL query returned no data.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            DisplayOrder = 20
        )]
        public bool ReturnNotFoundStatusCodeOnNoData { get; set; }

        #endregion

        #region Tab DataSource properties

        [CmsProperty(
            PrettyName = "Page name",
            Description = "The SEO name of the web page that should be retrieved.",
            DeveloperRemarks = "This property will be ignored if HandleRequest is enabled and a request parameter called 'pagename' is available.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10
        )]
        public string PageName { get; set; }

        [CmsProperty(
            PrettyName = "Page item ID",
            Description = "The Wiser item ID of the web page that should be retrieved.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 20
        )]
        public ulong PageId { get; set; }

        [CmsProperty(
            PrettyName = "Path must contain",
            Description = "The path of folders where the web page resides. Can be a partial path or full path.",
            DeveloperRemarks = "This only works if SearchNumberOfLevels is higher than 0.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 30
        )]
        public string PathMustContainName { get; set; }

        [CmsProperty(
            PrettyName = "Search numbers of levels",
            Description = "Determines how many levels down the query will look to reach the web page. Default is 5.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 40
        )]
        public int SearchNumberOfLevels { get; set; }

        #endregion
    }
}
