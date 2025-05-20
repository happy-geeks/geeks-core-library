using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.Filter.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"{filters\((.*?),(.*?)\)}", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex FilterJoinRegex { get; }
}