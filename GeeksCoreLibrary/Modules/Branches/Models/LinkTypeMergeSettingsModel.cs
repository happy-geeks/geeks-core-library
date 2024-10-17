using GeeksCoreLibrary.Modules.Branches.Enumerations;

namespace GeeksCoreLibrary.Modules.Branches.Models
{
    /// <inheritdoc />
    public class LinkTypeMergeSettingsModel : ObjectMergeSettingsModel
    {
        /// <summary>
        /// Gets or sets the link type to merge.
        /// </summary>
        public string Type { get; set; }
    }
}