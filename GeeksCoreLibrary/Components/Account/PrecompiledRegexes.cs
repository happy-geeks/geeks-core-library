using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.Account;

public partial class PrecompiledRegexes
{
    [GeneratedRegex("{repeat:subAccounts}(.*?){/repeat:subAccounts}", RegexOptions.Singleline, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex SubAccountsRepeaterRegex { get; }

    [GeneratedRegex("{repeat:fields}(.*?){/repeat:fields}", RegexOptions.Singleline, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex FieldsRepeaterRegex { get; }
}