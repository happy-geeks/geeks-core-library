using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class Template
    {
        /// <summary>
        /// Gets or sets the item id of the template.
        /// This is the id from easy_items.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the parent.
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Gets or sets the name of the root directory in the templates module.
        /// This should be one of the following values: CSS, HTML, Scripts, QUERY or SERVICES.
        /// </summary>
        public string RootName { get; set; }

        /// <summary>
        /// Gets or sets the name of the parent folder.
        /// </summary>
        public string ParentName { get; set; }

        /// <summary>
        /// Gets or sets the name of the template.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sort order of the template.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the sort order of the parent folder.
        /// </summary>
        public int ParentSortOrder { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public TemplateTypes Type { get; set; }

        /// <summary>
        /// Gets or sets the way this template needs to be inserted into the page.
        /// Only applicable for CSS and javascript templates.
        /// </summary>
        public ResourceInsertModes InsertMode { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets whether the template should be loaded on all pages (true), or only on specific pages (false).
        /// This property is only applicable to Javascript and CSS templates.
        /// </summary>
        public bool LoadAlways { get; set; }

        /// <summary>
        /// Gets or sets the regular expression that filters URLs on which this template should be loaded.
        /// </summary>
        public string UrlRegex { get; set; }

        /// <summary>
        /// Gets or sets the date and time that this template was last changed.
        /// </summary>
        public DateTime LastChanged { get; set; }

        /// <summary>
        /// Gets or sets the list of external files. This is only applicable for templates of type <see cref="TemplateTypes.Css"/> and <see cref="TemplateTypes.Js"/>.
        /// These are external CSS or Javascript files that need to be loaded before the current CSS or Javascript file.
        /// </summary>
        public List<PageResourceModel> ExternalFiles { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of CSS templates that should be used with the current template.
        /// This property is only applicable for HTML templates.
        /// </summary>
        public List<int> CssTemplates { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of Javascript templates that should be used with the current template.
        /// This property is only applicable for HTML templates.
        /// </summary>
        public List<int> JavascriptTemplates { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of extra files that need to be loaded from the Wiser CDN.
        /// </summary>
        public List<string> WiserCdnFiles { get; set; } = new();

        /// <summary>
        /// Gets or sets if and how the template will be cached. Legacy.
        /// </summary>
        [Obsolete("This property exists for backwards compatibility only. Use the booleans CachePerUrl, CachePerQueryString, CachePerHostName and CacheUsingRegex instead.")]
        public TemplateCachingModes CachingMode { get; set; } = TemplateCachingModes.NoCaching;

        /// <summary>
        /// Gets or sets whether the caching is seperated by SEO url
        /// </summary>
        public bool CachePerUrl { get; set; }

        /// <summary>
        /// Gets or sets whether the caching is seperated by query string parameters
        /// </summary>
        public bool CachePerQueryString { get; set; }

        /// <summary>
        /// Gets or sets whether the caching is seperated by hostname
        /// </summary>
        public bool CachePerHostName { get; set; }

        /// <summary>
        /// Gets or sets whether caching is determined by a regex
        /// </summary>
        public bool CacheUsingRegex { get; set; }

        /// <summary>
        /// Gets or sets how long the template will be cached in minutes.
        /// </summary>
        public int CachingMinutes { get; set; }

        /// <summary>
        /// Gets or sets where the template should be cached.
        /// </summary>
        public TemplateCachingLocations CachingLocation { get; set; } = TemplateCachingLocations.InMemory;

        /// <summary>
        /// Gets or sets the regular expression that is matched against the URL of the page, to decide whether to use content caching or not.
        /// </summary>
        public string CachingRegex { get; set; }

        /// <summary>
        /// Gets or sets the query that should be executed at the start of loading a HTML template on the page.
        /// </summary>
        public string PreLoadQuery { get; set; }

        /// <summary>
        /// Gets or sets whether we should return an HTTP 404 result when the pre load query returns 0 rows.
        /// </summary>
        public bool ReturnNotFoundWhenPreLoadQueryHasNoData { get; set; }

        /// <summary>
        /// Gets or sets whether it's required for a user to be logged when trying to access the template.
        /// </summary>
        public bool LoginRequired { get; set; }

        /// <summary>
        /// Gets or sets the roles that are allowed to open this template.
        /// If empty, all roles can see it.
        /// </summary>
        public List<int> LoginRoles { get; set; }

        /// <summary>
        /// Gets or sets the URL the user should be sent to if <see cref="LoginRequired"/> is <see langword="true"/>, but no user is logged in.
        /// </summary>
        public string LoginRedirectUrl { get; set; }

        /// <summary>
        /// Gets or sets whether thie template can be used as the default header.
        /// </summary>
        public bool IsDefaultHeader { get; set; }

        /// <summary>
        /// Gets or sets whether this template can be used as the default footer.
        /// </summary>
        public bool IsDefaultFooter { get; set; }

        /// <summary>
        /// Gets or sets the regular expression that is matched against the URL of the page, to decide whether to use the default header or footer.
        /// </summary>
        public string DefaultHeaderFooterRegex { get; set; }

        /// <summary>
        /// Gets or sets if this template is only a partial.
        /// </summary>
        public bool IsPartial { get; set; }

        /// <summary>
        /// Gets or sets the version of the template that was loaded.
        /// </summary>
        public int Version { get; set; }
    }
}