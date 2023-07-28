using System;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class JavaScriptResource
    {
        public Uri Uri { get; set; }

        public bool Async { get; set; }

        public bool Defer { get; set; } = true;
    }
}