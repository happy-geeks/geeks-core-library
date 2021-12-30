using System.Reflection;

namespace GeeksCoreLibrary.Modules.GclReplacements.Models
{
    public class StringReplacementMethod
    {
        public MethodInfo Method { get; set; }

        public object[] Parameters { get; set; }
    }
}
