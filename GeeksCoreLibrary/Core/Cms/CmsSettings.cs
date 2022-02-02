
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Core.Cms
{
    public abstract class CmsSettings
    {
        #region Constants
        
        public const string PageMetaDataFromComponentKey = "PageMetaDataFromComponent";

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
            DisplayOrder = 10,
            HideInCms = false,
            ReadOnlyInCms = false
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
            DisplayOrder = 10,
            HideInCms = false,
            ReadOnlyInCms = false
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
            DisplayOrder = 20,
            HideInCms = false,
            ReadOnlyInCms = false
        )]
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
            DisplayOrder = 30,
            HideInCms = false,
            ReadOnlyInCms = false
        )]
        public bool RemoveUnknownVariables { get; set; } = true;

        #endregion
    }
}
