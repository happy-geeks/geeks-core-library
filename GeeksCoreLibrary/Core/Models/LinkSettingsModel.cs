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
        /// Gets or sets the selection method.
        /// TODO: Figure out what this is for, doesn't seem to be used anywhere. Or remove this property if we don't need it.
        /// </summary>
        public LinkSelectionMethods SelectionMethod { get; set; }

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
    }
}