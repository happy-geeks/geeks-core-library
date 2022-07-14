using System;
using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Branches.Models
{
    /// <summary>
    /// A model with settings for merging a branch back into the main/original branch.
    /// </summary>
    public class MergeBranchSettingsModel
    {
        /// <summary>
        /// Gets or sets the ID of the branch to merge.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the new database for the branch that is being merged to the main/original branch.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the date and time that the branch should be merged.
        /// </summary>
        public DateTime? StartOn { get; set; } = DateTime.Now;

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
    }
}