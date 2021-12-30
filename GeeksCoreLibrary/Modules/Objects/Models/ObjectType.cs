namespace GeeksCoreLibrary.Modules.Objects.Models
{
    public class ObjectType
    {
        /// <summary>
        /// Gets or sets the ID (often also called "TypeNumber").
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the level number.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the ID of the parent object type.
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Gets or sets the description/name.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the short code. This value cannot be seen or changed by the customer, so it can be used for queries since this can only change if we want it to.
        /// </summary>
        public string ShortCode { get; set; }

        /// <summary>
        /// Gets or sets whether this is editable in Wiser.
        /// </summary>
        public bool Editable { get; set; }

        /// <summary>
        /// Gets or sets the long description. This is not visible in Wiser.
        /// </summary>
        public string LongDescription { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets whether this is visible for the customer in Wiser.
        /// </summary>
        public bool VisibleForCustomer { get; set; }
    }
}
