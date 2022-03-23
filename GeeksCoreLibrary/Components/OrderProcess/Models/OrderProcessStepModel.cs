using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// Model for the settings of a single step in the order process.
    /// </summary>
    public class OrderProcessStepModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the list of groups that are contained on this step.
        /// </summary>
        public List<OrderProcessGroupModel> Groups { get; set; } = new();
    }
}
