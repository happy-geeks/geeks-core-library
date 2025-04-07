using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.ShoppingBasket;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"{[^\]}\s]*}", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex VariableRegex { get; }
}