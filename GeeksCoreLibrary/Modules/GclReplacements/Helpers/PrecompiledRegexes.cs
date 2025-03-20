using System;
using System.Text.RegularExpressions;

namespace GeeksCoreLibrary.Modules.GclReplacements.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"\[(?:if\(|else\]|endif\])", RegexOptions.None, 200)]
    public static partial Regex ConditionalParts { get; }

    [GeneratedRegex(@"(?<methodname>[^\(\)]+)(?:\((?<parameters>[^\)]+)\))?", RegexOptions.IgnoreCase, 3_000, "nl-NL")]
    public static partial Regex Formatters { get; }
}