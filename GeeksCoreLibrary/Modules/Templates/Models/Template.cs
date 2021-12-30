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
        /// This should be one of the following values: CSS, HTML, Scripts, QUERY or AIS.
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
        public List<string> ExternalFiles { get; set; } = new();

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
        /// Gets or sets if and how the template will be cached.
        /// </summary>
        public TemplateCachingModes CachingMode { get; set; } = TemplateCachingModes.NoCaching;

        /// <summary>
        /// Gets or sets how long the template will be cached in minutes.
        /// </summary>
        public int CachingMinutes { get; set; }
    }
}