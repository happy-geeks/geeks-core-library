using System;
using System.Collections.Generic;

namespace GeeksCoreLibrary.Core.Models
{
    public class WiserItemLinkModel
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the source item ID.
        /// </summary>
        public ulong ItemId { get; set; }

        /// <summary>
        /// Gets or sets the destination item ID.
        /// </summary>
        public ulong DestinationItemId { get; set; }

        /// <summary>
        /// Gets or sets the sort order number.
        /// </summary>
        public int Ordering { get; set; }

        /// <summary>
        /// Gets or sets the link type.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the date and time that this link was created.
        /// </summary>
        public DateTime AddedOn { get; set; }

        /// <summary>
        /// Gets or sets the details for this item link.
        /// </summary>
        public List<WiserItemDetailModel> Details { get; set; } = new List<WiserItemDetailModel>();

        /// <summary>
        /// Gets or sets whether to use the column parent_item_id from wiser_item to link, instead of using the table wiser_itemlink.
        /// </summary>
        public bool UseParentItemId { get; set; }
    }
}
