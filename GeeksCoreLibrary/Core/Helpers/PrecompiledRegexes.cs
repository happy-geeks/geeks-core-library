using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Core.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"(\.jpe?g|\.gif|\.png|\.webp|\.svg|\.bmp|\.tif|\.ico|\.woff2?|\.s?css|\.js|\.[gj]cl|\.webmanifest|\.ttf)(?:\?.*)?$", RegexOptions.IgnoreCase,  Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex UrlsToSkipForMiddlewaresRegex { get; }

    [GeneratedRegex(@"\@import url\((.+?)\);", RegexOptions.IgnoreCase | RegexOptions.Singleline, 30_000)]
    public static partial Regex CssImportRegex { get; }
    
    [GeneratedRegex("""<div[^<>]*?(?:class=['"]dynamic-content['"][^<>]*?)?(entity-block-item-id)=['"](?<itemId>\d+)['"]([^<>]*?)?>[^<>]*?<h2>[^<>]*?(?<title>[^<>]*?)<\/h2>[^<>]*?<\/div>""", RegexOptions.IgnoreCase | RegexOptions.Singleline, 180_000)]
    public static partial Regex EntityBlockTemplateRegex { get; }
    
    [GeneratedRegex("gcl_clear.*?cache", RegexOptions.IgnoreCase,  Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex CacheQueryParamRegex { get; }
    
    [GeneratedRegex("^clear(?<area>.+?)cache$", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex CacheAreaRegex { get; }
    
    [GeneratedRegex("\\p{IsCyrillic}", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex IsCyrillicRegex { get; }
    
    [GeneratedRegex("""<img.*?src=["'](http:\/\/.*?)["']""", RegexOptions.IgnoreCase | RegexOptions.Singleline,  Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex ImgTagRegex { get; }
    
    [GeneratedRegex("""<source.*?srcset=["'](http:\/\/.*?)["']""", RegexOptions.IgnoreCase | RegexOptions.Singleline,  Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex SourceTagRegex { get; }
}