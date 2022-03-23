using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    public class OrderProcessCmsSettingsModel : CmsSettings
    {
        public OrderProcess.ComponentModes ComponentMode { get; set; } = OrderProcess.ComponentModes.Automatic;
        
        #region Tab DataSource properties

        /// <summary>
        /// The Wiser item ID of the order process that should be retrieved.
        /// </summary>
        [CmsProperty(
            PrettyName = "Order process item ID",
            Description = "The Wiser item ID of the order process that should be retrieved.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10
        )]
        public ulong OrderProcessId { get; set; }

        #endregion

        #region Tab layout properties
        
        /// <summary>
        /// The main template.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template",
            Description = "The main template.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic,PaymentMethods",
            DisplayOrder = 10
        )]
        public string Template { get; set; }
        
        /// <summary>
        /// The template for a step in the order process.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template step",
            Description = "The template for a step in the order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 20
        )]
        public string TemplateStep { get; set; }
        
        /// <summary>
        /// The template for a group of fields in the order process.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template group",
            Description = "The template for a group of fields in the order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 30
        )]
        public string TemplateGroup { get; set; }
        
        /// <summary>
        /// The template for a normal input field in the order process.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template input field",
            Description = "The template for a normal input field in the order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 40
        )]
        public string TemplateInputField { get; set; }
        
        /// <summary>
        /// The template for a radio button field in the order process.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template radio button field",
            Description = "The template for a radio button field in the order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 50
        )]
        public string TemplateRadioButtonField { get; set; }
        
        /// <summary>
        /// The template for a single option in a radio button field in the order process
        /// </summary>
        [CmsProperty(
            PrettyName = "Template radio button option",
            Description = "The template for a single option in a radio button field in the order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 55
        )]
        public string TemplateRadioButtonFieldOption { get; set; }
        
        /// <summary>
        /// The template for a select / combobox field in the order process.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template select field",
            Description = "The template for a select / combobox field in the order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 60
        )]
        public string TemplateSelectField { get; set; }
        
        /// <summary>
        /// The template for a single option in a select field in the order process
        /// </summary>
        [CmsProperty(
            PrettyName = "Template select option",
            Description = "The template for a single option in a select field in the order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 65
        )]
        public string TemplateSelectFieldOption { get; set; }
        
        /// <summary>
        /// The template for a checkbox field in the order process.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template checkbox field",
            Description = "The template for a checkbox field in the order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 70
        )]
        public string TemplateCheckboxField { get; set; }
        
        /// <summary>
        /// The template for showing the progress of the user in a multi step order process.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template progress",
            Description = "The template for showing the progress of the user in a multi step order process.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 80
        )]
        public string TemplateProgress { get; set; }
        
        /// <summary>
        /// The template for a single step for 'TemplateProgress'.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template progress step",
            Description = "The template for a single step for 'TemplateProgress'.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic",
            DisplayOrder = 80
        )]
        public string TemplateProgressStep { get; set; }
        
        /// <summary>
        /// The template for a single payment method.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template payment method",
            Description = "The template for a single payment method.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            ComponentMode = "Automatic,PaymentMethods",
            DisplayOrder = 90
        )]
        public string TemplatePaymentMethod { get; set; }

        #endregion
    }
}
