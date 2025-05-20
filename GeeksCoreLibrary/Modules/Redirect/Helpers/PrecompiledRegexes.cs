using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Redirect.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"\[(\d+?)\]", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex GroupIndexRegex { get; }
    
    [GeneratedRegex(@"\[(.+?)\]", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex GroupNameRegex { get; }
}