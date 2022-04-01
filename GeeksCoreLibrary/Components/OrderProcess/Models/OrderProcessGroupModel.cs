using System.Collections.Generic;
using GeeksCoreLibrary.Components.OrderProcess.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// A model for the settings of a field group in the order process.
    /// </summary>
    public class OrderProcessGroupModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the type of group.
        /// </summary>
        public OrderProcessGroupTypes Type { get; set; }

        /// <summary>
        /// Gets or sets the header content / HTML.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the footer content / HTML.
        /// </summary>
        public string Footer { get; set; }

        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        public List<OrderProcessFieldModel> Fields { get; set; } = new();

        /// <summary>
        /// Gets or sets any extra CSS classes for this group.
        /// </summary>
        public string CssClass { get; set; }
    }
}
