using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Account.Models
{
    internal class AccountCXmlPunchOutLoginSettingsModel
    {
        [DefaultValue(Constants.DefaultPunchOutSessionTableName)]
        internal string PunchOutSessionTable { get; }

        [DefaultValue(Constants.DefaultPunchOutSessionQueryStringParameterName)]
        internal string PunchOutSessionQueryStringParameterName { get; }

        [DefaultValue(Constants.DefaultOciUsernameKey)]
        internal int OciUsernameKey { get; }

        [DefaultValue(Constants.DefaultOciPasswordKey)]
        internal int OciPasswordKey { get; }

        [DefaultValue(Constants.DefaultOciHookUrlKey)]
        internal int OciHookUrlKey { get; }
    }
}
