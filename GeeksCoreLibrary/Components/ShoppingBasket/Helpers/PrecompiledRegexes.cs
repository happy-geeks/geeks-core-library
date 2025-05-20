using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Helpers;

public static partial class PrecompiledRegexes
{
    [GeneratedRegex(@"{[^\]}\s]*}", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex VariableRegex { get; }
    
    [GeneratedRegex("{count~(.*?)}", RegexOptions.Singleline, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex CountReplacementRegex { get; }
    
    [GeneratedRegex("{totalcount~(.*?)}", RegexOptions.Singleline, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex TotalCountReplacementRegex { get; }
    
    [GeneratedRegex("{price~(.*?)}", RegexOptions.Singleline, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex FormattedPriceReplacementRegex { get; }
    
    [GeneratedRegex("{price~?(.*?)}", RegexOptions.Singleline, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex PriceReplacementRegex { get; }
    
    [GeneratedRegex("{percentage~?(.*?)}", RegexOptions.Singleline, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex PercentageReplacementRegex { get; }
}