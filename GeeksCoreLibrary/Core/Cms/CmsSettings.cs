using System.ComponentModel;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace GeeksCoreLibrary.Core.Cms
{
    public class CmsSettings
    {
        #region Constants

        public const string ExternalJavaScriptLibrariesFromComponentKey = "ExternalJavaScriptLibrariesFromComponent";

        #endregion

        #region Other properties

        /// <summary>
        /// A description that describes the component for use in Wiser.
        /// </summary>
        [CmsProperty(
            PrettyName = "Description",
            Description = "A description that describes the component for use in Wiser. If the component cannot be rendered, this description will be visible in the trace/logs for better identification which component failed to render.",
            DeveloperRemarks = "This field is required.",
            HideInCms = true
        )]
        public string Description { get; set; }

        #endregion

        #region Tab behavior properties

        /// <summary>
        /// If the user needs to be logged in for this to work
        /// </summary>
        [CmsProperty(
            PrettyName = "User needs to be logged in",
            Description = "If set to true, this component will only be rendered if the user is logged in.",
            DeveloperRemarks = "This only works for Wiser 2 customers using the Account component.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 10
        )]
        public bool UserNeedsToBeLoggedIn { get; set; } = false;

        /// <summary>
        /// If set to true, the templates of this component replaces request variables. For example {requestvariablename} is replaced by the value of the parameter named 'requestvariablename' in the querystring or form variables.
        /// </summary>
        [CmsProperty(
            PrettyName = "Handle request",
            Description = "If set to true, the templates of this component replaces request variables. For example {requestvariablename} is replaced by the value of the parameter named 'requestvariablename' in the querystring or form variables.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 10
        )]
        public bool HandleRequest { get; set; } = false;

        /// <summary>
        /// Set this property to 'false' to indicate [if] statements should not be parsed, this is a speed improvement
        /// </summary>
        [CmsProperty(
            PrettyName = "Evaluate if/else in templates",
            Description = "Set this property to 'false' to indicate [if] statements should not be parsed, this is a speed improvement",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 20
        ), DefaultValue(true)]
        public bool EvaluateIfElseInTemplates { get; set; } = true;

        /// <summary>
        /// If this is set to true, all left over variables after all replacements will be removed from the string.
        /// </summary>
        [CmsProperty(
            PrettyName = "Remove unknown variables",
            Description = "If this is set to true, all left over variables after all replacements will be removed from the string.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 30
        ), DefaultValue(true)]
        public bool RemoveUnknownVariables { get; set; } = true;

        #endregion

        #region Tab developer properties

        /// <summary>
        /// How the component should be cached. Default value is 'ServerSideCaching'.
        /// </summary>
        [CmsProperty(
            PrettyName = "Caching mode",
            Description = @"<p>How the component should be cached. Default value is 'ServerSideCaching'. The options are:</p>
                            <ul>
                                <li><strong>NoCaching</strong>: Component will not be cached and will always be rendered on-the-fly.</li>
                                <li><strong>ServerSideCaching</strong>: Component will be cached regardless of URL.</li>
                                <li><strong>ServerSideCachingPerUrl</strong>: Component will be cached based on the URL, excluding the query string.</li>
                                <li><strong>ServerSideCachingPerUrlAndQueryString</strong>: Component will be cached based on the URL, including the query string.</li>
                                <li><strong>ServerSideCachingPerHostNameAndQueryString</strong>: Component will be cached based on the full URL, including domain and the query string.</li>
                                <li><strong>ServerSideCachingBasedOnUrlRegex</strong>: Component will be cached based on a regular expression. When using this option, you need to enter a regular expression in the corresponding field below.</li>
                            </ul>",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.Caching,
            DisplayOrder = 10
        ), DefaultValue(TemplateCachingModes.NoCaching)]
        public TemplateCachingModes CachingMode { get; set; } = TemplateCachingModes.NoCaching;

        /// <summary>
        /// How the component should be cached. Default value is 'ServerSideCaching'.
        /// </summary>
        [CmsProperty(
            PrettyName = "Caching location",
            Description = @"<p>Where the component should be cached. Default value is 'InMemory'. The options are:</p>
                            <ul>
                                <li><strong>InMemory</strong>: Component will be cached in memory. This is much faster than caching it on disk, but caching will be lost if the site is restarted and could cause high memory usage on sites with a lot of pages.</li>
                                <li><strong>OnDisk</strong>: Component will be cached on disk. This is much slower than caching it in memory, but it will not be lost if the site is restarted and will not use (much) memory.</li>
                            </ul>",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.Caching,
            DisplayOrder = 20
        ), DefaultValue(TemplateCachingLocations.InMemory)]
        public TemplateCachingLocations CachingLocation { get; set; } = TemplateCachingLocations.InMemory;

        /// <summary>
        /// The amount of time the component should stay cached. Default value is '0', which means that the setting 'DefaultTemplateCacheDuration' from the appsettings will be used. Set it to any other value to overwrite the appsettings.
        /// </summary>
        [CmsProperty(
            PrettyName = "Cache minutes",
            Description = "The amount of time (in minutes) the component should stay cached. Default value is '0', which means that the setting 'DefaultTemplateCacheDuration' from the appsettings will be used. Set it to any other value to overwrite the appsettings.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.Caching,
            DisplayOrder = 30
        )]
        public int CacheMinutes { get; set; }

        /// <summary>
        /// The regular expression for generating the unique cache key or file name.
        /// </summary>
        [CmsProperty(
            PrettyName = "Cache regex",
            Description = @"<p>The regular expression to use for deciding the unique cache key. The value of each named group will be added to the cache key or file name. This is useful for a main menu for example, to cache the main menu separately for each main category on the site, so that the selected item in the menu will always be correct, even with caching.</p>
                            <p>Example: If you use the regex ""\/products\/(?&lt;category&gt;.*)\/(?&lt;subCategory&gt;.*)\/(?&lt;product&gt;.*)\/"". This regex has 3 named groups (category, subCategory and product). If the user would then open the URL ""/products/drinks/soda/coca-cola/"", then the key or file name for the cache would be ""dynamicContent_123_drinks_soda_coca-cola"". This way the component will be cached separately for each product on the website.</p>",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.Caching,
            DisplayOrder = 30
        )]
        public string CacheRegex { get; set; }

        #endregion
    }
}
