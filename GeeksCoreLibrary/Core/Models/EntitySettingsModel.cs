using System.Collections.Generic;
using GeeksCoreLibrary.Core.Enums;

namespace GeeksCoreLibrary.Core.Models
{
    public class EntitySettingsModel
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the entity type.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

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
        /// Gets or sets the query to be executed before an item of this type is being updated.
        /// </summary>
        public string QueryBeforeUpdate { get; set; }

        /// <summary>
        /// Gets or sets the query to be executed before an item of this type is being deleted or archived.
        /// </summary>
        public string QueryBeforeDelete { get; set; }

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

        /// <summary>
        /// Gets or sets the icon that should be used for items of this entity type in the tree view of Wiser, when the item is collapsed.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the icon that should be shown next to the option to create a new item of this type, in the tree view of Wiser.
        /// </summary>
        public string IconAdd { get; set; }

        /// <summary>
        /// Gets or sets the icon that should be used for items of this entity type in the tree view of Wiser, when the item is expanded.
        /// </summary>
        public string IconExpanded { get; set; }

        /// <summary>
        /// Gets or sets the color that items of this type should have in Wiser. At the moment, this is only used in the search module in Wiser. 
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets whether items of this type should be able to be searched for in the search module in Wiser.
        /// </summary>
        public bool ShowInSearch { get; set; }

        /// <summary>
        /// Gets or sets the API to be executed after a new item of this type has been created.
        /// This should be an ID for the table wiser_api_connection.
        /// </summary>
        public int? ApiAfterInsert { get; set; }

        /// <summary>
        /// Gets or sets the API to be executed after an item of this type has been updated.
        /// This should be an ID for the table wiser_api_connection.
        /// </summary>
        public int? ApiAfterUpdate { get; set; }

        /// <summary>
        /// Gets or sets the API to be executed before an item of this type is being updated.
        /// This should be an ID for the table wiser_api_connection.
        /// </summary>
        public int? ApiBeforeUpdate { get; set; }

        /// <summary>
        /// Gets or sets the API to be executed before an item of this type is being deleted or archived.
        /// This should be an ID for the table wiser_api_connection.
        /// </summary>
        public int? ApiBeforeDelete { get; set; }

        /// <summary>
        /// Gets or sets whether to save any changes to items of this type to wiser_history.
        /// </summary>
        public bool SaveHistory { get; set; }

        /// <summary>
        /// Gets or sets the default ordering of items of this type in the tree view of Wiser.
        /// </summary>
        public EntityOrderingTypes DefaultOrdering { get; set; }

        /// <summary>
        /// Gets or sets what should be done when items of this type are being deleted.
        /// </summary>
        public EntityDeletionTypes DeleteAction { get; set; } = EntityDeletionTypes.Archive;

        /// <summary>
        /// Gets or sets the query to use for items that are added as template blocks.
        /// You can use '{itemId}' or '?itemId' to use the ID of the item in the query.
        /// </summary>
        public string TemplateQuery { get; set; }

        /// <summary>
        /// Gets or sets the HTML template to use for items that are added as template blocks.
        /// You can use any values that the query from <see cref="TemplateQuery"/> returns in here as replacements.
        /// So if the query returns a column "title", you can place that value in the HTML by adding "{title}" to the template.
        /// </summary>
        public string TemplateHtml { get; set; }
        
        /// <summary>
        /// Gets or sets whether statistics of this entity type should be shown in the dashboard module in Wiser.
        /// </summary>
        public bool ShowInDashboard { get; set; }

        /// <summary>
        /// Gets or Sets the where item details of this entity type get stored.
        /// </summary>
        public StorageLocation StorageLocation { get; set; } = StorageLocation.Table;
    }
}
