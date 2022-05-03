using System.Collections.Generic;

namespace GeeksCoreLibrary.Core.Models
{
    public class EntitySettingsModel
    {
        /// <summary>
        /// Gets or sets the name of the entity type.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the default module ID that items of this type will be used in.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets all extra options for all fields.
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> FieldOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets all fields with auto increment values.
        /// </summary>
        public List<(string PropertyName, string LanguageCode)> AutoIncrementFields { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to also save the title as a SEO value in the details.
        /// </summary>
        public bool SaveTitleAsSeo { get; set; }

        /// <summary>
        /// Gets or sets the query to be executed after a new item of this type has been created.
        /// </summary>
        public string QueryAfterInsert { get; set; }

        /// <summary>
        /// Gets or sets the query to be executed after an item of this type has been updated.
        /// </summary>
        public string QueryAfterUpdate { get; set; }

        /// <summary>
        /// Gets or sets the prefix for the dedicated tables for items of this type.
        /// If this contains a value, then items of this type will be saved in [prefix]_wiser_item and [prefix]_wiser_itemdetail.
        /// </summary>
        public string DedicatedTablePrefix { get; set; }

        /// <summary>
        /// Gets or sets whether items of this type can have different versions for each environment.
        /// </summary>
        public bool EnableMultipleEnvironments { get; set; }

        /// <summary>
        /// Gets or sets what kind of entities items of this type can have as children.
        /// </summary>
        public List<string> AcceptedChildTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets whether items of this type should be shown in the tree view of the corresponding module in Wiser.
        /// </summary>
        public bool ShowInTreeView { get; set; }

        /// <summary>
        /// Gets or sets whether to show the overview tab in Wiser for items of this type.
        /// The overview tab will show a grid with all children of the item.
        /// </summary>
        public bool ShowOverviewTab { get; set; }

        /// <summary>
        /// Gets or sets whether to show the title field on items of this type in Wiser.
        /// </summary>
        public bool ShowTitleField { get; set; }
    }
}
