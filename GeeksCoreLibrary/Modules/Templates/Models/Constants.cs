using GeeksCoreLibrary.Modules.Templates.Enums;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public static class Constants
    {
        public const string OriginalPathKey = "OriginalPath";
        public const string OriginalQueryStringKey = "OriginalQueryString";
        public const string OriginalPathAndQueryStringKey = "OriginalPathAndQueryString";
        public const string WiserUriOverrideForReplacements = "WiserUriOverride";
        public const string TemplatePreLoadQueryResultKey = "TemplatePreLoadQueryResult";
        public const string PageMetaDataFromComponentKey = "PageMetaDataFromComponent";
        public const string TemplateCacheRootDirectoryName = "Templates";
        public const string ComponentsCacheRootDirectoryName = "Components";
        public const string PageCacheRootDirectoryName = "Pages";
        
        public const string PageWidgetEntityType = "page-widget";
        public const int PageWidgetParentLinkType = 1;
        public const string PageWidgetLocationPropertyName = "location";
        public const string PageWidgetHtmlPropertyName = "html";
        public const PageWidgetLocations PageWidgetDefaultLocation = PageWidgetLocations.HeaderBottom;
    }
}
