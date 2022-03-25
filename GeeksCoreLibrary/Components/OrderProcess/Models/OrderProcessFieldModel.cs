using System.Collections.Generic;
using GeeksCoreLibrary.Components.OrderProcess.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// A model for a field in the order process.
    /// </summary>
    public class OrderProcessFieldModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the ID of the field to use in HTML and for saving.
        /// </summary>
        public string FieldId { get; set; }

        /// <summary>
        /// Gets or sets the label. This is the text that is shown to the user next to or above the field.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the placeholder. This is the text that is shown inside the field when no value has been entered yet.
        /// </summary>
        public string Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the type (input, radio, select etc).
        /// </summary>
        public OrderProcessFieldTypes Type { get; set; }

        /// <summary>
        /// Gets or sets the type of input field. This can be any value supported by HTML for an input field.
        /// </summary>
        public string InputFieldType { get; set; }

        /// <summary>
        /// Gets or sets the values that should be possible in a multiple choice type of field (such as radio buttons or selects).
        /// </summary>
        public Dictionary<string, string> Values { get; set; }

        /// <summary>
        /// Gets or sets whether this field should be mandatory for the user to enter a value in.
        /// </summary>
        public bool Mandatory { get; set; }

        /// <summary>
        /// Gets or sets the pattern for validating the value of the field. This should be a valid regular expression.
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Gets or sets when the field should be visible.
        /// </summary>
        public OrderProcessFieldVisibilityTypes Visibility { get; set; }
    }
}
