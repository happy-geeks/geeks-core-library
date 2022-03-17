namespace GeeksCoreLibrary.Core.Cms
{
    public class CmsSettingsLegacy
    {
        /// <summary>
        /// A description that describes the component for use in Wiser.
        /// </summary>
        public string VisibleDescription { get; set; }

        public int ComponentMode { get; set; } 

        /// <summary>
        /// If the user needs to be logged in for this to work
        /// </summary>
        public bool UserNeedsToBeLoggedIn { get; set; } = false;
        
        /// <summary>
        /// If set to true, the templates of this component replaces request variables. For example {requestvariablename} is replaced by the value of the parameter named 'requestvariablename' in the querystring or form variables.
        /// </summary>
        public bool HandleRequest { get; set; } = false;

        /// <summary>
        /// Set this property to 'false' to indicate [if] statements should not be parsed, this is a speed improvement
        /// </summary>
        public bool EvaluateIfElseInTemplates { get; set; } = false;

        /// <summary>
        /// If this is set to true, all left over variables after all replacements will be removed from the string.
        /// </summary>
        public bool RemoveUnknownVariables { get; set; } = false;
        
        /// <summary>
        /// Will set a 404 status for the page if no data was found
        /// </summary>
        public bool Return404OnNoData { get; set; } 
        
        /// <summary>
        /// Comma separated list of parameters to exclude. Will be overruled by the system object if it's filled.
        /// </summary>
        public string ParametersToExclude { get; set; } 

        /// <summary>
        /// The way the caching is being applied, choose 'nocache' to disable output (HTML) caching, query caching will be applied if 'Caching hours' is above 0. Choose 'cache' or 'cacheurldifference' to cache to an html file. Choose the memcache options to cache to the server memory.
        /// </summary>
        public string CacheModus { get; set; } 

        /// <summary>
        /// Number of hours to cache the query or output (HTML) result, 0 takes the caching settings from the web.config. -1 is no caching. When 'Cache Mode' is 'nocache', then only the query results will be cached.
        /// </summary>
        public string CachingHours { get; set; } 
        
        /// <summary>
        /// By default '[if]' statements will be ignored in queries. Enable this checkbox if you want to use them in queries.
        /// </summary>
        public bool EvaluateLogicBlocksInQueries { get; set; } 

        /// <summary>
        /// Extra data from template.
        /// </summary>
        public string ExtraData { get; set; } 
        
        /// <summary>
        /// Used to replace the template with explaination of certain words with a infoballoon of some kind. Currently only available in the Productmodule component.
        /// </summary>
        public bool HandleDefinitions { get; set; } 

        /// <summary>
        /// If set, it's possible to use dynamic content in a template of this component.
        /// </summary>
        public bool HandleDynamicContent { get; set; } 

        /// <summary>
        /// If set to true the replacement of translation or object fields is done at the creation of the items.
        /// If set to false the replacement is done after all items are created, this is much faster, but not always what one would want, for example:
        /// if you would like to use the items with their replacement in a for loop after creation of the items this property must be set to true
        /// </summary>
        public bool HandleTransalationOrObjectReplacementOnItemLevel { get; set; } 
        
        /// <summary>
        /// If set to true, the translations in the regex format \[T{(.+?)?}] are replaced with the correct translations
        /// </summary>
        public bool HandleTranslations { get; set; } 
        
        public string Html { get; set; } 
        
        public string SQLQuery { get; set; }
    }
}
