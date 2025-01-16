using System;
using System.Diagnostics.CodeAnalysis;
using GeeksCoreLibrary.Core.Cms;

namespace GeeksCoreLibrary.Components.WebForm.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class WebFormLegacySettingsModel : CmsSettingsLegacy
{
    public string BasisHTML { get; set; }

    public string FailureHTML { get; set; }

    public string MailBody { get; set; }

    public string OntvangersAdres { get; set; }

    public string ReplyAddress { get; set; }

    public string ReplyName { get; set; }

    public string Subject { get; set; }

    public string SuccesHTML { get; set; }

    public string VerzendAdres { get; set; }

    public string VerzendNaam { get; set; }

    public string BCCAdres { get; set; }

    public int MailTemplateID { get; set; }

    public bool SaveFormValuesToDatabase { get; set; }

    public WebFormCmsSettingsModel ToSettingsModel()
    {
        return new()
        {
            HandleRequest = HandleRequest,
            EvaluateIfElseInTemplates = EvaluateIfElseInTemplates,
            RemoveUnknownVariables = RemoveUnknownVariables,
            Description = VisibleDescription,

            FormHtmlTemplate = BasisHTML,
            SuccessHtmlTemplate = SuccesHTML,
            FailureHtmlTemplate = FailureHTML,
            EmailBodyTemplate = MailBody,
            EmailSubjectTemplate = Subject,
            EmailTemplateItemId = Convert.ToUInt64(MailTemplateID),
            ReceiverAddresses = OntvangersAdres,
            Bcc = BCCAdres,
            ReplyToAddress = ReplyAddress,
            ReplyToName = ReplyName,
            SenderAddress = VerzendAdres,
            SenderName = VerzendNaam,
            SaveFormInDatabase = SaveFormValuesToDatabase
        };
    }

    public static WebFormLegacySettingsModel FromSettingsModel(WebFormCmsSettingsModel settings)
    {
        return new()
        {
            HandleRequest = settings.HandleRequest,
            EvaluateIfElseInTemplates = settings.EvaluateIfElseInTemplates,
            RemoveUnknownVariables = settings.RemoveUnknownVariables,
            VisibleDescription = settings.Description,

            BasisHTML = settings.FormHtmlTemplate,
            SuccesHTML = settings.SuccessHtmlTemplate,
            FailureHTML = settings.FailureHtmlTemplate,
            MailBody = settings.EmailBodyTemplate,
            Subject = settings.EmailSubjectTemplate,
            MailTemplateID = Convert.ToInt32(settings.EmailTemplateItemId),
            OntvangersAdres = settings.ReceiverAddresses,
            BCCAdres = settings.Bcc,
            ReplyAddress = settings.ReplyToAddress,
            ReplyName = settings.ReplyToName,
            VerzendAdres = settings.SenderAddress,
            VerzendNaam = settings.SenderName,
            SaveFormValuesToDatabase = settings.SaveFormInDatabase
        };
    }
}