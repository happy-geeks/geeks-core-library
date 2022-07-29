using System.ComponentModel.DataAnnotations;
using GeeksCoreLibrary.Core.Enums;

namespace GeeksCoreLibrary.Core.Models
{
    /// <summary>
    /// A model for link settings, for linking items to other items.
    /// </summary>
    public class LinkSettingsModel
    {
        /// <summary>
        /// Gets or sets the ID of the settings.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the link type number. Make sure you have a unique ID for every link type.
        /// </summary>
        [Required]
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the destination (= parent) item.
        /// </summary>
        [Required]
        public string DestinationEntityType { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the source (= child) item.
        /// </summary>
        [Required]
        public string SourceEntityType { get; set; }

        /// <summary>
        /// Gets or sets the name of the link type.
        /// This should be a user friendly name, as it's shown in Wiser in some places like the import module.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether or not to show items, that are linked with this type, in the default tree view in Wiser.
        /// </summary>
        public bool ShowInTreeView { get; set; }

        /// <summary>
        /// Gets or sets whether or not to show this link type in the data selector module in Wiser, as an option for getting linked items.
        /// </summary>
        public bool ShowInDataSelector { get; set; }

        /// <summary>
        /// Gets or sets the relationship of items that use this link type.
        /// </summary>
        public LinkRelationships Relationship { get; set; }

        /// <summary>
        /// Gets or sets what should happen with all source items that use this link type, when a destination item is being duplicated.
        /// </summary>
        public LinkDuplicationMethods DuplicationMethod { get; set; }

        /// <summary>
        /// Gets or sets whether or not to use the parent_item_id column from wiser_item instead of the wiser_itemlink table.
        /// </summary>
        public bool UseItemParentId { get; set; }

        /// <summary>
        /// Gets or sets whether to use a dedicated table for links of this type.
        /// The GCL and Wiser expect there to be a table "[linkType]_wiser_itemlink" to store the links in. So if your link type is "1", we will use the table "1_wiser_itemlink" instead of "wiser_itemlink".
        /// This table will not be created automatically. To create this table, make a copy of wiser_itemlink (including triggers, but the the name of the table in the triggers too).
        /// </summary>
        public bool UseDedicatedTable { get; set; }

        /// <summary>
        /// Gets or sets whether to also delete children when the parent is being deleted.
        /// </summary>
        public bool CascadeDelete { get; set; }
    }
}