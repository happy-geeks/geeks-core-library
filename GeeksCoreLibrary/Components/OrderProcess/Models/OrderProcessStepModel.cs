using System.Collections.Generic;
using GeeksCoreLibrary.Components.OrderProcess.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// Model for the settings of a single step in the order process.
    /// </summary>
    public class OrderProcessStepModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the type of step this is.
        /// </summary>
        public OrderProcessStepTypes Type { get; set; }
        
        /// <summary>
        /// Gets or sets the list of groups that are contained on this step.
        /// </summary>
        public List<OrderProcessGroupModel> Groups { get; set; } = new();

        /// <summary>
        /// Gets or sets the summary text.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Gets or sets the header content / HTML.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the footer content / HTML.
        /// </summary>
        public string Footer { get; set; }

        /// <summary>
        /// Gets or sets the default text for the confirm button of this step. This value is only used when no translation can be found.
        /// </summary>
        public string ConfirmButtonText { get; set; }
        
        /// <summary>
        /// Gets or sets the default text for the link to go back to the previous step. This value is only used when no translation can be found.
        /// </summary>
        public string PreviousStepLinkText { get; set; }

        /// <summary>
        /// Gets or sets the URL to redirect the user too once they reach this step.
        /// </summary>
        public string StepRedirectUrl { get; set; }
        
        /// <summary>
        /// Gets or sets whether to hide this step in the progress part of the order process.
        /// </summary>
        public bool HideInProgress { get; set; }
    }
}
