using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Account.Models;

internal class AccountCXmlPunchOutContinueSessionSettingsModel
{
    [DefaultValue(Constants.DefaultPunchOutSessionTableName)]
    internal string PunchOutSessionTable { get; }

    [DefaultValue(Constants.DefaultPunchOutSessionQueryStringParameterName)]
    internal string PunchOutSessionQueryStringParameterName { get; }
}