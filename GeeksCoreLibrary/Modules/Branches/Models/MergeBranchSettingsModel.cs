using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Branches.Models
{
    /// <summary>
    /// A model with settings for merging a branch back into the main/original branch.
    /// </summary>
    public class MergeBranchSettingsModel : BranchActionBaseModel
    {
        /// <summary>
        /// Gets or sets whether the branch should be deleted after/if the merge was successful.
        /// </summary>
        public bool DeleteAfterSuccessfulMerge { get; set; }

        /// <summary>
        /// Gets or sets the settings per entity of what should be merged.
        /// </summary>
        public List<EntityMergeSettingsModel> Entities { get; set; }

        /// <summary>
        /// Gets or sets the settings per entity of what should be merged.
        /// </summary>
        public List<SettingMergeSettingsModel> Settings { get; set; }
        
        /// <summary>
        /// Gets or sets the linktypes too see what should be merged.
        /// </summary>
        public List<LinkTypeMergeSettingsModel> LinkTypes { get; set; }

        /// <summary>
        /// Gets or sets the settings for how the user wants to handle conflicts.
        /// </summary>
        public List<MergeConflictModel> ConflictSettings { get; set; }

        /// <summary>
        /// Gets or sets whether to check for conflicts or just merge everything.
        /// </summary>
        public bool CheckForConflicts { get; set; } = true;
    }
}