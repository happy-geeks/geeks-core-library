namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class JavaScriptResourceModel : PageResourceModel
    {
        public bool Async { get; set; }

        public bool Defer { get; set; } = true;
    }
}