using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.WebForm.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace GeeksCoreLibrary.Components.WebForm;

public class WebForm : CmsComponent<WebFormCmsSettingsModel, WebForm.ComponentModes>
{
    private readonly GclSettings gclSettings;
    private readonly IWiserItemsService wiserItemsService;
    private readonly ICommunicationsService communicationsService;
    private readonly ILanguagesService languagesService;
    private readonly IObjectsService objectsService;

    #region Internal variables

    private bool allowLegacyEmailTemplateGeneration;

    #endregion

    #region Enums

    public enum ComponentModes
    {
        BasicForm = 1,

        /// <summary>
        /// Legacy support for the JCL Sendform component.
        /// </summary>
        [CmsEnum(HideInCms = true)]
        Legacy = 2
    }

    #endregion

    #region Constructor

    public WebForm(ILogger<WebForm> logger, IOptions<GclSettings> gclSettings, IStringReplacementsService stringReplacementsService, IWiserItemsService wiserItemsService, ICommunicationsService communicationsService, ILanguagesService languagesService, ITemplatesService templatesService, IObjectsService objectsService)
    {
        this.gclSettings = gclSettings.Value;
        this.wiserItemsService = wiserItemsService;
        this.communicationsService = communicationsService;
        this.languagesService = languagesService;
        this.objectsService = objectsService;

        Logger = logger;
        StringReplacementsService = stringReplacementsService;
        TemplatesService = templatesService;

        Settings = new WebFormCmsSettingsModel();
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    public override async Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData)
    {
        if (Request == null)
        {
            throw new Exception("WebForm component requires an http context, but it's null, so can't continue!");
        }

        ComponentId = dynamicContent.Id;
        Settings.Description = dynamicContent.Title;
        if (dynamicContent.Name is "JuiceControlLibrary.Sendform" && !forcedComponentMode.HasValue)
        {
            // Force component mode to Legacy mode if it was created through the JCL.
            Settings.ComponentMode = ComponentModes.Legacy;
            allowLegacyEmailTemplateGeneration = true;
        }

        ParseSettingsJson(dynamicContent.SettingsJson, forcedComponentMode);
        if (forcedComponentMode.HasValue)
        {
            Settings.ComponentMode = (ComponentModes) forcedComponentMode.Value;
        }
        else if (!String.IsNullOrWhiteSpace(dynamicContent.ComponentMode))
        {
            Settings.ComponentMode = Enum.Parse<ComponentModes>(dynamicContent.ComponentMode);
        }

        HandleDefaultSettingsFromComponentMode();

        var (renderHtml, debugInformation) = await ShouldRenderHtmlAsync();
        if (!renderHtml)
        {
            ViewBag.Html = debugInformation;
            return new HtmlString(debugInformation);
        }

        // Check if we need to call a specific method and then do so. Skip everything else, because we don't want to render the entire component then.
        if (!String.IsNullOrWhiteSpace(callMethod))
        {
            TempData["InvokeMethodResult"] = await InvokeMethodAsync(callMethod);
            return new HtmlString(String.Empty);
        }

        var resultHtml = new StringBuilder();
        if (!Request.HasFormContentType)
        {
            resultHtml.Append(await CreateFormHtmlAsync());
        }
        else
        {
            resultHtml.Append(await SubmitFormAsync());
        }

        if (!String.IsNullOrWhiteSpace(Settings.TemplateJavaScript))
        {
            var javascript = Settings.TemplateJavaScript.Replace("{contentId}", ComponentId.ToString(), StringComparison.OrdinalIgnoreCase).Replace("{WebFormName}", Settings.Description, StringComparison.OrdinalIgnoreCase);
            resultHtml.Append($"<script>{javascript}</script>");
        }

        Logger.LogDebug("WebForm - End generating HTML.");

        return new HtmlString(resultHtml.ToString());
    }

    #endregion

    /// <summary>
    /// Creates the form HTML based on the form HTML template as set in the settings.
    /// </summary>
    /// <returns>The generated form HTML as a string.</returns>
    public async Task<string> CreateFormHtmlAsync()
    {
        if (String.IsNullOrWhiteSpace(Settings.FormHtmlTemplate))
        {
            return String.Empty;
        }

        var formHtml = Settings.FormHtmlTemplate;

        formHtml = formHtml.Replace("{contentId}", ComponentId.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("<jform", "<form", StringComparison.OrdinalIgnoreCase)
            .Replace("</jform", "</form", StringComparison.OrdinalIgnoreCase);

        // Check if reCAPTCHA should be placed.
        if (formHtml.Contains("{recaptcha"))
        {
            formHtml = await PlaceReCaptchaInHtml(formHtml);
        }

        // Place some hidden fields at the end of the form.
        var endTagIndex = formHtml.LastIndexOf("</form>", StringComparison.Ordinal);
        if (endTagIndex > -1)
        {
            var hiddenInputs = $"<input type=\"hidden\" name=\"__WebForm{ComponentId}\" value=\"{ComponentId}\" /><input type=\"hidden\" name=\"__WebFormCheck{ComponentId}\" value=\"\" />";
            formHtml = formHtml.Insert(endTagIndex, hiddenInputs);
        }

        formHtml = await TemplatesService.DoReplacesAsync(formHtml, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, handleRequest: Settings.HandleRequest, removeUnknownVariables: Settings.RemoveUnknownVariables);

        return formHtml;
    }

    /// <summary>
    /// Attempts to place the required reCAPTCHA in the HTML.
    /// </summary>
    /// <param name="input">The HTML to update.</param>
    /// <returns>The input HTML with the reCAPTCHA variable replaced.</returns>
    private async Task<string> PlaceReCaptchaInHtml(string input)
    {
        var regexMatches = Regex.Matches(input, @"\{recaptcha_v(?<version>\d)\}");
        if (regexMatches.Count == 0)
        {
            Logger.LogWarning("Couldn't place reCAPTCHA in the form. The variable is malformed.");
            return input;
        }

        var siteKey = await objectsService.FindSystemObjectByDomainNameAsync("google_recaptcha_sitekey");
        var updatedHtml = input;
        foreach (Match regexMatch in regexMatches)
        {
            // The version of reCAPTCHA to use.
            var version = Int32.Parse(regexMatch.Groups["version"].Value);

            switch (version)
            {
                case 2:
                    updatedHtml = updatedHtml.Replace(regexMatch.Value, $"<div class=\"g-recaptcha\" data-sitekey=\"{siteKey}\"></div>");
                    AddExternalJavaScriptLibrary("https://www.google.com/recaptcha/api.js", true, true);
                    break;
                case 3:
                    // In version 3, the javascript will already be added by PagesService.
                    updatedHtml = updatedHtml.Replace(regexMatch.Value, $"<input type=\"hidden\" name=\"g-recaptcha-response-v3\" id=\"RecaptchaResponseToken{ComponentId}\" value>");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), version, $"Unknown or unsupported reCAPTCHA version: {version}");
            }
        }

        return updatedHtml;
    }

    /// <summary>
    /// Submits the form, and returns either the HTML of the success template or the HTML of the failed template.
    /// </summary>
    /// <returns>The HTML of the success template or the HTML of the failed template.</returns>
    public async Task<string> SubmitFormAsync()
    {
        if (!await ValidateFormSubmitAsync())
        {
            return await CreateFormHtmlAsync();
        }

        var communication = new SingleCommunicationModel();

        // Check if there are files to upload.
        if (Request.Form.Files.Count > 0)
        {
            communication.WiserItemFiles = new List<ulong>(Request.Form.Files.Count);

            foreach (var formFile in Request.Form.Files)
            {
                await using var stream = new MemoryStream();
                await formFile.CopyToAsync(stream);

                var itemFile = new WiserItemFileModel
                {
                    ContentType = formFile.ContentType,
                    Content = stream.ToArray(),
                    FileName = Path.GetFileName(formFile.FileName),
                    Extension = Path.GetExtension(formFile.FileName),
                    Title = Path.GetFileNameWithoutExtension(formFile.FileName),
                    PropertyName = "form_attachment",
                    Protected = true
                };
                communication.WiserItemFiles.Add(await wiserItemsService.AddItemFileAsync(itemFile, skipPermissionsCheck: true));
            }
        }

        // Make sure the language code has a value.
        if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
        {
            // This function fills the property "CurrentLanguageCode".
            await languagesService.GetLanguageCodeAsync();
        }

        if (Settings.EmailTemplateItemId > 0)
        {
            var wiserItem = await wiserItemsService.GetItemDetailsAsync(Settings.EmailTemplateItemId, languageCode: languagesService.CurrentLanguageCode, skipPermissionsCheck: true);
            if (wiserItem is {Id: > 0})
            {
                communication.Content = await StringReplacementsService.DoAllReplacementsAsync(wiserItem.GetDetailValue("template"), handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);
                communication.Subject = await StringReplacementsService.DoAllReplacementsAsync(wiserItem.GetDetailValue("subject"), handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);
            }
        }
        else if (!String.IsNullOrWhiteSpace(Settings.EmailBodyTemplate))
        {
            communication.Content = await StringReplacementsService.DoAllReplacementsAsync(Settings.EmailBodyTemplate, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);
            communication.Subject = await StringReplacementsService.DoAllReplacementsAsync(Settings.EmailSubjectTemplate, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);
        }
        else if (allowLegacyEmailTemplateGeneration)
        {
            // Legacy allows a mail body to be generated based on details in the Form values.
            communication.Content = CreateMailBodyFromFormValues();
            communication.Subject = await StringReplacementsService.DoAllReplacementsAsync(Settings.EmailSubjectTemplate, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);
        }

        if (String.IsNullOrWhiteSpace(communication.Content))
        {
            Logger.LogError("WebForm: Cannot send Form due to there being no valid template for the email set.");
            throw new Exception("WebForm: Cannot send Form due to there being no valid template for the email set.");
        }

        communication.Sender = Settings.SenderAddress;
        communication.SenderName = Settings.SenderName;
        communication.Receivers = Settings.ReceiverAddresses.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(receiver => new CommunicationReceiverModel {Address = receiver, DisplayName = Settings.SenderName}).ToList();
        communication.Bcc = Settings.Bcc.Split(';', StringSplitOptions.RemoveEmptyEntries);
        communication.ReplyTo = Settings.ReplyToAddress;
        communication.ReplyToName = Settings.ReplyToName;

        try
        {
            if (gclSettings.SmtpSettings != null)
            {
                try
                {
                    await communicationsService.SendEmailDirectlyAsync(communication, 2000);
                    communication.ProcessedDate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error sending email: {ex}");
                }
            }

            // Save the email in the Wiser communication table, regardless if it was successful.
            await communicationsService.SendEmailAsync(communication);

            if (Settings.SaveFormInDatabase)
            {
                // Save the form as a Wiser item, and all form values as the item's details.
                await SaveFormInDatabaseAsync();
            }

            return await StringReplacementsService.DoAllReplacementsAsync(Settings.SuccessHtmlTemplate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"WebForm - Error while trying to submit form: {ex}");
            return await StringReplacementsService.DoAllReplacementsAsync(Settings.FailureHtmlTemplate);
        }
    }

    /// <summary>
    /// Validates whether the submitted form belongs to this WebForm.
    /// </summary>
    /// <returns>Whether the form clear to submit.</returns>
    public async Task<bool> ValidateFormSubmitAsync()
    {
        if (!Request.HasFormContentType || Request.Form.Count == 0)
        {
            return false;
        }

        // reCAPTCHA v2
        if (Settings.FormHtmlTemplate.Contains("{recaptcha_v2}") && (!Request.Form.TryGetValue("g-recaptcha-response", out var recaptchaResponse) || String.IsNullOrWhiteSpace(recaptchaResponse.ToString()) || !await ValidateRecaptchaResponseAsync(recaptchaResponse.ToString())))
        {
            return false;
        }

        // reCAPTCHA v3
        if (Settings.FormHtmlTemplate.Contains("{recaptcha_v3}") && (!Request.Form.TryGetValue("g-recaptcha-response-v3", out recaptchaResponse) || String.IsNullOrWhiteSpace(recaptchaResponse.ToString()) || !await ValidateRecaptchaResponseAsync(recaptchaResponse.ToString())))
        {
            return false;
        }

        return Request.Form.TryGetValue($"__WebForm{ComponentId}", out var value1) && value1.Equals(ComponentId.ToString())
                                                                                   && Request.Form.TryGetValue($"__WebFormCheck{ComponentId}", out var value2) && value2.Equals(String.Empty);
    }

    private async Task<bool> ValidateRecaptchaResponseAsync(string response)
    {
        var isVersion3 = Settings.FormHtmlTemplate.Contains("{recaptcha_v3}");
        var secret = await objectsService.FindSystemObjectByDomainNameAsync(isVersion3 ? "google_recaptcha_v3_secretkey" : "google_recaptcha_secretkey");

        var restClient = new RestClient("https://www.google.com");
        var restRequest = new RestRequest("/recaptcha/api/siteverify", Method.Post);
        restRequest.AddParameter("secret", secret, ParameterType.GetOrPost);
        restRequest.AddParameter("response", response, ParameterType.GetOrPost);

        var restResult = await restClient.ExecuteAsync(restRequest);
        if (!restResult.IsSuccessful)
        {
            return false;
        }

        var dataObject = JObject.Parse(restResult.Content);
        var result = dataObject.Value<bool>("success");
        if (!isVersion3 || !result)
        {
            return result;
        }

        // For reCAPTCHA v3 we need to check the score that was returned.
        var score = dataObject.Value<decimal>("score");
        return score >= Settings.ReCaptchaV3ScoreThreshold;
    }

    /// <summary>
    /// Creates an email body based on values in the Form collection. This is only for Legacy mode, as the Sendform component in the JCL also did this.
    /// </summary>
    /// <returns>A standardized mail body, based purely on the data in the form collection.</returns>
    private string CreateMailBodyFromFormValues()
    {
        var emailBodyBuilder = new StringBuilder();

        emailBodyBuilder.Append("<table>");

        foreach (var formKey in Request.Form.Keys)
        {
            if (formKey.StartsWith("__"))
            {
                continue;
            }

            emailBodyBuilder.Append("<tr><td style=\"font-family: Arial; font-size: 11px;\" valign=\"top\">");
            emailBodyBuilder.Append($"<strong>{formKey}:</strong>");
            emailBodyBuilder.Append("</td><td width=\"10\"></td>");
            emailBodyBuilder.Append("<td style=\"font-family: Arial; font-size: 11px;\" valign=\"top\">");
            emailBodyBuilder.Append(Request.Form[formKey].ToString().Replace("\r", "").Replace("\n", "<br />"));
            emailBodyBuilder.Append("</td></tr>");
        }

        emailBodyBuilder.Append("</table>");

        return emailBodyBuilder.ToString();
    }

    /// <summary>
    /// Saves a submitted form as an item.
    /// </summary>
    /// <returns></returns>
    private async Task SaveFormInDatabaseAsync()
    {
        var wiserItem = new WiserItemModel
        {
            EntityType = "web-form"
        };

        foreach (var formKey in Request.Form.Keys)
        {
            wiserItem.SetDetail(formKey, Request.Form[formKey].ToString());
        }

        await wiserItemsService.SaveAsync(wiserItem, skipPermissionsCheck: true);
    }

    #region Handling settings

    /// <inheritdoc />
    public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
    {
        Settings = Settings.ComponentMode == ComponentModes.Legacy
            ? Newtonsoft.Json.JsonConvert.DeserializeObject<WebFormLegacySettingsModel>(settingsJson)?.ToSettingsModel()
            : Newtonsoft.Json.JsonConvert.DeserializeObject<WebFormCmsSettingsModel>(settingsJson);

        if (Settings == null)
        {
            return;
        }

        if (forcedComponentMode.HasValue)
        {
            Settings.ComponentMode = (ComponentModes) forcedComponentMode.Value;
        }

        HandleDefaultSettingsFromComponentMode();
    }

    /// <inheritdoc />
    public override string GetSettingsJson()
    {
        return Settings.ComponentMode == ComponentModes.Legacy
            ? Newtonsoft.Json.JsonConvert.SerializeObject(WebFormLegacySettingsModel.FromSettingsModel(Settings))
            : Newtonsoft.Json.JsonConvert.SerializeObject(Settings);
    }

    #endregion
}