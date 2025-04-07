using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Templates.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"<\[(.*?)\]>", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex MinimalInclusionsRegex { get; }

    [GeneratedRegex(@"\[include\[([^{?\]]*)(\?)?([^{?\]]*?)\]\]", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex InclusionsRegex { get; }

    [GeneratedRegex("""<div[^<>]*?(?:class=['"]dynamic-content['"][^<>]*?)?(?:data=['"](?<data>.*?)['"][^>]*?)?(component-id|content-id)=['"](?<contentId>\d+)['"][^>]*?>[^<>]*?<h2>[^<>]*?(?<title>[^<>]*?)<\/h2>[^<>]*?<\/div>""", RegexOptions.IgnoreCase | RegexOptions.Singleline, 180_000, Constants.DefaultRegexCulture)]
    public static partial Regex DynamicContentRegex { get; }

    [GeneratedRegex(@"PUSHER<channel\((.*?)\),event\((.*?)\),message\(((?s:.)*?)\)>")]
    public static partial Regex PusherRegex { get; }

    [GeneratedRegex(@"\[image\[(.*?)\]\]", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex ImageTemplatingRegex { get; }
    
    [GeneratedRegex(@"\:(.*?)\)", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex ImageTemplatingSetsRegex { get; }

    [GeneratedRegex("""<svg(?:[^>]*)>(?:\s*)<use(?:[^>]*)xlink:href="([^>"]*)#(?:[^>"]*)"(?:[^>]*)>""", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex SvgTagRegex { get; }
}