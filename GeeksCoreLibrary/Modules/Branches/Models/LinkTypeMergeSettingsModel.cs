namespace GeeksCoreLibrary.Modules.Branches.Models
{
    /// <inheritdoc />
    public class LinkTypeMergeSettingsModel : ObjectMergeSettingsModel
    {
        /// <summary>
        /// Gets or sets the link type to merge.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the source item if this link type.
        /// </summary>
        public string SourceEntityType { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the destination item if this link type.
        /// </summary>
        public string DestinationEntityType { get; set; }
    }
}