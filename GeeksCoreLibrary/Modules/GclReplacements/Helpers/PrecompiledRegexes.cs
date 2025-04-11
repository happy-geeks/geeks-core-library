using System;
using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.GclReplacements.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"\[(?:if\(|else\]|endif\])", RegexOptions.None, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex ConditionalParts { get; }

    [GeneratedRegex(@"(?<methodname>[^\(\)]+)(?:\((?<parameters>[^\)]+)\))?", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex Formatters { get; }
    
    [GeneratedRegex(@"\[SO{([^\}]+)}]", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex SystemObjectReplacementRegex { get; }

    [GeneratedRegex(@"\[T{([^\}]+)}]", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex TranslationReplacementRegex { get; }

    [GeneratedRegex(@"\[O{([^\}]+)}]", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex CmsObjectRegex { get; }

    [GeneratedRegex(@"{repeat:([^\.]+?)}", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds, Constants.DefaultRegexCulture)]
    public static partial Regex RepeatReplacementRegex { get; }
    
    [GeneratedRegex("{([^};]*[^};\\s])}", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex MultiPartReplacementRegex { get; }

    [GeneratedRegex(@"(.*)\((\d.*)\)(.*)", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex IndexedPropertyRegex { get; }

    [GeneratedRegex(" ?style=\".*?\"", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex InlineStyleRegex { get; }

    [GeneratedRegex("{.*?}", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex VariableNonCaptureRegex { get; }
}