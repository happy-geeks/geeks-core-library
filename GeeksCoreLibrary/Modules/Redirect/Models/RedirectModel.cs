using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Redirect.Models
{
    public class RedirectModel
    {
        /// <summary>
        /// The URL to redirect to the new URL. Can contain domain optionally.
        /// </summary>
        public string OldUrl { get; set; }
        
        /// <summary>
        /// The URL to redirect to. 
        /// </summary>
        public string NewUrl { get; set; }

        /// <summary>
        /// Indicate whether the redirect is permanent (301) or temporary (302).
        /// </summary>
        public bool Permanent { get; set; }

        /// <summary>
        /// Order number for handling. The higher the number, the sooner the rule is settled.
        /// </summary>
        public int Ordering { get; set; }
    }
}
