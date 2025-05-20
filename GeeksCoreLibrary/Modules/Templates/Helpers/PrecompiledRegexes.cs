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
    
    [GeneratedRegex(@"\[{seomodule_content}\|(.*?)\]", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex SeoModuleContentReplacementRegex { get; }
    
    [GeneratedRegex(@"\[{seomodule_h1header}\|(.*?)\]", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex SeoModuleH1ReplacementRegex { get; }
    
    [GeneratedRegex(@"\[{seomodule_h2header}\|(.*?)\]", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex SeoModuleH2ReplacementRegex { get; }
    
    [GeneratedRegex(@"\[{seomodule_h3header}\|(.*?)\]", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex SeoModuleH3ReplacementRegex { get; }
    
    [GeneratedRegex(@"\[{seomodule_.*?}\|(.*?)\]", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex SeoModuleLeftOverReplacementRegex { get; }
    
    [GeneratedRegex("^<script.*?>(?<script>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline )]
    public static partial Regex ScriptElementRegex { get; }
}