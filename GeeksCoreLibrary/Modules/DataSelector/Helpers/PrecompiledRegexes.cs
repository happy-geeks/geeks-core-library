using System.Text.RegularExpressions;

namespace GeeksCoreLibrary.Modules.DataSelector.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex("""<div[^<>]*?(?:class=['"]dynamic-content['"][^<>]*?)?(data-selector-id)=['"](?<dataSelectorId>\d+)['"]([^<>]*?)?(template-id)=['"](?<templateId>\d+)['"][^>]*?>[^<>]*?<h2>[^<>]*?(?<title>[^<>]*?)<\/h2>[^<>]*?<\/div>""", RegexOptions.IgnoreCase | RegexOptions.Singleline, 180_000)]
    public static partial Regex TemplateRegex { get; }
}