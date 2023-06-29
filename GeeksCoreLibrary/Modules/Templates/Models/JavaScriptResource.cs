using System;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class JavaScriptResource : PageResource
    {
        public bool Async { get; set; }

        public bool Defer { get; set; } = true;
    }
}