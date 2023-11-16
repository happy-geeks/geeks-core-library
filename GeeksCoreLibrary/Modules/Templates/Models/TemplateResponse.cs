using System;
using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class TemplateResponse
    {
        /// <summary>
        /// Gets or sets the content of the template. This can be HTML, CSS, Javascript etc.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the date and time that the template was last changed.
        /// </summary>
        public DateTime LastChangeDate { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets a list of external files. If this template needs to load external CSS/Javascript files, the URLs to these files will be added here.
        /// </summary>
        public List<PageResourceModel> ExternalFiles { get; set; } = new();
    }
}