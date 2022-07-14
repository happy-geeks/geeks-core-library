using GeeksCoreLibrary.Modules.Branches.Enumerations;

namespace GeeksCoreLibrary.Modules.Branches.Models
{
    /// <inheritdoc />
    public class SettingMergeSettingsModel : ObjectMergeSettingsModel
    {
        /// <summary>
        /// Gets or sets the type of setting to merge.
        /// </summary>
        public WiserSettingTypes Type { get; set; }
    }
}