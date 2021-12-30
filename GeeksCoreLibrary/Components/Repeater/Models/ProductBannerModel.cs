namespace GeeksCoreLibrary.Components.Repeater.Models
{
    public class ProductBannerModel
    {
        public enum PlacingMethods
        {
            Unknown = 0,

            /// <summary>
            /// The banner appears on a single position.
            /// </summary>
            Fixed = 1,

            /// <summary>
            /// The banner repeats every X positions.
            /// </summary>
            Repeating = 2
        }

        /// <summary>
        /// Gets or sets the ID of the banner item.
        /// </summary>
        public ulong ItemId { get;set; }

        /// <summary>
        /// Gets or sets the name of the banner item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the base URL where the banner should be shown.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets an extra filter to show this banner on more specific URLs.
        /// Multiple filters can be added by separating them with a semicolon (;).
        /// </summary>
        public string UrlContains { get; set; }

        /// <summary>
        /// Gets or sets the fixed position of the banner, or every Nth banner.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets or sets the placing method.
        /// </summary>
        public PlacingMethods Method { get; set; }

        /// <summary>
        /// Gets or sets the (HTML) content of the banner.
        /// </summary>
        public string Content { get; set; } = "";

        /// <summary>
        /// Gets or sets the language code this banner is used for.
        /// </summary>
        public string LanguageCode { get; set; }

        /// <summary>
        /// Gets or sets how many items are replaced by this banner.
        /// </summary>
        public int BannerSize { get; set; }

        /// <summary>
        /// Gets or sets whether a banner is handled on a specific position. This is for internal use only.
        /// </summary>
        public bool Handled { get; set; }
    }
}
