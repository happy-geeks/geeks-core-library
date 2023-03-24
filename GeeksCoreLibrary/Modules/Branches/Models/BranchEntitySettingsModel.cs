using System;
using GeeksCoreLibrary.Modules.Branches.Enumerations;

namespace GeeksCoreLibrary.Modules.Branches.Models
{
    /// <summary>
    /// A model for settings of a single entity type, for creating a new branch.
    /// </summary>
    public class BranchEntitySettingsModel
    {
        /// <summary>
        /// Gets or sets the entity type.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the mode of what items of this entity type to copy to the new branch.
        /// </summary>
        public CreateBranchEntityModes Mode { get; set; }

        /// <summary>
        /// Gets or sets the amount of items that should be copied. Only applicable for certain modes.
        /// </summary>
        public int AmountOfItems { get; set; }

        /// <summary>
        /// Gets or sets the start date for items to copy.
        /// This means that only items that were created after this date will be copied.
        /// Only applicable for certain modes.
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// Gets or sets the end date for the items to copy.
        /// This means that only items that were created before this date will be copied.
        /// Only applicable for certain modes.
        /// </summary>
        public DateTime? End { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the data selector for the items to copy.
        /// This means that only items that are retrieved with the data selector will be copied.
        /// Only applicable for certain modes.
        /// </summary>
        public int DataSelector { get; set; }
    }
}