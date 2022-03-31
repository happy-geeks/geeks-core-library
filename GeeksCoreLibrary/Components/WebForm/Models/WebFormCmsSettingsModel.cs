using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.WebForm.Models
{
    public class WebFormCmsSettingsModel : CmsSettings
    {
        public WebForm.ComponentModes ComponentMode { get; set; } = WebForm.ComponentModes.BasicForm;

        #region Tab Layout properties

        [CmsProperty(
            PrettyName = "Form HTML template",
            Description = "The HTML template of the form where a user can enter their information.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 10
        )]
        public string FormHtmlTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Success HTML template",
            Description = "The HTML template when a form was successfully sent.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 20
        )]
        public string SuccessHtmlTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Failure HTML template",
            Description = "The HTML template when an error was encountered while trying to send the form.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 30
        )]
        public string FailureHtmlTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Template JavaScript",
            Description = "If this component requires any javascript, you can write that here.",
            DeveloperRemarks = "This JavaScript will always be added, whether an action was successful or not.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.JsEditor,
            DisplayOrder = 40
        )]
        public string TemplateJavaScript { get; set; }

        [CmsProperty(
            PrettyName = "Email subject template",
            Description = "The template of the email's subject.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            DisplayOrder = 10
        )]
        public string EmailSubjectTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Email body template",
            Description = "The HTML template of the email's body.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 20
        )]
        public string EmailBodyTemplate { get; set; }

        [CmsProperty(
            PrettyName = "Email template item ID",
            Description = "The item ID of a Wiser item containing the email's body and subject templates.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            DisplayOrder = 30
        )]
        public ulong EmailTemplateItemId { get; set; }

        #endregion

        #region Tab Behavior properties

        [CmsProperty(
            PrettyName = "Receiver addresses",
            Description = "A semicolon-separated list of email addresses that the form will be sent to.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 10
        )]
        public string ReceiverAddresses { get; set; }

        [CmsProperty(
            PrettyName = "BCC",
            Description = "A semicolon-separated list of hidden email addresses that will also receive a copy.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 20
        )]
        public string Bcc { get; set; }

        [CmsProperty(
            PrettyName = "Reply address",
            Description = "The email address that will be used when a user replies to the email that was sent to them.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 30
        )]
        public string ReplyToAddress { get; set; }

        [CmsProperty(
            PrettyName = "Reply name",
            Description = "The name that will be used when a user replies to the email that was sent to them.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 40
        )]
        public string ReplyToName { get; set; }

        [CmsProperty(
            PrettyName = "Sender address",
            Description = "The email address of the sender.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 50
        )]
        public string SenderAddress { get; set; }

        [CmsProperty(
            PrettyName = "Sender name",
            Description = "The name of the sender.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Handling,
            DisplayOrder = 60
        )]
        public string SenderName { get; set; }

        [CmsProperty(
            PrettyName = "Save form in database",
            Description = "Saves entered values in the database",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.DataHandling,
            DisplayOrder = 10
        )]
        public bool SaveFormInDatabase { get; set; }

        [CmsProperty(
            PrettyName = "reCAPTCHA v3 Score threshold",
            Description = "The minimum score a user needs to be given by reCAPTCHA v3 to be allowed to submit this form. Scores can be between 0.0 and 1.0, where 0.0 means it's most likely a bot and 1.0 means it's most likely a good interaction.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Validation,
            DisplayOrder = 10
        )]
        public decimal ReCaptchaV3ScoreThreshold { get; set; }

        #endregion
    }
}
