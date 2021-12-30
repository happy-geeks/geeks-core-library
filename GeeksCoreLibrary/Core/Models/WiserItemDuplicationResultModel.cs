namespace GeeksCoreLibrary.Core.Models
{
    public class WiserItemDuplicationResultModel
    {
        /// <summary>
        /// Gets or sets the encrypted ID of the new item.
        /// </summary>
        public string NewItemId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the new item.
        /// </summary>
        public ulong NewItemIdPlain { get; set; }

        /// <summary>
        /// Gets or sets the icon of the new item.
        /// This is used in the tree view in Wiser 2.0.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the link ID of the new item.
        /// </summary>
        public ulong NewLinkId { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets whether the item has children.
        /// </summary>
        public bool Haschilds { get; set; }
    }
}
