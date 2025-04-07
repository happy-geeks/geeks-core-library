using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Communication.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"\D+", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex NumbersOnlyRegex { get; }
}