using System;

namespace GeeksCoreLibrary.Modules.Branches.Models
{
    /// <summary>
    /// A model with information about a single merge conflict.
    /// </summary>
    public class MergeConflictModel
    {
        /// <summary>
        /// Gets or sets the ID of the item that has a conflict.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the title of the item.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the type that was changed. When an item has been changed, this is the entity type, otherwise it's the type of setting (module, entity property etc).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the display name of the entity type.
        /// </summary>
        public string EntityTypeDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the name of the field that has a conflict.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the field.
        /// </summary>
        public string FieldDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the value of the field in the main/original branch.
        /// </summary>
        public string ValueInMain { get; set; }

        /// <summary>
        /// Gets or sets the value of the field in the branch that is being merged.
        /// </summary>
        public string ValueInBranch { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the change in the main/original branch.
        /// </summary>
        public DateTime ChangeDateInMain { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the change in the branch that is being merged.
        /// </summary>
        public DateTime ChangeDateInBranch { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that made the change in the main/original branch.
        /// </summary>
        public string ChangedByInMain { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that made the change in the branch that is being merged.
        /// </summary>
        public string ChangedByInBranch { get; set; }

        /// <summary>
        /// Gets or sets whether the user has accepted the change that caused a conflict.
        /// If this value is true, the use wants to use the change from the branch that is being merged, if it's false the user wants to keep the change that was done in the main/original branch. 
        /// </summary>
        public bool? AcceptChange { get; set; }

        /// <summary>
        /// Gets or sets the name of the table in which the change was done.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the language code. Only applicable for entity properties.
        /// </summary>
        public string LanguageCode { get; set; }

        /// <summary>
        /// Gets or sets the group name. Only applicable for entity properties.
        /// </summary>
        public string GroupName { get; set; }
    }
}