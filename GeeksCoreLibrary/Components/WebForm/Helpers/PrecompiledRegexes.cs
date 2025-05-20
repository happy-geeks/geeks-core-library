using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.WebForm.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"\{recaptcha_v(?<version>\d)\}", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex RecaptchaReplacementRegex { get; }
}