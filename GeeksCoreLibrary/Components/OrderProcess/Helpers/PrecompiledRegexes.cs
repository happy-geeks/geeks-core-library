using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.OrderProcess.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex("(@)(.+)$", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex EmailRegex { get; }
}