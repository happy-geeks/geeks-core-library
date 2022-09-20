using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Account.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Google.Protobuf;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Constants = GeeksCoreLibrary.Components.Account.Models.Constants;

namespace GeeksCoreLibrary.Components.Account
{
    [CmsObject(
        PrettyName = "Account",
        Description = "Component for handling accounts on a website, such as login, logout, change password etc."
    )]
    public class Account : CmsComponent<AccountCmsSettingsModel, Account.ComponentModes>
    {
        private readonly GclSettings gclSettings;
        private readonly IObjectsService objectsService;
        private readonly ICommunicationsService communicationsService;
        private readonly IWiserItemsService wiserItemsService;

        #region Enums

        public enum ComponentModes
        {
            /// <summary>
            /// For all login functionality with a login in a single step (e-mail and password in one form), including password forgotten, Google Authenticator etc.
            /// </summary>
            LoginSingleStep = 1,

            /// <summary>
            /// For all login functionality with a login in multiple steps (IE first e-mail, then check if e-mail is active, then enter password or activate account), including password forgotten, Google Authenticator etc.
            /// </summary>
            LoginMultipleSteps = 2,

            /// <summary>
            /// For all password forgotten functionality.
            /// </summary>
            ResetPassword = 3,

            /// <summary>
            /// For creating a new account when someone is not logged in, or changing their account when they are.
            /// </summary>
            CreateOrUpdateAccount = 4,

            /// <summary>
            /// A mode for creating a place where the user can manage their sub accounts.
            /// </summary>
            SubAccountsManagement = 5,

            /// <summary>
            /// A mode for starting a new punch out cXML session.
            /// </summary>
            CXmlPunchOutLogin = 6,

            /// <summary>
            /// A mode for continuing a punch out cXML session.
            /// </summary>
            CXmlPunchOutContinueSession = 7
        }

        /// <summary>
        /// Enum with possible values for the result of a login attempt.
        /// </summary>
        public enum LoginResults
        {
            /// <summary>
            /// Indicates the login attempt was successful.
            /// </summary>
            Success = 1,

            /// <summary>
            /// Indicates the login attempt was unsuccessful, without telling the use which data was entered correctly.
            /// This is used when ComponentMode = <see cref="ComponentModes.LoginSingleStep"/>.
            /// </summary>
            InvalidUsernameOrPassword = 2,

            /// <summary>
            /// The user does not exist.
            /// This is used when ComponentMode = <see cref="ComponentModes.LoginMultipleSteps"/>.
            /// </summary>
            UserDoesNotExist = 3,

            /// <summary>
            /// The user entered an invalid password.
            /// This is used when ComponentMode = <see cref="ComponentModes.LoginMultipleSteps"/>.
            /// </summary>
            InvalidPassword = 4,

            /// <summary>
            /// The user had too many invalid attempts and needs to wait an hour before trying again.
            /// </summary>
            TooManyAttempts = 5,

            /// <summary>
            /// The user is not activated yet.
            /// </summary>
            UserNotActivated = 6,

            /// <summary>
            /// When the user entered the correct credentials, but still needs to use 2FA.
            /// </summary>
            TwoFactorAuthenticationRequired = 7,

            /// <summary>
            /// When the user entered the wrong 2FA pin.
            /// </summary>
            InvalidTwoFactorAuthentication = 8,

            /// <summary>
            /// Only used for logging in via Wiser. This error will be thrown if the validation token is incorrect.
            /// </summary>
            InvalidValidationToken = 9,

            /// <summary>
            /// Only used for logging in via Wiser. This error will be thrown if the user ID could not be decrypted.
            /// </summary>
            InvalidUserId = 10
        }

        /// <summary>
        /// Enum with possible values for the result of a reset/change password attempt.
        /// </summary>
        public enum ResetOrChangePasswordResults
        {
            /// <summary>
            /// Indicates the attempt was successful.
            /// </summary>
            Success = 1,

            /// <summary>
            /// Indicates that the query string contains either an invalid user ID, or an invalid or expired token.
            /// </summary>
            InvalidTokenOrUser = 2,

            /// <summary>
            /// Indicates that the user has entered 2 different passwords.
            /// </summary>
            PasswordsNotTheSame = 3,

            /// <summary>
            /// Indicates that the user didn't enter their old password correctly.
            /// </summary>
            OldPasswordInvalid = 4,

            /// <summary>
            /// Indicates that the user didn't enter a new password.
            /// </summary>
            EmptyPassword = 5,

            /// <summary>
            /// Indicates that the password does not match the regex from <see cref="PasswordValidationRegex"/>.
            /// </summary>
            PasswordNotSecure = 6
        }

        /// <summary>
        /// Enum with possible values for the result of a create/change account attempt.
        /// </summary>
        public enum CreateOrUpdateAccountResults
        {
            /// <summary>
            /// Indicates the attempt was successful.
            /// </summary>
            Success = 1,

            /// <summary>
            /// An user with this login already exists.
            /// </summary>
            UserAlreadyExists = 2,

            /// <summary>
            /// For some functions, such as changing the login or password, the user needs to enter their current password first.
            /// When that current password is invalid, this error/status will be returned.
            /// </summary>
            InvalidPassword = 3
        }

        /// <summary>
        /// An enumeration with all possible steps for logging in a user.
        /// </summary>
        public enum LoginSteps
        {
            /// <summary>
            /// The first / initial step, where the user has to enter their username or e-mail address (and password, if ComponentMode is set to <see cref="ComponentModes.LoginSingleStep"/>).
            /// </summary>
            Initial = 1,

            /// <summary>
            /// The step where the user has to enter their password. Only used when ComponentMode is set to <see cref="ComponentModes.LoginMultipleSteps"/>).
            /// </summary>
            Password = 2,

            /// <summary>
            /// The step where the user logged in for the first time since enabling 2FA.
            /// </summary>
            SetupTwoFactorAuthentication = 3,

            /// <summary>
            /// The step where the user needs to enter their code for 2FA.
            /// </summary>
            LoginWithTwoFactorAuthentication = 4,

            /// <summary>
            /// When the user successfully logged in.
            /// </summary>
            Done = 99
        }

        #endregion

        #region Constructor

        public Account(IOptions<GclSettings> gclSettings, ILogger<Account> logger, IStringReplacementsService stringReplacementsService, IObjectsService objectsService, ICommunicationsService communicationsService, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IAccountsService accountsService, IAntiforgery antiForgery, IWiserItemsService wiserItemsService)
        {
            this.gclSettings = gclSettings.Value;
            this.objectsService = objectsService;
            this.communicationsService = communicationsService;
            this.wiserItemsService = wiserItemsService;

            Logger = logger;
            StringReplacementsService = stringReplacementsService;
            DatabaseConnection = databaseConnection;
            TemplatesService = templatesService;
            AccountsService = accountsService;

            Settings = new AccountCmsSettingsModel();
        }

        #endregion

        #region Handling settings

        /// <inheritdoc />
        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            Settings = JsonConvert.DeserializeObject<AccountCmsSettingsModel>(settingsJson) ?? new AccountCmsSettingsModel();
            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
        }

        /// <inheritdoc />
        public override string GetSettingsJson()
        {
            return JsonConvert.SerializeObject(Settings);
        }

        #endregion

        #region Rendering

        /// <inheritdoc />
        public override async Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData)
        {
            if (Request == null)
            {
                throw new Exception("Account component requires an http context, but it's null, so can't continue!");
            }

            ComponentId = dynamicContent.Id;
            ExtraDataForReplacements = extraData;
            ParseSettingsJson(dynamicContent.SettingsJson, forcedComponentMode);
            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
            else if (!String.IsNullOrWhiteSpace(dynamicContent.ComponentMode))
            {
                Settings.ComponentMode = Enum.Parse<ComponentModes>(dynamicContent.ComponentMode);
            }

            HandleDefaultSettingsFromComponentMode();

            // Check if we need to call a specific method and then do so. Skip everything else, because we don't want to render the entire component then.
            if (!String.IsNullOrWhiteSpace(callMethod))
            {
                TempData["InvokeMethodResult"] = await InvokeMethodAsync(callMethod);
                return new HtmlString("");
            }

            if (!String.IsNullOrWhiteSpace(Request.Query[Constants.UserIdQueryStringKey]) && !String.IsNullOrWhiteSpace(Request.Query[Constants.ResetPasswordTokenQueryStringKey]))
            {
                // If we have a userId and token in the query string, we need to force the Reset Password mode, otherwise the wrong templates will be used.
                Settings.ComponentMode = ComponentModes.ResetPassword;
            }

            Logger.LogDebug($"Account - Mode set to: {Settings.ComponentMode}");

            var resultHtml = new StringBuilder();
            switch (Settings.ComponentMode)
            {
                case ComponentModes.LoginSingleStep:
                case ComponentModes.LoginMultipleSteps:
                    resultHtml.Append(await HandleLoginModeAsync());
                    break;
                case ComponentModes.ResetPassword:
                    resultHtml.Append(await HandleResetPasswordMode());
                    break;
                case ComponentModes.CreateOrUpdateAccount:
                    resultHtml.Append(await HandleCreateOrUpdateUserModeAsync());
                    break;
                case ComponentModes.SubAccountsManagement:
                    resultHtml.Append(await HandleSubAccountsManagementModeAsync());
                    break;
                case ComponentModes.CXmlPunchOutLogin:
                    await HandleCXmlPunchOutLoginModeAsync();
                    break;
                case ComponentModes.CXmlPunchOutContinueSession:
                    await HandleCXmlPunchOutContinueSessionModeAsync();
                    break;
                default:
                    throw new NotImplementedException($"Unknown or unsupported component mode '{Settings.ComponentMode}' in 'GenerateHtmlAsync'.");
            }

            if (!String.IsNullOrWhiteSpace(Settings.TemplateJavaScript))
            {
                var javascript = Settings.TemplateJavaScript.ReplaceCaseInsensitive("{loginFieldName}", Settings.LoginFieldName)
                    .ReplaceCaseInsensitive("{passwordFieldName}", Settings.PasswordFieldName)
                    .ReplaceCaseInsensitive("{newPasswordFieldName}", Settings.NewPasswordFieldName)
                    .ReplaceCaseInsensitive("{passwordConfirmationFieldName}", Settings.NewPasswordConfirmationFieldName)
                    .ReplaceCaseInsensitive("{entityType}", Settings.EntityType)
                    .ReplaceCaseInsensitive("{contentId}", ComponentId.ToString());

                resultHtml.Append($"<script>{javascript}</script>");
            }

            Logger.LogDebug($"Account - End generating HTML.");

            return new HtmlString(resultHtml.ToString());
        }

        /// <summary>
        /// Renders the HTML for Google 2FA, if it's enabled.
        /// </summary>
        /// <param name="template">The HTML template that is going to be shown to the user.</param>
        /// <param name="stepNumber">The current step number. Note: This is a ref parameter and the function will change the value based on whether the user already has 2FA setup or not.</param>
        /// <returns></returns>
        private string RenderGoogleAuthenticator(string template, ref int stepNumber)
        {
            if (!Settings.EnableGoogleAuthenticator)
            {
                return template;
            }

            var sessionUserId = HttpContext?.Session.GetString($"{Constants.UserIdSessionKey}_{ComponentId}");
            var username = HttpContext?.Session.GetString($"{Constants.LoginValueSessionKey}_{ComponentId}");
            if (String.IsNullOrWhiteSpace(sessionUserId) || sessionUserId == "0" || String.IsNullOrWhiteSpace(username))
            {
                return template;
            }

            return $"<!-- Google Authenticator not implemented yet. --> {template}";
            /*var googleAuthenticator = new GoogleAuthenticator(GoogleAuthenticatorSiteId, sessionUserId, username, "");
            stepNumber = googleAuthenticator.HasAuthEnabled() ? LoginSteps.LoginWithTwoFactorAuthentication : LoginSteps.SetupTwoFactorAuthentication;
            return template.ReplaceCaseInsensitive("{googleAuthenticationQrImageUrl}", googleAuthenticator.GetQrCodeUrl()).ReplaceCaseInsensitive("{googleAuthenticationVerificationId}", googleAuthenticator.GetVerificationId());*/
        }

        #endregion

        #region Handling different component modes

        /// <summary>
        /// Handles everything that needs to be done when this component is set to login mode.
        /// </summary>
        /// <returns>A string with the HTML to put on the page.</returns>
        public async Task<string> HandleLoginModeAsync()
        {
            var resultHtml = Settings.Template;
            DataRow dataRow = null;

            try
            {
                var httpContext = HttpContext;
                var response = httpContext?.Response;
                var request = httpContext?.Request;

                ulong userId = 0;
                Int32.TryParse(HttpContextHelpers.GetRequestValue(httpContext, Constants.StepNumberFieldName), out var stepNumber);

                var ociHookUrl = HttpContextHelpers.GetRequestValue(httpContext, Settings.OciHookUrlKey);
                var ociUsername = HttpContextHelpers.GetRequestValue(httpContext, Settings.OciUsernameKey);
                var ociPassword = HttpContextHelpers.GetRequestValue(httpContext, Settings.OciPasswordKey);
                var encryptedWiserUserId = HttpContextHelpers.GetRequestValue(httpContext, Settings.WiserLoginUserIdKey);

                if (Settings.EnableOciLogin && !String.IsNullOrWhiteSpace(ociHookUrl))
                {
                    HttpContextHelpers.WriteCookie(httpContext, Constants.OciHookUrlCookieName, ociHookUrl);
                }

                if (Settings.EnableOciLogin && !String.IsNullOrWhiteSpace(ociUsername) && !String.IsNullOrWhiteSpace(ociPassword))
                {
                    await AccountsService.LogoutUserAsync(Settings);
                    var loginResult = await LoginUserAsync(stepNumber, ociUsername, ociPassword, (int)ComponentModes.LoginSingleStep);
                    userId = loginResult.UserId;

                    switch (loginResult.Result)
                    {
                        case LoginResults.Success:
                        {
                            stepNumber += 1;

                            if (!String.IsNullOrWhiteSpace(Settings.RedirectAfterAction))
                            {
                                response?.Redirect(await StringReplacementsService.DoAllReplacementsAsync(Settings.RedirectAfterAction, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables), true);
                            }

                            break;
                        }

                        default:
                        {
                            // There was an error, show that error to the user.
                            resultHtml = RenderGoogleAuthenticator(Settings.Template, ref stepNumber).ReplaceCaseInsensitive("{error}", Settings.TemplateError.ReplaceCaseInsensitive("{errorType}", loginResult.Result.ToString()));
                            break;
                        }
                    }
                }
                else if (Settings.EnableWiserLogin && !String.IsNullOrWhiteSpace(encryptedWiserUserId))
                {
                    await AccountsService.LogoutUserAsync(Settings);
                    var loginResult = await LoginUserAsync(stepNumber, overrideComponentMode: (int)ComponentModes.LoginSingleStep, encryptedUserId: encryptedWiserUserId);
                    userId = loginResult.UserId;

                    switch (loginResult.Result)
                    {
                        case LoginResults.Success:
                        {
                            stepNumber += 1;

                            // If there have been any extra query string parameters sent via Wiser, add them to the session so that they can be used in other places.
                            var queryStringValuesToSkip = new List<string> { "templateid", "templatename", Settings.WiserLoginTokenKey, Settings.WiserLoginUserIdKey };
                            foreach (var queryStringKey in Request.Query.Keys)
                            {
                                if (queryStringValuesToSkip.Any(q => q.Equals(queryStringKey, StringComparison.OrdinalIgnoreCase)))
                                {
                                    continue;
                                }

                                HttpContext.Session.SetString($"WiserLogin_{queryStringKey}", Request.Query[queryStringKey]);
                            }

                            if (!String.IsNullOrWhiteSpace(Settings.RedirectAfterAction))
                            {
                                response?.Redirect(await StringReplacementsService.DoAllReplacementsAsync(Settings.RedirectAfterAction, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables), true);
                            }

                            break;
                        }

                        default:
                        {
                            // There was an error, show that error to the user.
                            resultHtml = Settings.Template.ReplaceCaseInsensitive("{error}", Settings.TemplateError.ReplaceCaseInsensitive("{errorType}", loginResult.Result.ToString()));
                            break;
                        }
                    }
                }
                else if (request == null || !request.HasFormContentType || request.Form.Count == 0 || request.Form[Constants.ComponentIdFormKey].ToString() != ComponentId.ToString())
                {
                    if (stepNumber <= 0)
                    {
                        stepNumber = 1;
                    }

                    resultHtml = Settings.Template.ReplaceCaseInsensitive("{error}", "");

                    if (String.Equals(request?.Query[$"{Constants.LogoutQueryStringKey}{ComponentId}"], "true", StringComparison.OrdinalIgnoreCase))
                    {
                        // User is logging out.
                        await AccountsService.LogoutUserAsync(Settings);
                    }
                    else
                    {
                        // Check if the user is already logged in and show the success template if they are.
                        userId = (await AccountsService.GetUserDataFromCookieAsync()).UserId;
                        if (userId > 0)
                        {
                            resultHtml = Settings.TemplateSuccess;
                        }
                    }
                }
                else
                {
                    // If there are form post variables and the correct content ID has been posted with them, it means the user is trying to login.
                    var externalLogin = request.Form[Constants.ExternalLoginButtonOrFieldName].ToString();
                    if (!String.IsNullOrWhiteSpace(externalLogin))
                    {
                        switch (externalLogin.ToLowerInvariant())
                        {
                            case "google":
                                RedirectToGoogleLogin();
                                break;
                            default:
                                throw new NotImplementedException($"External login type '{externalLogin}' is not (yet) supported.");
                        }

                        return null;
                    }

                    // The user is attempting to login.
                    if (stepNumber <= 0)
                    {
                        stepNumber = 1;
                    }
                    var loginResult = await LoginUserAsync(stepNumber);
                    userId = loginResult.UserId;
                    switch (loginResult.Result)
                    {
                        case LoginResults.Success:
                        {
                            stepNumber += 1;
                            var done = false;

                            switch (Settings.ComponentMode)
                            {
                                case ComponentModes.LoginSingleStep:
                                {
                                    resultHtml = Settings.TemplateSuccess;
                                    done = true;
                                    break;
                                }

                                case ComponentModes.LoginMultipleSteps:
                                {
                                    resultHtml = stepNumber > 2 ? Settings.TemplateSuccess : Settings.Template.ReplaceCaseInsensitive("{error}", "");
                                    done = stepNumber > 2;
                                    break;
                                }

                                default:
                                {
                                    throw new NotImplementedException($"Component mode '{Settings.ComponentMode}' has not been implemented in 'HandleLoginMode'.");
                                }
                            }

                            if (done && !String.IsNullOrWhiteSpace(Settings.RedirectAfterAction))
                            {
                                response.Redirect(await StringReplacementsService.DoAllReplacementsAsync(Settings.RedirectAfterAction, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables), true);
                            }

                            break;
                        }

                        case LoginResults.TwoFactorAuthenticationRequired:
                        {
                            resultHtml = RenderGoogleAuthenticator(Settings.Template, ref stepNumber).ReplaceCaseInsensitive("{error}", "");
                            break;
                        }

                        case LoginResults.UserNotActivated:
                        {
                            if (!String.IsNullOrWhiteSpace(loginResult.EmailAddress))
                            {
                                await SendResetPasswordEmail(loginResult.EmailAddress);
                                resultHtml = Settings.TemplateSuccess.ReplaceCaseInsensitive("{sentActivationMail}", "true");
                            }
                            else
                            {
                                resultHtml = Settings.Template.ReplaceCaseInsensitive("{error}", Settings.TemplateError.ReplaceCaseInsensitive("{errorType}", loginResult.Result.ToString()));
                            }

                            break;
                        }

                        default:
                        {
                            // There was an error, show that error to the user.
                            resultHtml = RenderGoogleAuthenticator(Settings.Template, ref stepNumber).ReplaceCaseInsensitive("{error}", Settings.TemplateError.ReplaceCaseInsensitive("{errorType}", loginResult.Result.ToString()));
                            break;
                        }
                    }
                }

                resultHtml = resultHtml.ReplaceCaseInsensitive("{stepNumber}", stepNumber.ToString()).ReplaceCaseInsensitive("{step}", ((LoginSteps)stepNumber).ToString());

                // If we have a user ID and a main query, replace the results from the query into the results HTML.
                if (userId > 0 && !String.IsNullOrWhiteSpace(Settings.MainQuery))
                {
                    var query = SetupAccountQuery(Settings.MainQuery, userId);
                    var queryResult = await RenderAndExecuteQueryAsync(query, skipCache: true);

                    if (queryResult.Rows.Count > 0)
                    {
                        dataRow = queryResult.Rows[0];
                    }
                }
            }
            catch (Exception exception)
            {
                if (resultHtml == null || !resultHtml.Contains("{error}"))
                {
                    resultHtml = Settings.TemplateError;
                }
                else
                {
                    resultHtml = resultHtml.ReplaceCaseInsensitive("{error}", Settings.TemplateError);
                }

                resultHtml = resultHtml.ReplaceCaseInsensitive("{errorType}", "Server");
                Logger.LogError(exception.ToString());
            }

            return AddComponentIdToForms(await TemplatesService.DoReplacesAsync(DoDefaultAccountHtmlReplacements(resultHtml), dataRow: dataRow, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables), Constants.ComponentIdFormKey);
        }

        /// <summary>
        /// Handles everything that needs to be done when this component is set to reset password mode.
        /// </summary>
        /// <returns>A string with the HTML to put on the page.</returns>
        public async Task<string> HandleResetPasswordMode()
        {
            var resultHtml = "";
            var resetPasswordResult = ResetOrChangePasswordResults.Success;

            try
            {
                var httpContext = HttpContext;
                var response = httpContext?.Response;
                var request = httpContext?.Request;
                ulong userIdFromQueryString = 0;
                var encryptedUerId = request?.Query[Constants.UserIdQueryStringKey].ToString();

                if (!String.IsNullOrWhiteSpace(encryptedUerId))
                {
                    try
                    {
                        userIdFromQueryString = Convert.ToUInt64(encryptedUerId.DecryptWithAes(gclSettings.AccountUserIdEncryptionKey));
                    }
                    catch (Exception exception)
                    {
                        WriteToTrace($"Error while decrypting or converting user ID: {exception}", true);
                        resetPasswordResult = ResetOrChangePasswordResults.InvalidTokenOrUser;
                    }
                }

                // If there is a user ID in the query string, it means the user clicked the link in the e-mail to reset their password.
                var userLogin = "";
                if (userIdFromQueryString > 0)
                {
                    var query = SetupAccountQuery(Settings.ValidateResetPasswordTokenQuery, userIdFromQueryString, token: request?.Query[Constants.ResetPasswordTokenQueryStringKey]);
                    var dataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);

                    if (dataTable == null || dataTable.Rows.Count == 0)
                    {
                        resetPasswordResult = ResetOrChangePasswordResults.InvalidTokenOrUser;
                    }
                    else if (!dataTable.Columns.Contains(Constants.LoginColumn))
                    {
                        WriteToTrace($"The ValidateResetPasswordTokenQuery did not return a column named '{Constants.LoginColumn}' and therefor returned the status 'InvalidTokenOrUser'.", true);
                        resetPasswordResult = ResetOrChangePasswordResults.InvalidTokenOrUser;
                    }
                    else
                    {
                        userLogin = dataTable.Rows[0].Field<string>(Constants.LoginColumn);
                        if (String.IsNullOrWhiteSpace(userLogin))
                        {
                            WriteToTrace($"The ValidateResetPasswordTokenQuery returned an empty string in the column '{Constants.LoginColumn}' and therefor returned the status 'InvalidTokenOrUser'.", true);
                            resetPasswordResult = ResetOrChangePasswordResults.InvalidTokenOrUser;
                        }
                    }
                }

                // If there are form post variables and the correct content ID has been posted with them, it means the user is trying reset their password.
                if (request == null || !request.HasFormContentType || request.Form.Count == 0 || request.Form[Constants.ComponentIdFormKey].ToString() != ComponentId.ToString())
                {
                    resultHtml = Settings.Template;
                }
                else
                {
                    var isLoggedIn = false;
                    if (userIdFromQueryString <= 0)
                    {
                        await SendResetPasswordEmail();
                    }
                    else
                    {
                        resetPasswordResult = await ChangePasswordAsync(userIdFromQueryString);
                        if (resetPasswordResult == ResetOrChangePasswordResults.Success)
                        {
                            if (Settings.ComponentMode == ComponentModes.ResetPassword && Settings.AutoLoginUserAfterAction && !String.IsNullOrWhiteSpace(userLogin))
                            {
                                var loginResult = await LoginUserAsync(1, userLogin, request.Form[Settings.NewPasswordFieldName], (int)ComponentModes.LoginSingleStep);
                                if (loginResult.Result == LoginResults.Success)
                                {
                                    isLoggedIn = true;
                                }
                            }

                            if (!String.IsNullOrWhiteSpace(Settings.RedirectAfterAction))
                            {
                                response.Redirect(Settings.RedirectAfterAction);
                            }
                        }
                    }

                    resultHtml = (resetPasswordResult == ResetOrChangePasswordResults.Success ? Settings.TemplateSuccess : Settings.Template).ReplaceCaseInsensitive("{isLoggedIn}", isLoggedIn.ToString().ToLowerInvariant());
                }

                resultHtml = resetPasswordResult == ResetOrChangePasswordResults.Success
                    ? resultHtml.ReplaceCaseInsensitive("{error}", "")
                    : resultHtml.ReplaceCaseInsensitive("{error}", Settings.TemplateError).ReplaceCaseInsensitive("{errorType}", resetPasswordResult.ToString());
            }
            catch (Exception exception)
            {
                if (resultHtml == null || !resultHtml.Contains("{error}", StringComparison.OrdinalIgnoreCase))
                {
                    resultHtml = Settings.TemplateError;
                }
                else
                {
                    resultHtml = resultHtml.ReplaceCaseInsensitive("{error}", Settings.TemplateError);
                }

                resultHtml = resultHtml.ReplaceCaseInsensitive("{errorType}", "Server");
                WriteToTrace(exception.ToString(), true);
            }
            return AddComponentIdToForms(await TemplatesService.DoReplacesAsync(DoDefaultAccountHtmlReplacements(resultHtml), handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables), Constants.ComponentIdFormKey);
        }

        /// <summary>
        /// Handle everything for the functionality of creating a new account, or changing a logged in user's account.
        /// </summary>
        /// <returns>The HTML for this mode.</returns>
        public async Task<string> HandleCreateOrUpdateUserModeAsync()
        {
            var resultHtml = Settings.Template;
            DataRow firstDataRowOfMainQuery = null;
            var httpContext = HttpContext;
            if (httpContext == null)
            {
                throw new Exception("No http context available.");
            }

            var request = httpContext.Request;
            var response = httpContext.Response;

            try
            {
                // Replace request variables first, so that they take preference over values from the database.
                if (Settings.HandleRequest)
                {
                    resultHtml = StringReplacementsService.DoHttpRequestReplacements(resultHtml);
                    resultHtml = StringReplacementsService.DoSessionReplacements(resultHtml);
                }

                var userData = await AccountsService.GetUserDataFromCookieAsync();
                var query = SetupAccountQuery(Settings.MainQuery, userData.UserId > 0 ? userData.UserId : AccountsService.GetRecentlyCreateAccountId());
                var accountDataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);
                var availableFields = new List<string>();

                if (accountDataTable.Rows.Count > 0)
                {
                    firstDataRowOfMainQuery = accountDataTable.Rows[0];

                    if (accountDataTable.Columns.Contains(Constants.PropertyNameColumn))
                    {
                        // If the results contain the column 'property_name', we have one row per field.
                        availableFields.AddRange(accountDataTable.Rows.Cast<DataRow>().Select(dataRow => dataRow.Field<string>(Constants.PropertyNameColumn)));
                    }
                    else
                    {
                        // If the results don't contain the column 'property_name', we have one column per field.
                        var fieldsToIgnore = new List<string> { "id", "error", "success", "entity_type" };
                        availableFields.AddRange(accountDataTable.Columns.Cast<DataColumn>()
                            .Where(dataColumn => !fieldsToIgnore.Any(f => f.Equals(dataColumn.ColumnName, StringComparison.OrdinalIgnoreCase)))
                            .Select(dataColumn => dataColumn.ColumnName));
                    }
                }

                // If there are form post variables and the correct content ID has been posted with them, it means the user is trying to login.
                if (!request.HasFormContentType || request.Form.Count == 0 || request.Form[Constants.ComponentIdFormKey].ToString() != ComponentId.ToString())
                {
                    resultHtml = resultHtml.ReplaceCaseInsensitive("{error}", "").ReplaceCaseInsensitive("{success}", "");
                }
                else
                {
                    var changePasswordResult = ResetOrChangePasswordResults.Success;
                    var passwordEmpty = String.IsNullOrWhiteSpace(request.Form[Settings.NewPasswordFieldName]) && String.IsNullOrWhiteSpace(request.Form[Settings.NewPasswordConfirmationFieldName]);
                    var userIsChangingPassword = userData.UserId > 0 && !passwordEmpty;
                    var userIsChangingLoginName = userData.UserId > 0 && request.Form.ContainsKey(Settings.LoginFieldName) && !String.IsNullOrWhiteSpace(request.Form[Settings.LoginFieldName]);
                    var creatingUserThatCannotLogin = userData.UserId == 0 && passwordEmpty && Settings.AllowEmptyPassword;

                    if ((userIsChangingPassword || (userIsChangingLoginName && Settings.RequireCurrentPasswordForChangingLogin)) && (!passwordEmpty || !Settings.AllowEmptyPassword))
                    {
                        // If the user is changing their login name or password, we want to validate their current password first.
                        // If the user ID is 0, the ChangePassword method will only validate and not save anything, othewrwise the password will be changed right away.
                        changePasswordResult = await ChangePasswordAsync(userData.UserId);
                    }

                    // Only update the user's account, if the password has been validated, or if the user is not changing their login name and password.
                    var createOrUpdateAccountResult = (Result: CreateOrUpdateAccountResults.InvalidPassword, ErrorTemplate: Settings.TemplateError, SuccessTemplate: "", UserId: userData.UserId, SubAccountId: 0UL, Role: "");

                    if (changePasswordResult == ResetOrChangePasswordResults.Success)
                    {
                        await DatabaseConnection.BeginTransactionAsync();
                        createOrUpdateAccountResult = await CreateOrUpdateAccountAsync(userData.UserId > 0 ? userData.UserId : AccountsService.GetRecentlyCreateAccountId(), availableFields, useTransaction: false);

                        if (createOrUpdateAccountResult.UserId > 0 && userData.UserId == 0)
                        {
                            // If we just created an account, save the password now, otherwise the password was already changed.
                            changePasswordResult = await ChangePasswordAsync(createOrUpdateAccountResult.UserId, true);
                            if (changePasswordResult != ResetOrChangePasswordResults.Success)
                            {
                                createOrUpdateAccountResult.ErrorTemplate = Settings.TemplateError;
                                createOrUpdateAccountResult.SuccessTemplate = "";
                                await DatabaseConnection.RollbackTransactionAsync();
                            }
                        }

                        await DatabaseConnection.CommitTransactionAsync(false);
                    }

                    if (createOrUpdateAccountResult.Result == CreateOrUpdateAccountResults.Success
                        && changePasswordResult == ResetOrChangePasswordResults.Success
                        && !resultHtml.Contains("{success}", StringComparison.OrdinalIgnoreCase)
                        && !String.IsNullOrWhiteSpace(createOrUpdateAccountResult.SuccessTemplate))
                    {
                        // If the template does not contain the replacement '{success}', we want to only return the success template.
                        resultHtml = createOrUpdateAccountResult.SuccessTemplate;
                    }
                    else if ((createOrUpdateAccountResult.Result != CreateOrUpdateAccountResults.Success || changePasswordResult != ResetOrChangePasswordResults.Success)
                             && !resultHtml.Contains("{error}", StringComparison.OrdinalIgnoreCase)
                             && !String.IsNullOrWhiteSpace(createOrUpdateAccountResult.ErrorTemplate))
                    {
                        // If the template does not contain the replacement '{error}', we want to only return the error template.
                        resultHtml = changePasswordResult != ResetOrChangePasswordResults.Success ? Settings.TemplateError : createOrUpdateAccountResult.ErrorTemplate;
                    }
                    else
                    {
                        // In other cases, return the entire template with the error or success message somewhere inside it.
                        if (userIsChangingPassword)
                        {
                            resultHtml = changePasswordResult != ResetOrChangePasswordResults.Success
                                ? resultHtml.ReplaceCaseInsensitive("{error}", Settings.TemplateError).ReplaceCaseInsensitive("{success}", "")
                                : resultHtml.ReplaceCaseInsensitive("{error}", "").ReplaceCaseInsensitive("{success}", Settings.TemplateSuccess);
                        }
                        else
                        {
                            resultHtml = resultHtml.ReplaceCaseInsensitive("{error}", createOrUpdateAccountResult.ErrorTemplate).ReplaceCaseInsensitive("{success}", createOrUpdateAccountResult.SuccessTemplate);
                        }
                    }

                    resultHtml = resultHtml.ReplaceCaseInsensitive("{errorType}", changePasswordResult != ResetOrChangePasswordResults.Success ? changePasswordResult.ToString() : "");

                    // Check if we can automatically login the user after creating a new account.
                    var isLoggedIn = false;
                    if (creatingUserThatCannotLogin && createOrUpdateAccountResult.Result == CreateOrUpdateAccountResults.Success)
                    {
                        HttpContextHelpers.WriteCookie(HttpContext, Constants.CreatedAccountCookieName, createOrUpdateAccountResult.UserId.ToString().EncryptWithAes(gclSettings.AccountUserIdEncryptionKey), isEssential: true);
                    }
                    else if (userData.UserId == 0 && createOrUpdateAccountResult.Result == CreateOrUpdateAccountResults.Success && changePasswordResult == ResetOrChangePasswordResults.Success && Settings.AutoLoginUserAfterAction)
                    {
                        await AutoLoginUserAsync(createOrUpdateAccountResult.SubAccountId > 0 ? createOrUpdateAccountResult.SubAccountId : createOrUpdateAccountResult.UserId, createOrUpdateAccountResult.UserId, createOrUpdateAccountResult.Role);
                        isLoggedIn = true;
                    }

                    if (createOrUpdateAccountResult.Result == CreateOrUpdateAccountResults.Success && changePasswordResult == ResetOrChangePasswordResults.Success && !String.IsNullOrWhiteSpace(Settings.RedirectAfterAction))
                    {
                        response.Redirect(await StringReplacementsService.DoAllReplacementsAsync(Settings.RedirectAfterAction, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables), true);
                    }

                    resultHtml = resultHtml.ReplaceCaseInsensitive("{isLoggedIn}", isLoggedIn.ToString().ToLowerInvariant());
                }

                // Loop through lines and replace variables.
                foreach (Match match in Regex.Matches(resultHtml, "{repeat:fields}(.*?){/repeat:fields}", RegexOptions.Singleline))
                {
                    if (accountDataTable.Rows.Count == 0)
                    {
                        resultHtml = resultHtml.Replace(match.Value, "");
                        continue;
                    }

                    var subTemplate = match.Groups[1].Value;
                    var fieldsHtmlBuilder = new StringBuilder();

                    foreach (DataRow dataRow in accountDataTable.Rows)
                    {
                        // Replace details
                        var lineTemplate = await TemplatesService.DoReplacesAsync(subTemplate, dataRow: dataRow, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);

                        fieldsHtmlBuilder.Append(lineTemplate);
                    }

                    resultHtml = resultHtml.Replace(match.Value, fieldsHtmlBuilder.ToString());
                }
            }
            catch (Exception exception)
            {
                resultHtml = resultHtml == null || !resultHtml.Contains("{error}", StringComparison.OrdinalIgnoreCase) ? Settings.TemplateError : resultHtml.ReplaceCaseInsensitive("{error}", Settings.TemplateError);
                resultHtml = resultHtml.ReplaceCaseInsensitive("{errorType}", "Server");
                WriteToTrace(exception.ToString(), true);
            }

            return AddComponentIdToForms(await TemplatesService.DoReplacesAsync(DoDefaultAccountHtmlReplacements(resultHtml), dataRow: firstDataRowOfMainQuery, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables), Constants.ComponentIdFormKey);
        }

        /// <summary>
        /// Handle everything for the functionality of managing sub accounts of a user.
        /// </summary>
        /// <returns>The HTML for this mode.</returns>
        public async Task<string> HandleSubAccountsManagementModeAsync()
        {
            var resultHtml = Settings.Template;
            DataRow firstDataRowOfMainQuery = null;
            var httpContext = HttpContext;
            if (httpContext == null)
            {
                throw new Exception("No http context available.");
            }

            var request = httpContext.Request;
            ulong selectedSubAccount = 0;

            try
            {
                // Replace request variables first, so that they take preference over values from the database.
                if (Settings.HandleRequest)
                {
                    resultHtml = StringReplacementsService.DoHttpRequestReplacements(resultHtml);
                    resultHtml = StringReplacementsService.DoSessionReplacements(resultHtml);
                }

                var userData = await AccountsService.GetUserDataFromCookieAsync();
                if (userData.UserId <= 0)
                {
                    // TODO Show a nice error message to the user, instead of this exception.
                    throw new Exception("User is not logged in!");
                }

                UInt64.TryParse(request.Query[$"{Constants.SelectedSubAccountQueryStringKey}{ComponentId}"], out selectedSubAccount);

                // Add fields to the page.
                var query = SetupAccountQuery(Settings.GetSubAccountQuery, userData.MainUserId, subAccountId: selectedSubAccount);
                var dataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);
                var availableFields = new List<string> { Settings.PasswordFieldName, Settings.NewPasswordFieldName, Settings.NewPasswordConfirmationFieldName, Settings.LoginFieldName, Settings.EmailAddressFieldName, Settings.RoleFieldName };

                if (dataTable.Rows.Count > 0)
                {
                    firstDataRowOfMainQuery = dataTable.Rows[0];

                    if (dataTable.Columns.Contains(Constants.PropertyNameColumn))
                    {
                        // If the results contain the column 'property_name', we have one row per field.
                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            var name = dataRow.Field<string>(Constants.PropertyNameColumn);
                            if (availableFields.Contains(name))
                            {
                                continue;
                            }

                            availableFields.Add(name);
                        }
                    }
                    else
                    {
                        // If the results don't contain the column 'property_name', we have one column per field.
                        var fieldsToIgnore = new List<string> { "id", "error", "success", "entity_type" };
                        foreach (DataColumn dataColumn in dataTable.Columns)
                        {
                            if (availableFields.Contains(dataColumn.ColumnName) || fieldsToIgnore.Any(f => f.Equals(dataColumn.ColumnName, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            availableFields.Add(dataColumn.ColumnName);
                        }
                    }
                }

                var fieldsDataRows = dataTable.Rows;

                if (UInt64.TryParse(request.Query[$"{Constants.DeleteSubAccountQueryStringKey}{ComponentId}"].ToString(), out var deleteSubAccountId) && deleteSubAccountId > 0)
                {
                    query = SetupAccountQuery(Settings.DeleteAccountQuery, userData.MainUserId, subAccountId: deleteSubAccountId);
                    await RenderAndExecuteQueryAsync(query, skipCache: true);
                    resultHtml = !resultHtml.Contains("{success}", StringComparison.OrdinalIgnoreCase) ? Settings.TemplateSuccess : resultHtml.ReplaceCaseInsensitive("{error}", "").ReplaceCaseInsensitive("{success}", Settings.TemplateSuccess);
                }
                else if (!request.HasFormContentType || request.Form.Count == 0 || request.Form[Constants.ComponentIdFormKey].ToString() != ComponentId.ToString())
                {
                    resultHtml = resultHtml.ReplaceCaseInsensitive("{error}", "").ReplaceCaseInsensitive("{success}", "");
                }
                else
                {
                    var changePasswordResult = ResetOrChangePasswordResults.Success;
                    var passwordEmpty = String.IsNullOrWhiteSpace(request.Form[Settings.NewPasswordFieldName]) && String.IsNullOrWhiteSpace(request.Form[Settings.NewPasswordConfirmationFieldName]);
                    var userIsChangingPassword = !passwordEmpty;

                    // Only update the user's account, if the password has been validated, or if the user is not changing their login name and password.
                    var createOrUpdateAccountResult = await CreateOrUpdateAccountAsync(userData.UserId, availableFields, selectedSubAccount);
                    selectedSubAccount = createOrUpdateAccountResult.SubAccountId;
                    if (createOrUpdateAccountResult.Result == CreateOrUpdateAccountResults.Success && userIsChangingPassword)
                    {
                        // If we just created an account, save the password now, otherwise the password was already changed.
                        changePasswordResult = await ChangePasswordAsync(createOrUpdateAccountResult.SubAccountId, true);
                    }

                    if (createOrUpdateAccountResult.Result == CreateOrUpdateAccountResults.Success && changePasswordResult == ResetOrChangePasswordResults.Success && !resultHtml.Contains("{success}", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(createOrUpdateAccountResult.SuccessTemplate))
                    {
                        // If the template does not contain the replacement '{success}', we want to only return the success template.
                        resultHtml = createOrUpdateAccountResult.SuccessTemplate;
                    }
                    else if ((createOrUpdateAccountResult.Result != CreateOrUpdateAccountResults.Success || changePasswordResult != ResetOrChangePasswordResults.Success) && !resultHtml.Contains("{error}", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(createOrUpdateAccountResult.ErrorTemplate))
                    {
                        // If the template does not contain the replacement '{error}', we want to only return the error template.
                        resultHtml = changePasswordResult != ResetOrChangePasswordResults.Success ? Settings.TemplateError : createOrUpdateAccountResult.ErrorTemplate;
                    }
                    else if (createOrUpdateAccountResult.Result != CreateOrUpdateAccountResults.Success)
                    {
                        resultHtml = resultHtml.ReplaceCaseInsensitive("{error}", createOrUpdateAccountResult.ErrorTemplate).ReplaceCaseInsensitive("{success}", createOrUpdateAccountResult.SuccessTemplate);
                    }
                    else if (userIsChangingPassword && changePasswordResult != ResetOrChangePasswordResults.Success)
                    {
                        resultHtml = resultHtml.ReplaceCaseInsensitive("{error}", Settings.TemplateError).ReplaceCaseInsensitive("{success}", "");
                    }
                    else
                    {
                        resultHtml = resultHtml.ReplaceCaseInsensitive("{error}", createOrUpdateAccountResult.ErrorTemplate).ReplaceCaseInsensitive("{success}", createOrUpdateAccountResult.SuccessTemplate);
                    }

                    resultHtml = resultHtml.ReplaceCaseInsensitive("{errorType}", changePasswordResult != ResetOrChangePasswordResults.Success ? changePasswordResult.ToString() : "");

                    // Execute the GetSubAccountQuery again, so that have can show the new values in the HTML.
                    query = SetupAccountQuery(Settings.GetSubAccountQuery, userData.MainUserId, subAccountId: selectedSubAccount);
                    dataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);
                    fieldsDataRows = dataTable.Rows;
                }

                // Get available fields and values from main query. We do this last, so that this will also retrieve any newly created sub account.
                foreach (Match match in Regex.Matches(resultHtml, "{repeat:fields}(.*?){/repeat:fields}", RegexOptions.Singleline))
                {
                    if (fieldsDataRows.Count == 0)
                    {
                        resultHtml = resultHtml.Replace(match.Value, "");
                        continue;
                    }

                    var subTemplate = match.Groups[1].Value;
                    var fieldsHtmlBuilder = new StringBuilder();

                    foreach (DataRow dataRow in fieldsDataRows)
                    {
                        // Replace details
                        var lineTemplate = await TemplatesService.DoReplacesAsync(subTemplate, dataRow: dataRow, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);

                        fieldsHtmlBuilder.Append(lineTemplate);
                    }

                    resultHtml = resultHtml.Replace(match.Value, fieldsHtmlBuilder.ToString());
                }

                // List of sub accounts.
                query = SetupAccountQuery(Settings.MainQuery, userData.MainUserId, subAccountId: selectedSubAccount);
                dataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);
                resultHtml = resultHtml.ReplaceCaseInsensitive("{amountOfSubAccounts}", dataTable.Rows.Count.ToString());

                foreach (Match match in Regex.Matches(resultHtml, "{repeat:subAccounts}(.*?){/repeat:subAccounts}", RegexOptions.Singleline))
                {
                    if (dataTable.Rows.Count == 0)
                    {
                        resultHtml = resultHtml.Replace(match.Value, "");
                        continue;
                    }

                    var subTemplate = match.Groups[1].Value;
                    var fieldsHtmlBuilder = new StringBuilder();

                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        // Replace details
                        var lineTemplate = await TemplatesService.DoReplacesAsync(subTemplate.ReplaceCaseInsensitive("{selectedSubAccount}", selectedSubAccount.ToString()).ReplaceCaseInsensitive("{selectedSubAccount_htmlencode}", selectedSubAccount.ToString()), dataRow: dataRow, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);

                        fieldsHtmlBuilder.Append(lineTemplate);
                    }

                    resultHtml = resultHtml.Replace(match.Value, fieldsHtmlBuilder.ToString());
                }
            }
            catch (Exception exception)
            {
                resultHtml = resultHtml == null || !resultHtml.Contains("{error}", StringComparison.OrdinalIgnoreCase) ? Settings.TemplateError : resultHtml.ReplaceCaseInsensitive("{error}", Settings.TemplateError);

                resultHtml = resultHtml.ReplaceCaseInsensitive("{errorType}", "Server");
                WriteToTrace(exception.ToString(), true);
            }

            return AddComponentIdToForms(await TemplatesService.DoReplacesAsync(DoDefaultAccountHtmlReplacements(resultHtml), dataRow: firstDataRowOfMainQuery, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables), Constants.ComponentIdFormKey);
        }

        /// <summary>
        /// Handle everything for logging in for cXML punch out (OCI).
        /// </summary>
        /// <returns></returns>
        private async Task HandleCXmlPunchOutLoginModeAsync()
        {
            throw new NotImplementedException();
            /*var httpContext = HttpContext;
            if (httpContext == null)
            {
                throw new Exception("No http context available.");
            }

            var request = httpContext.Request;
            string requestBody;
            using (var streamReader = new StreamReader(request.Body))
            {
                streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                requestBody = await streamReader.ReadToEndAsync();
            }

            var xmlDocument = XDocument.Parse(requestBody);
            var punchOutRequest = xmlDocument.XPathSelectElement("/cXML/Request");
            var innerRequest = punchOutRequest?.Descendants()?.FirstOrDefault();
            if (!String.Equals("PunchOutSetupRequest", innerRequest?.Name.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var punchOutSessionPrefix = await objectsService.FindSystemObjectByDomainNameAsync("CXmlPunchOutSessionPrefix");
            var hookUrl = HttpContextHelpers.GetRequestValue(httpContext, Settings.OciHookUrlKey);
            var username = HttpContextHelpers.GetRequestValue(httpContext, Settings.OciUsernameKey);
            var password = HttpContextHelpers.GetRequestValue(httpContext, Settings.OciPasswordKey);
            if (String.IsNullOrWhiteSpace(hookUrl))
            {
                //hookUrl = 
            }*/
        }

        /// <summary>
        /// Handle everything for continuing a cXML punch out (OCI) session.
        /// </summary>
        private Task HandleCXmlPunchOutContinueSessionModeAsync()
        {
            throw new NotImplementedException();
            /*var httpContext = HttpContext;
            if (httpContext == null)
            {
                throw new Exception("No http context available.");
            }

            var request = httpContext.Request;*/

        }

        /// <summary>
        /// Automatically logs in the user via ID. This function should never be made available publicly, only for internal usage to login after creating an account for example.
        /// </summary>
        /// <param name="userId">The ID of the user to login.</param>
        /// <param name="mainUserId">The ID of the main user, if the user is logging in with a sub account.</param>
        /// <param name="role">The role of user.</param>
        private async Task AutoLoginUserAsync(ulong userId, ulong mainUserId, string role)
        {
            // Make sure we have a valid user ID.
            if (userId <= 0)
            {
                WriteToTrace("AutoLoginUser called with invalid user ID.", true);
                return;
            }

            // Everything succeeded, so generate a cookie for the user and reset any failed login attempts.
            var amountOfDaysToRememberCookie = GetAmountOfDaysToRememberCookie();
            var cookieValue = await AccountsService.GenerateNewCookieTokenAsync(userId, mainUserId, !amountOfDaysToRememberCookie.HasValue || amountOfDaysToRememberCookie.Value <= 0 ? 0 : amountOfDaysToRememberCookie.Value, Settings.EntityType, Settings.SubAccountEntityType, role);
            await SaveGoogleClientIdAsync(userId);
            
            var offset = (amountOfDaysToRememberCookie ?? 0) <= 0 ? (DateTimeOffset?)null : DateTimeOffset.Now.AddDays(amountOfDaysToRememberCookie.Value);
            HttpContextHelpers.WriteCookie(HttpContext, Constants.CookieName, cookieValue, offset, isEssential: true);

            await SaveLoginAttemptAsync(true, userId);
        }

        /// <summary>
        /// Creates a new account if the user is not logged in, or updates an existing account if they are logged in.
        /// </summary>
        /// <param name="userId">The ID of the logged in user, or 0 if they're not logged in.</param>
        /// <param name="availableFields">The fields that the user is allowed to post and save in their account.</param>
        /// <param name="subAccountId">Optional: The ID of the sub account, if you want to update a sub account. Default is 0.</param>
        /// <param name="useTransaction">Optional: Set to <see langword="false" /> to not use transactions. Default value is <see langword="true" />.</param>
        /// <returns></returns>
        private async Task<(CreateOrUpdateAccountResults Result, string ErrorTemplate, string SuccessTemplate, ulong UserId, ulong SubAccountId, string Role)> CreateOrUpdateAccountAsync(ulong userId, List<string> availableFields, ulong subAccountId = 0, bool useTransaction = true)
        {
            var httpContext = HttpContext;
            if (httpContext == null)
            {
                throw new Exception("No http context available.");
            }

            var request = httpContext.Request;

            try
            {

                if (useTransaction)
                {
                    await DatabaseConnection.BeginTransactionAsync();
                }

                var result = CreateOrUpdateAccountResults.Success;
                var createdNewAccount = false;

                // If we have no available fields from the main query, just add everything. The developer has been warned in the documentation about this.
                if (!availableFields.Any())
                {
                    foreach (var formKey in request.Form.Keys.Where(k => !k.StartsWith("_")))
                    {
                        availableFields.Add(formKey);
                    }
                }

                var passwordFields = new List<string> { Settings.PasswordFieldName, Settings.NewPasswordFieldName, Settings.NewPasswordConfirmationFieldName };
                availableFields = availableFields.Where(f => !passwordFields.Contains(f)).ToList();

                // Check if the user already exists.
                var emailAddress = request.Form[Settings.EmailAddressFieldName];
                var loginValue = request.Form[Settings.LoginFieldName];
                var userRole = request.Form[Settings.RoleFieldName];
                var query = SetupAccountQuery(Settings.CheckIfAccountExistsQuery, userId, subAccountId: subAccountId, emailAddress: emailAddress, loginValue: loginValue);
                foreach (var field in availableFields)
                {
                    var formValue = GetFormValue(field);
                    if (formValue == null)
                    {
                        continue;
                    }

                    var parameterName = DatabaseHelpers.CreateValidParameterName(field);
                    DatabaseConnection.AddParameter(parameterName, formValue);
                    query = query.ReplaceCaseInsensitive($"'{{{parameterName}}}'", $"?{parameterName}").ReplaceCaseInsensitive($"{{{parameterName}}}", $"?{parameterName}");
                }

                var dataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);

                if (dataTable.Rows.Count > 0)
                {
                    result = CreateOrUpdateAccountResults.UserAlreadyExists;
                    var errorTemplate = await StringReplacementsService.DoAllReplacementsAsync(Settings.TemplateError, dataTable.Rows[0], Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables);
                    return (result, errorTemplate, SuccessTemplate: "", UserId: 0, SubAccountId: 0, Role: "");
                }

                if ((Settings.ComponentMode == ComponentModes.SubAccountsManagement && subAccountId > 0) || (Settings.ComponentMode != ComponentModes.SubAccountsManagement && userId > 0))
                {
                    query = SetupAccountQuery(Settings.UpdateAccountQuery, userId, subAccountId: subAccountId, emailAddress: emailAddress, loginValue: loginValue, role: userRole);
                }
                else
                {
                    query = SetupAccountQuery(Settings.CreateAccountQuery, userId, subAccountId: subAccountId, emailAddress: emailAddress, loginValue: loginValue, role: userRole);
                }

                // Add fields to main query, for creating or updating an account.
                foreach (var field in availableFields)
                {
                    var formValue = GetFormValue(field);
                    if (formValue == null)
                    {
                        continue;
                    }

                    var parameterName = DatabaseHelpers.CreateValidParameterName(field);
                    DatabaseConnection.AddParameter(parameterName, formValue);
                    query = query.ReplaceCaseInsensitive($"'{{{parameterName}}}'", $"?{parameterName}").ReplaceCaseInsensitive($"{{{parameterName}}}", $"?{parameterName}");
                }

                // Add / update the account.
                dataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);

                // Get the user ID, if a new account was created. This will throw an exception if the CreateAccountQuery did not return a new ID, so that the changes will be rolled back and it's a developer mistake, not a user mistake.
                if (userId <= 0)
                {
                    if (Settings.ComponentMode == ComponentModes.SubAccountsManagement)
                    {
                        throw new Exception("Trying to update or create a sub account, but we have no valid user Id!");
                    }

                    if (dataTable.Rows.Count == 0 || dataTable.Rows[0].IsNull(Constants.UserIdColumn))
                    {
                        throw new Exception("Account was created, but query did not return a valid user ID, rolling back changes...");
                    }

                    userId = Convert.ToUInt64(dataTable.Rows[0][Constants.UserIdColumn]);
                    createdNewAccount = true;
                    if (userId <= 0)
                    {
                        throw new Exception("Account was created, but query did not return a valid user ID, rolling back changes...");
                    }
                }

                if (Settings.ComponentMode == ComponentModes.SubAccountsManagement && subAccountId <= 0)
                {
                    if (dataTable.Rows.Count == 0 || dataTable.Rows[0].IsNull(Constants.UserIdColumn))
                    {
                        throw new Exception("Sub account was created, but query did not return a valid user ID, rolling back changes...");
                    }

                    subAccountId = Convert.ToUInt64(dataTable.Rows[0][Constants.UserIdColumn]);
                    if (subAccountId <= 0)
                    {
                        throw new Exception("Sub account was created, but query did not return a valid user ID, rolling back changes...");
                    }
                }

                // Save the fields / entity properties, if a query is entered for that.
                if (!String.IsNullOrWhiteSpace(Settings.SetValueInWiserEntityPropertyQuery))
                {
                    DatabaseConnection.AddParameter("userId", userId);
                    DatabaseConnection.AddParameter("subAccountId", subAccountId);
                    query = Settings.SetValueInWiserEntityPropertyQuery.ReplaceCaseInsensitive("'{name}'", "?name").ReplaceCaseInsensitive("{name}", "?name").ReplaceCaseInsensitive("'{value}'", "?value").ReplaceCaseInsensitive("{value}", "?value");

                    foreach (var field in availableFields)
                    {
                        var formValue = GetFormValue(field);
                        if (Settings.IgnoreEmptyValues && String.IsNullOrWhiteSpace(formValue))
                        {
                            continue;
                        }

                        DatabaseConnection.AddParameter("name", field);
                        DatabaseConnection.AddParameter("value", formValue);
                        await RenderAndExecuteQueryAsync(query, skipCache: true);
                    }
                }

                if (createdNewAccount && Settings.SendNotificationsForNewAccounts)
                {
                    await SendNewAccountNotificationAsync(userId);
                }

                // Save the Google Analytics client ID. Make sure to always save it, even if the required settings don't contain a value.
                await SaveGoogleClientIdAsync(createdNewAccount ? userId : (await AccountsService.GetUserDataFromCookieAsync()).UserId);

                if (useTransaction)
                {
                    // Everything succeeded, commit transaction and return the result
                    await DatabaseConnection.CommitTransactionAsync();
                }

                return (result, ErrorTemplate: "", SuccessTemplate: Settings.TemplateSuccess, userId, subAccountId, Role: userRole);
            }
            catch
            {
                await DatabaseConnection.RollbackTransactionAsync(false);
                throw;
            }
        }

        /// <summary>
        /// Sends an e-mail to the site admin(s) when a new account has been created.
        /// </summary>
        /// <param name="newUserId">The ID of the newly created account.</param>
        /// <returns></returns>
        private async Task SendNewAccountNotificationAsync(ulong newUserId)
        {
            // Get the data we need to send the mail.
            var subject = "";
            var body = "";
            var senderEmail = "";
            var senderName = "";
            var receiverEmail = "";
            var receiverName = "";
            var bcc = "";

            if (!String.IsNullOrWhiteSpace(Settings.QueryNotificationEmail))
            {
                var query = Settings.QueryNotificationEmail.ReplaceCaseInsensitive("'{userId}'", "?userId").ReplaceCaseInsensitive("{userId}", "?userId");
                var dataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);

                if (dataTable.Rows.Count == 0)
                {
                    WriteToTrace("A query was entered in 'QueryPasswordForgottenEmail', but that query returned no results!", true);

                    await SendNotificationMailAsync(newUserId, subject, body, receiverEmail, receiverName, bcc, senderEmail, senderName);
                }
                else
                {
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        // subject, body, senderName and senderEmail.
                        if (dataTable.Columns.Contains("subject"))
                        {
                            subject = dataRow.Field<string>("subject");

                            // Replace others values in the subject and body. This is useful if, for example, the values come from a template in the email templates module
                            // and/or the replacements require encryption or decryption, which MySQL cannot handle.
                            subject = await StringReplacementsService.DoAllReplacementsAsync(subject, dataRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables);
                        }

                        if (dataTable.Columns.Contains("body"))
                        {
                            body = dataRow.Field<string>("body");

                            // Replace others values in the subject and body. This is useful if, for example, the values come from a template in the email templates module
                            // and/or the replacements require encryption or decryption, which MySQL cannot handle.
                            body = await StringReplacementsService.DoAllReplacementsAsync(body, dataRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables);
                        }

                        if (dataTable.Columns.Contains("senderEmail"))
                        {
                            senderEmail = dataRow.Field<string>("senderEmail");
                        }

                        if (dataTable.Columns.Contains("senderName"))
                        {
                            senderName = dataRow.Field<string>("senderName");
                        }

                        if (dataTable.Columns.Contains("receiverEmail"))
                        {
                            receiverEmail = dataRow.Field<string>("receiverEmail");
                        }

                        if (dataTable.Columns.Contains("receiverName"))
                        {
                            receiverName = dataRow.Field<string>("receiverName");
                        }

                        if (dataTable.Columns.Contains("bcc"))
                        {
                            bcc = dataRow.Field<string>("bcc");
                        }

                        await SendNotificationMailAsync(newUserId, subject, body, receiverEmail, receiverName, bcc, senderEmail, senderName);
                    }
                }
            }
        }

        private async Task SendNotificationMailAsync(ulong newUserId, string subject, string body, string receiverEmail, string receiverName, string bcc, string senderEmail, string senderName)
        {
            if (String.IsNullOrWhiteSpace(subject))
            {
                subject = Settings.SubjectNewAccountNotificationEmail;
            }

            if (String.IsNullOrWhiteSpace(body))
            {
                body = Settings.BodyNewAccountNotificationEmail;
            }

            if (String.IsNullOrWhiteSpace(receiverEmail))
            {
                receiverEmail = Settings.NotificationsReceiver;
            }

            if (String.IsNullOrWhiteSpace(receiverName))
            {
                receiverName = receiverEmail;
            }

            if (String.IsNullOrWhiteSpace(bcc))
            {
                bcc = Settings.NotificationsBcc;
            }

            if (String.IsNullOrWhiteSpace(receiverEmail))
            {
                WriteToTrace("Trying to send notification e-mail, but 'NotificationsReceiver' is empty.", true);
                return;
            }

            WriteToTrace("Sending reset password e-mail...");
            WriteToTrace($"receiverMail: {receiverEmail}");
            WriteToTrace($"bodyHtml: {body}");
            WriteToTrace($"subject: {subject}");

            body = body.ReplaceCaseInsensitive("<jform", "<form")
                .ReplaceCaseInsensitive("</jform", "</form")
                .ReplaceCaseInsensitive("<jhtml", "<html")
                .ReplaceCaseInsensitive("</jhtml", "</html")
                .ReplaceCaseInsensitive("<jhead", "<head")
                .ReplaceCaseInsensitive("</jhead", "</head")
                .ReplaceCaseInsensitive("<jtitle", "<title")
                .ReplaceCaseInsensitive("</jtitle", "</title")
                .ReplaceCaseInsensitive("<jbody", "<body")
                .ReplaceCaseInsensitive("</jbody", "</body")
                .ReplaceCaseInsensitive("{userId}", newUserId.ToString());

            await communicationsService.SendEmailAsync(await StringReplacementsService.DoAllReplacementsAsync(receiverEmail, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                                                       await StringReplacementsService.DoAllReplacementsAsync(subject, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                                                       await StringReplacementsService.DoAllReplacementsAsync(body),
                                                       await StringReplacementsService.DoAllReplacementsAsync(receiverName, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                                                       bcc: await StringReplacementsService.DoAllReplacementsAsync(bcc, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                                                       sender: await StringReplacementsService.DoAllReplacementsAsync(senderEmail, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                                                       senderName: await StringReplacementsService.DoAllReplacementsAsync(senderName, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
        }

        /// <summary>
        /// Gets a submitted form value. If this is a password field, it will return a SHA512 hash with salt of that value.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The value of the field or NULL if it wasn't found.</returns>
        private string GetFormValue(string fieldName)
        {
            var httpContext = HttpContext;
            if (httpContext == null)
            {
                throw new Exception("No http context available.");
            }

            var request = httpContext.Request;
            if (!request.Form.ContainsKey(fieldName))
            {
                return null;
            }

            var result = request.Form[fieldName].ToString();
            if (fieldName.Equals(Settings.PasswordFieldName, StringComparison.OrdinalIgnoreCase) || fieldName.Equals(Settings.NewPasswordFieldName, StringComparison.OrdinalIgnoreCase) || fieldName.Equals(Settings.NewPasswordConfirmationFieldName, StringComparison.OrdinalIgnoreCase))
            {
                // Password will be validated/saved separately.
                return null;
            }

            return result;
        }

        /// <summary>
        /// Attempt to login the user. Will return whether that was successful or what the error was.
        /// </summary>
        /// <param name="stepNumber">Optional: The current step number (for <see cref="ComponentModes.LoginMultipleSteps"/>). Default is 1.</param>
        /// <param name="loginValue">Optional: The username of e-mail address of the user trying to login. If empty, it will be retrieved from the posted data.</param>
        /// <param name="password">Optional: The password of the user that is trying to login. If empty, it will be retrieved from the posted data.</param>
        /// <param name="overrideComponentMode">Optional: Use this to force a specific <see langword="ComponentMode"/>.</param>
        /// <param name="encryptedUserId">Optional: An encrypted user ID for logging in via a link.</param>
        /// <returns>A <see cref="Tuple"/> containing the <see cref="LoginResults"/>, userId and e-mail address.</returns>
        private async Task<(LoginResults Result, ulong UserId, string EmailAddress)> LoginUserAsync(int stepNumber = 1, string loginValue = null, string password = null, int overrideComponentMode = 0, string encryptedUserId = null)
        {
            if (HttpContext == null)
            {
                throw new Exception("No HttpContext available!");
            }

            var request = Request;
            var session = HttpContext.Session;
            var actualComponentMode = overrideComponentMode > 0 ? (ComponentModes)overrideComponentMode : Settings.ComponentMode;
            var wiserValidationToken = HttpContextHelpers.GetRequestValue(HttpContext, Settings.WiserLoginTokenKey);

            // Check if we have enough information to login.
            if (String.IsNullOrWhiteSpace(loginValue) && request.HasFormContentType)
            {
                loginValue = request.Form[Settings.LoginFieldName].ToString();
            }

            if (String.IsNullOrWhiteSpace(loginValue) && stepNumber > 1)
            {
                loginValue = session.GetString($"{Constants.LoginValueSessionKey}_{ComponentId}");
            }

            if (String.IsNullOrWhiteSpace(loginValue) && String.IsNullOrWhiteSpace(encryptedUserId))
            {
                WriteToTrace("No username or e-mail address given.");
                return (Result: LoginResults.InvalidUsernameOrPassword, UserId: 0, EmailAddress: null);
            }

            // Decrypt the user ID, if we have one.
            ulong decryptedUserId = 0;
            if (!String.IsNullOrWhiteSpace(encryptedUserId))
            {
                try
                {
                    var decryptedId = encryptedUserId.DecryptWithAesWithSalt(withDateTime: true);
                    UInt64.TryParse(decryptedId, out decryptedUserId);
                }
                catch (Exception exception)
                {
                    WriteToTrace($"An error occurred while trying to decrypt the user ID '{encryptedUserId}'. The error was: {exception}", true);
                }

                if (decryptedUserId == 0)
                {
                    return (Result: LoginResults.InvalidUserId, UserId: decryptedUserId, EmailAddress: null);
                }

                if (String.IsNullOrWhiteSpace(wiserValidationToken) || !wiserValidationToken.Equals(Settings.WiserLoginToken))
                {
                    return (Result: LoginResults.InvalidValidationToken, UserId: decryptedUserId, EmailAddress: null);
                }
            }

            // Check for Google 2FA.
            string googleAuthenticatorPin = null;
            var usingGoogleAuthentication = false;

            if (request.HasFormContentType)
            {
                googleAuthenticatorPin = request.Form[Constants.GoogleAuthenticationPinFieldName].ToString();
                var googleAuthenticationVerificationId = request.Form[Constants.GoogleAuthenticationVerificationIdFieldName];
                usingGoogleAuthentication = Settings.EnableGoogleAuthenticator && !String.IsNullOrWhiteSpace(googleAuthenticationVerificationId);
            }

            if (usingGoogleAuthentication && String.IsNullOrWhiteSpace(googleAuthenticatorPin))
            {
                WriteToTrace("No googleAuthenticationPin or googleAuthenticationVerificationId given.");
                return (Result: LoginResults.InvalidTwoFactorAuthentication, UserId: 0, EmailAddress: null);
            }

            // Save the login value in the session, so that we can remember it during the rest of the steps if the mode is LoginMultipleSteps.
            if (!String.IsNullOrWhiteSpace(loginValue))
            {
                session.SetString($"{Constants.LoginValueSessionKey}_{ComponentId}", loginValue);
            }

            // Get user information.
            var query = decryptedUserId > 0 ? SetupAccountQuery(Settings.AutoLoginQuery, decryptedUserId) : SetupAccountQuery(Settings.LoginQuery, loginValue: loginValue);
            var accountResult = await RenderAndExecuteQueryAsync(query, skipCache: true);

            // User doesn't exist.
            if (accountResult == null || accountResult.Rows.Count == 0)
            {
                WriteToTrace($"No user found with login '{loginValue}'.");
                return (Result: (actualComponentMode == ComponentModes.LoginMultipleSteps ? LoginResults.UserDoesNotExist : LoginResults.InvalidUsernameOrPassword), UserId: 0, EmailAddress: null);
            }

            // Check if the user is allowed to login.
            var loggedInUserId = Convert.ToUInt64(accountResult.Rows[0][Constants.UserIdColumn]);
            var mainUserId = !accountResult.Columns.Contains(Constants.MainAccountIdColumn) || accountResult.Rows[0].IsNull(Constants.MainAccountIdColumn) ? 0 : Convert.ToUInt64(accountResult.Rows[0][Constants.MainAccountIdColumn]);
            var userRole = !accountResult.Columns.Contains(Constants.RoleColumn) || accountResult.Rows[0].IsNull(Constants.RoleColumn) ? "" : accountResult.Rows[0].Field<string>(Constants.RoleColumn);
            var userEmail = !accountResult.Columns.Contains(Constants.EmailAddressColumn) || accountResult.Rows[0].IsNull(Constants.EmailAddressColumn) ? "" : accountResult.Rows[0].Field<string>(Constants.EmailAddressColumn);
            var failedLoginAttempts = !accountResult.Columns.Contains(Constants.FailedLoginAttemptsColumn) || accountResult.Rows[0].IsNull(Constants.FailedLoginAttemptsColumn) ? 0 : Convert.ToInt32(accountResult.Rows[0][Constants.FailedLoginAttemptsColumn]);

            if (mainUserId == 0)
            {
                mainUserId = loggedInUserId;
            }

            if (accountResult.Columns.Contains(Constants.LastLoginDateColumn))
            {
                DateTime lastLoginDate;
                DateTime.TryParseExact(accountResult.Rows[0].Field<string>(Constants.LastLoginDateColumn), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out lastLoginDate);
                if (Settings.MaximumAmountOfFailedLoginAttempts > 0 && Settings.DefaultLockoutTime.HasValue && Settings.DefaultLockoutTime.Value > 0 && failedLoginAttempts >= Settings.MaximumAmountOfFailedLoginAttempts && lastLoginDate.AddMinutes(Settings.DefaultLockoutTime.Value) > DateTime.Now)
                {
                    return (Result: LoginResults.TooManyAttempts, UserId: 0, EmailAddress: userEmail);
                }
            }

            // Make sure we have a valid user ID.
            if (loggedInUserId <= 0)
            {
                WriteToTrace("The login was successful, but the query returned an invalid user ID!");
                return (Result: (actualComponentMode == ComponentModes.LoginMultipleSteps ? LoginResults.UserDoesNotExist : LoginResults.InvalidUsernameOrPassword), UserId: 0, EmailAddress: null);
            }

            // Login with password.
            if (decryptedUserId == 0)
            {
                // Check if the user is active. It's not active if it doesn't have a password yet.
                var passwordHash = accountResult.Rows[0].Field<string>(Constants.PasswordColumn);
                if (String.IsNullOrWhiteSpace(passwordHash))
                {
                    WriteToTrace("The password hash is empty.");
                    return (Result: LoginResults.UserNotActivated, UserId: loggedInUserId, EmailAddress: userEmail);
                }

                // If the component mode is LoginMultipleSteps and we're still on the first step, don't check the password yet.
                if (actualComponentMode == ComponentModes.LoginMultipleSteps && stepNumber <= 1)
                {
                    return (Result: LoginResults.Success, UserId: loggedInUserId, EmailAddress: userEmail);
                }

                // Verify the user's password.
                if (String.IsNullOrWhiteSpace(password))
                {
                    password = request.Form[Settings.PasswordFieldName].ToString();
                }

                if (!usingGoogleAuthentication && !password.VerifySha512(passwordHash))
                {
                    WriteToTrace("The password hash validation failed.");
                    await SaveLoginAttemptAsync(false, loggedInUserId);
                    return (Result: (actualComponentMode == ComponentModes.LoginMultipleSteps ? LoginResults.InvalidPassword : LoginResults.InvalidUsernameOrPassword), UserId: 0, EmailAddress: userEmail);
                }

                // Verify 2 factor authentication, if enabled.
                if (usingGoogleAuthentication)
                {
                    var sessionUserId = session.GetString($"{Constants.UserIdSessionKey}_{ComponentId}");
                    throw new NotImplementedException("Google authenticator not yet implemented!");
                    /* TODO:
                    Dim googleAuthenticator = New GoogleAuthenticator(GoogleAuthenticatorSiteId, sessionUserId, loginValue, "")
                    If Not googleAuthenticator.Verify(googleAuthenticationVerificationId, googleAuthenticationPin) Then
                        SaveLoginAttempt(connection, False, loggedInUserId)
                        Return (Result:=LoginResults.InvalidTwoFactorAuthentication, 0, EmailAddress:=userEmail)
                    End If

                    googleAuthenticator.SaveAndEnable(googleAuthenticationVerificationId)
                    */
                }
                else if (Settings.EnableGoogleAuthenticator)
                {
                    session.SetString($"{Constants.UserIdSessionKey}_{ComponentId}", loggedInUserId.ToString());
                    return (Result: LoginResults.TwoFactorAuthenticationRequired, UserId: loggedInUserId, EmailAddress: userEmail);
                }
            }

            // Everything succeeded, so generate a cookie for the user and reset any failed login attempts.
            var amountOfDaysToRememberCookie = GetAmountOfDaysToRememberCookie();
            var cookieValue = await AccountsService.GenerateNewCookieTokenAsync(loggedInUserId, mainUserId, !amountOfDaysToRememberCookie.HasValue || amountOfDaysToRememberCookie.Value <= 0 ? 0 : amountOfDaysToRememberCookie.Value, Settings.EntityType, Settings.SubAccountEntityType, userRole);
            if (decryptedUserId == 0)
            {
                await SaveGoogleClientIdAsync(loggedInUserId);
            }

            var offset = (amountOfDaysToRememberCookie ?? 0) <= 0 ? (DateTimeOffset?)null : DateTimeOffset.Now.AddDays(amountOfDaysToRememberCookie.Value);
            HttpContextHelpers.WriteCookie(HttpContext, Constants.CookieName, cookieValue, offset, isEssential: true);

            if (decryptedUserId == 0)
            {
                await SaveLoginAttemptAsync(true, loggedInUserId);
            }

            return (Result: LoginResults.Success, UserId: loggedInUserId, EmailAddress: userEmail);
        }

        /// <summary>
        /// Gets the Google Client ID from the Google Analytics cookie and saved it.
        /// </summary>
        /// <param name="userIdForGoogleCid">The ID of the user to save the CID for.</param>
        private async Task SaveGoogleClientIdAsync(ulong userIdForGoogleCid)
        {
            var googleClientIdCookieValue = Request.Cookies[Constants.GoogleAnalyticsCookieName];
            if (String.IsNullOrWhiteSpace(googleClientIdCookieValue))
            {
                return;
            }

            // GA1.2.1248174149.1587127355
            var clientIdSplit = googleClientIdCookieValue.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (clientIdSplit.Length != 4)
            {
                WriteToTrace($"Google Analytics cookie found ({Constants.GoogleAnalyticsCookieName}: {googleClientIdCookieValue}), but is not a valid format. Expected to have 3 dots, but it has {clientIdSplit.Length - 1}", true);
                return;
            }

            if (userIdForGoogleCid <= 0)
            {
                WriteToTrace($"Google Analytics cookie found ({Constants.GoogleAnalyticsCookieName}: {googleClientIdCookieValue}), but the user is not logged in and we did not create a new account (successfully).", true);
                return;
            }

            var googleClientId = String.Join(".", clientIdSplit.Skip(2));

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(Settings.EntityType);

            DatabaseConnection.AddParameter("userId", userIdForGoogleCid);
            DatabaseConnection.AddParameter("name", String.IsNullOrWhiteSpace(Settings.GoogleClientIdFieldName) ? Constants.DefaultGoogleCidFieldName : Settings.GoogleClientIdFieldName);
            DatabaseConnection.AddParameter("value", googleClientId);

            await RenderAndExecuteQueryAsync($"INSERT INTO {tablePrefix}{WiserTableNames.WiserItemDetail} (item_id, `key`, value) VALUES (?userId, ?name, ?value) ON DUPLICATE KEY UPDATE value = VALUES(value)", skipCache: true);
        }

        /// <summary>
        /// Redirect the user to the Google login page.
        /// </summary>
        private void RedirectToGoogleLogin()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends an e-mail to the user with instructions about how they can set a new password.
        /// </summary>
        /// <param name="emailAddress"></param>
        private async Task SendResetPasswordEmail(string emailAddress = null)
        {
            // First check if the user exists.
            if (String.IsNullOrWhiteSpace(emailAddress))
            {
                emailAddress = Request.Form[Settings.EmailAddressFieldName].ToString();
            }

            var query = SetupAccountQuery(Settings.GetUserIdViaEmailAddressQuery, emailAddress: emailAddress);
            var result = await RenderAndExecuteQueryAsync(query, skipCache: true);

            if (result.Rows.Count == 0)
            {
                // Do nothing if the user doesn't exist.
                return;
            }

            var userDataRow = result.Rows[0];
            var userId = userDataRow[Constants.UserIdColumn];

            // Generate a new token for the user.
            string token;
            var tokenExpireDate = !Settings.ResetPasswordTokenValidity.HasValue || Settings.ResetPasswordTokenValidity.Value == 0 ? DateTime.MaxValue : DateTime.Now.AddDays(Settings.ResetPasswordTokenValidity.Value);
            using (var secureRandomNumberGenerator = RandomNumberGenerator.Create())
            {
                var tokenData = new byte[129];
                secureRandomNumberGenerator.GetBytes(tokenData);
                token = Convert.ToBase64String(tokenData);
            }

            // Save the token and expire date.
            query = Settings.SaveResetPasswordValuesQuery.ReplaceCaseInsensitive("'{resetPasswordToken}'", "?resetPasswordToken").ReplaceCaseInsensitive("{resetPasswordToken}", "?resetPasswordToken")
                .ReplaceCaseInsensitive("'{resetPasswordTokenFieldName}'", "?resetPasswordTokenFieldName").ReplaceCaseInsensitive("{resetPasswordTokenFieldName}", "?resetPasswordTokenFieldName")
                .ReplaceCaseInsensitive("'{resetPasswordExpireDate}'", "?resetPasswordExpireDate").ReplaceCaseInsensitive("{resetPasswordExpireDate}", "?resetPasswordExpireDate")
                .ReplaceCaseInsensitive("'{resetPasswordExpireDateFieldName}'", "?resetPasswordExpireDateFieldName").ReplaceCaseInsensitive("{resetPasswordExpireDateFieldName}", "?resetPasswordExpireDateFieldName")
                .ReplaceCaseInsensitive("'{userId}'", "?userId").ReplaceCaseInsensitive("{userId}", "?userId");

            DatabaseConnection.ClearParameters();
            DatabaseConnection.AddParameter("userId", userId);
            DatabaseConnection.AddParameter("resetPasswordToken", token);
            DatabaseConnection.AddParameter("resetPasswordTokenFieldName", Settings.ResetPasswordTokenFieldName);
            DatabaseConnection.AddParameter("resetPasswordExpireDate", tokenExpireDate);
            DatabaseConnection.AddParameter("resetPasswordExpireDateFieldName", Settings.ResetPasswordExpireDateFieldName);
            await RenderAndExecuteQueryAsync(query, skipCache: true);

            // Get the data we need to send the mail.
            var subject = "";
            var body = "";
            var senderEmail = "";
            var senderName = "";

            if (!String.IsNullOrWhiteSpace(Settings.QueryPasswordForgottenEmail))
            {
                query = Settings.QueryPasswordForgottenEmail.ReplaceCaseInsensitive("'{userId}'", "?userId").ReplaceCaseInsensitive("{userId}", "?userId");
                result = await RenderAndExecuteQueryAsync(query, skipCache: true);

                if (result.Rows.Count == 0)
                {
                    WriteToTrace("A query was entered in 'QueryPasswordForgottenEmail', but that query returned no results!", true);
                }
                else
                {
                    // subject, body, senderName and senderEmail.
                    subject = result.Rows[0].Field<string>("subject");
                    body = result.Rows[0].Field<string>("body");
                    senderEmail = result.Rows[0].Field<string>("senderEmail");
                    senderName = result.Rows[0].Field<string>("senderName");

                    // Replace others values in the subject and body. This is useful if, for example, the values come from a template in the email templates module
                    // and/or the replacements require encryption or decryption, which MySQL cannot handle.
                    subject = await StringReplacementsService.DoAllReplacementsAsync(subject, result.Rows[0], Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables);
                    body = await StringReplacementsService.DoAllReplacementsAsync(body, result.Rows[0], Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables);
                }
            }

            if (String.IsNullOrWhiteSpace(subject))
            {
                subject = Settings.SubjectResetPasswordEmail;
            }

            if (String.IsNullOrWhiteSpace(body))
            {
                body = Settings.BodyResetPasswordEmail;
            }

            // Build the reset password URL.
            Uri baseUrl;
            if (HttpContext == null)
            {
                baseUrl = new Uri(Settings.ResetPasswordUrl);
            }
            else if (String.IsNullOrWhiteSpace(Settings.ResetPasswordUrl))
            {
                baseUrl = Request.GetTypedHeaders().Referer ?? new Uri(Request.GetDisplayUrl());
            }
            else if (Settings.ResetPasswordUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || Settings.ResetPasswordUrl.StartsWith("//"))
            {
                baseUrl = new Uri(Settings.ResetPasswordUrl);
            }
            else
            {
                baseUrl = new Uri(Request.GetTypedHeaders().Referer ?? new Uri(Request.GetDisplayUrl()), Settings.ResetPasswordUrl);
            }

            var uriBuilder = new UriBuilder(baseUrl);
            var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryString[Constants.UserIdQueryStringKey] = userId.ToString().EncryptWithAes(gclSettings.AccountUserIdEncryptionKey);
            queryString[Constants.ResetPasswordTokenQueryStringKey] = token;
            uriBuilder.Query = queryString.ToString();

            WriteToTrace("Sending reset password e-mail...");
            WriteToTrace($"senderName: {senderName}");
            WriteToTrace($"senderEmail: {senderEmail}");
            WriteToTrace($"receiverMail: {emailAddress}");
            WriteToTrace($"bodyHtml: {body}");
            WriteToTrace($"subject: {subject}");
            WriteToTrace($"token: {token}");
            WriteToTrace($"url: {uriBuilder}");

            body = body.ReplaceCaseInsensitive("{url}", uriBuilder.ToString()).ReplaceCaseInsensitive("<jform", "<form").ReplaceCaseInsensitive("</jform", "</form").ReplaceCaseInsensitive("<jhtml", "<html").ReplaceCaseInsensitive("</jhtml", "</html")
                .ReplaceCaseInsensitive("<jhead", "<head").ReplaceCaseInsensitive("</jhead", "</head").ReplaceCaseInsensitive("<jtitle", "<title").ReplaceCaseInsensitive("</jtitle", "</title").ReplaceCaseInsensitive("<jbody", "<body")
                .ReplaceCaseInsensitive("</jbody", "</body");

            await communicationsService.SendEmailAsync(await StringReplacementsService.DoAllReplacementsAsync(emailAddress, userDataRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                await StringReplacementsService.DoAllReplacementsAsync(subject, userDataRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                await StringReplacementsService.DoAllReplacementsAsync(body, userDataRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                sender: await StringReplacementsService.DoAllReplacementsAsync(senderEmail, userDataRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables),
                senderName: await StringReplacementsService.DoAllReplacementsAsync(senderName, userDataRow, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables));
        }

        /// <summary>
        /// Saves a login attempt in the details of the user. This will use the query in <see cref="JsonFormatter.Settings.SaveLoginAttemptQuery"/>.
        /// </summary>
        /// <param name="success">Whether the login attempt was successful or not.</param>
        /// <param name="userId">The ID of the user that is attempting to login.</param>
        private async Task SaveLoginAttemptAsync(bool success, ulong userId)
        {
            var query = SetupAccountQuery(Settings.SaveLoginAttemptQuery, userId, success: success);
            if (String.IsNullOrWhiteSpace(query))
            {
                return;
            }

            await RenderAndExecuteQueryAsync(query, skipCache: true);
        }

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="userId">The ID of the user or sub account to change the password of.</param>
        /// <param name="isMakingNewAccount">Optional: Set to <see langword="true" /> if the user is currently creating a new account, so that the old password does not get checked. Default is <see langword="false"/>.</param>
        /// <returns>A success or error type.</returns>
        private async Task<ResetOrChangePasswordResults> ChangePasswordAsync(ulong userId, bool isMakingNewAccount = false)
        {
            if (userId <= 0)
            {
                return ResetOrChangePasswordResults.InvalidTokenOrUser;
            }

            var request = Request;
            if (request == null)
            {
                throw new Exception("No http context found.");
            }

            var oldPassword = request.Form[Settings.PasswordFieldName].ToString();
            var newPassword = request.Form[Settings.NewPasswordFieldName].ToString();
            var newPasswordConfirmation = request.Form[Settings.NewPasswordConfirmationFieldName].ToString();
            string query;

            // Then validate the old password, if the user is changing the password in their account.
            if (Settings.ComponentMode != ComponentModes.ResetPassword && Settings.ComponentMode != ComponentModes.SubAccountsManagement && userId > 0 && !isMakingNewAccount && Settings.RequireCurrentPasswordForChangingPassword)
            {
                query = SetupAccountQuery(Settings.ValidatePasswordQuery, userId);
                var dataTable = await RenderAndExecuteQueryAsync(query, skipCache: true);
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    return ResetOrChangePasswordResults.InvalidTokenOrUser;
                }

                var passwordHash = dataTable.Rows[0].Field<string>(Constants.PasswordColumn);
                if (!oldPassword.VerifySha512(passwordHash))
                {
                    return ResetOrChangePasswordResults.OldPasswordInvalid;
                }
            }

            if (Settings.ComponentMode == ComponentModes.ResetPassword && (String.IsNullOrWhiteSpace(newPassword) || String.IsNullOrWhiteSpace(newPasswordConfirmation)))
            {
                return ResetOrChangePasswordResults.EmptyPassword;
            }

            if (newPassword != newPasswordConfirmation)
            {
                return ResetOrChangePasswordResults.PasswordsNotTheSame;
            }

            // Validate the password with a regex, if we have one.
            if (!String.IsNullOrWhiteSpace(newPassword) && !String.IsNullOrWhiteSpace(Settings.PasswordValidationRegex))
            {
                var regex = new Regex(Settings.PasswordValidationRegex);
                if (!regex.IsMatch(newPassword))
                {
                    return ResetOrChangePasswordResults.PasswordNotSecure;
                }
            }

            // If everything is OK, save the new password.
            if (userId > 0 && !String.IsNullOrWhiteSpace(newPassword))
            {
                var newPasswordHash = newPassword.ToSha512ForPasswords();
                query = SetupAccountQuery(Settings.ChangePasswordQuery, userId, passwordHash: newPasswordHash);
                await RenderAndExecuteQueryAsync(query, skipCache: true);
            }

            return ResetOrChangePasswordResults.Success;
        }

        #endregion

        /// <summary>
        /// Do all default login replacements on a SQL template and adds the variables to the <see cproperty="SystemConnection"/>.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="userId"></param>
        /// <param name="loginValue"></param>
        /// <param name="emailAddress"></param>
        /// <param name="token"></param>
        /// <param name="success"></param>
        /// <param name="passwordHash"></param>
        /// <param name="subAccountId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        private string SetupAccountQuery(string template,
            ulong userId = 0,
            string loginValue = null,
            string emailAddress = null,
            string token = null,
            bool success = true,
            string passwordHash = null,
            ulong subAccountId = 0,
            string role = "")
        {
            if (String.IsNullOrWhiteSpace(template))
            {
                return template;
            }

            DatabaseConnection.ClearParameters();
            DatabaseConnection.AddParameter("entityType", Settings.EntityType);
            DatabaseConnection.AddParameter("subAccountEntityType", Settings.SubAccountEntityType);
            DatabaseConnection.AddParameter("loginFieldName", Settings.LoginFieldName);
            DatabaseConnection.AddParameter("passwordFieldName", Settings.PasswordFieldName);
            DatabaseConnection.AddParameter("emailAddressFieldName", Settings.EmailAddressFieldName);
            DatabaseConnection.AddParameter("failedLoginAttemptsFieldName", Settings.FailedLoginAttemptsFieldName);
            DatabaseConnection.AddParameter("lastLoginAttemptFieldName", Settings.LastLoginAttemptFieldName);
            DatabaseConnection.AddParameter("resetPasswordTokenFieldName", Settings.ResetPasswordTokenFieldName);
            DatabaseConnection.AddParameter("resetPasswordExpireDateFieldName", Settings.ResetPasswordExpireDateFieldName);
            DatabaseConnection.AddParameter("subAccountLinkTypeNumber", Settings.SubAccountLinkTypeNumber);
            DatabaseConnection.AddParameter("roleFieldName", Settings.RoleFieldName);
            DatabaseConnection.AddParameter("userId", userId);
            DatabaseConnection.AddParameter("login", loginValue);
            DatabaseConnection.AddParameter("emailAddress", emailAddress);
            DatabaseConnection.AddParameter("token", token);
            DatabaseConnection.AddParameter("success", success);
            DatabaseConnection.AddParameter("newPasswordHash", passwordHash);
            DatabaseConnection.AddParameter("subAccountId", subAccountId);
            DatabaseConnection.AddParameter("role", role);

            // Check if the encrypted values are requested in the query template before adding them to the parameters.
            if (template.Contains("{emailAddressGclAesEncrypted}") || template.Contains("?emailAddressGclAesEncrypted"))
            {
                var value = String.IsNullOrWhiteSpace(emailAddress) ? "" : emailAddress.EncryptWithAes();
                DatabaseConnection.AddParameter("emailAddressGclAesEncrypted", value);
            }
            else if (template.Contains("{emailAddressAesEncrypted}") || template.Contains("?emailAddressAesEncrypted"))
            {
                var value = String.IsNullOrWhiteSpace(emailAddress) ? "" : emailAddress.EncryptWithAes();
                DatabaseConnection.AddParameter("emailAddressAesEncrypted", value);
            }

            // Check if the template contains a basket id variable
            if (template.Contains("{basketId}") || template.Contains("?basketId"))
            {
                // TODO: In toekomst de cookiename variabel maken door deze toe te voegen aan de variabele naam en er een regex overheen te gooien
                throw new NotImplementedException("TODO: {basketId}");
                //SystemConnection.AddParameter("basketId", ShoppingBasket.GetBasketItemId("winkelmandje"));
            }

            return template.ReplaceCaseInsensitive("'{userId}'", "?userId").ReplaceCaseInsensitive("{userId}", "?userId").ReplaceCaseInsensitive("'{token}'", "?token").ReplaceCaseInsensitive("{token}", "?token")
                .ReplaceCaseInsensitive("'{resetPasswordTokenFieldName}'", "?resetPasswordTokenFieldName").ReplaceCaseInsensitive("{resetPasswordTokenFieldName}", "?resetPasswordTokenFieldName")
                .ReplaceCaseInsensitive("'{resetPasswordExpireDateFieldName}'", "?resetPasswordExpireDateFieldName").ReplaceCaseInsensitive("{resetPasswordExpireDateFieldName}", "?resetPasswordExpireDateFieldName")
                .ReplaceCaseInsensitive("'{loginFieldName}'", "?loginFieldName").ReplaceCaseInsensitive("{loginFieldName}", "?loginFieldName").ReplaceCaseInsensitive("'{entityType}'", "?entityType")
                .ReplaceCaseInsensitive("{entityType}", "?entityType").ReplaceCaseInsensitive("'{failedLoginAttemptsFieldName}'", "?failedLoginAttemptsFieldName")
                .ReplaceCaseInsensitive("{failedLoginAttemptsFieldName}", "?failedLoginAttemptsFieldName").ReplaceCaseInsensitive("'{lastLoginAttemptFieldName}'", "?lastLoginAttemptFieldName")
                .ReplaceCaseInsensitive("{lastLoginAttemptFieldName}", "?failedLoginAttemptsFieldName").ReplaceCaseInsensitive("'{roleFieldName}'", "?roleFieldName").ReplaceCaseInsensitive("{roleFieldName}", "?roleFieldName")
                .ReplaceCaseInsensitive("'{success}'", "?success").ReplaceCaseInsensitive("{success}", "?success").ReplaceCaseInsensitive("'{emailAddress}'", "?emailAddress").ReplaceCaseInsensitive("{emailAddress}", "?emailAddress")
                .ReplaceCaseInsensitive("'{emailAddressGclAesEncrypted}'", "?emailAddressGclAesEncrypted").ReplaceCaseInsensitive("{emailAddressGclAesEncrypted}", "?emailAddressGclAesEncrypted")
                .ReplaceCaseInsensitive("'{emailAddressAesEncrypted}'", "?emailAddressAesEncrypted").ReplaceCaseInsensitive("{emailAddressAesEncrypted}", "?emailAddressAesEncrypted")
                .ReplaceCaseInsensitive("'{emailAddressFieldName}'", "?emailAddressFieldName").ReplaceCaseInsensitive("{emailAddressFieldName}", "?emailAddressFieldName").ReplaceCaseInsensitive("'{newPasswordHash}'", "?newPasswordHash")
                .ReplaceCaseInsensitive("{newPasswordHash}", "?newPasswordHash").ReplaceCaseInsensitive("'{passwordFieldName}'", "?passwordFieldName").ReplaceCaseInsensitive("{passwordFieldName}", "?passwordFieldName")
                .ReplaceCaseInsensitive("'{subAccountLinkTypeNumber}'", "?subAccountLinkTypeNumber").ReplaceCaseInsensitive("{subAccountLinkTypeNumber}", "?subAccountLinkTypeNumber")
                .ReplaceCaseInsensitive("{subAccountEntityType}", "?subAccountEntityType").ReplaceCaseInsensitive("'{subAccountId}'", "?subAccountId").ReplaceCaseInsensitive("{subAccountId}", "?subAccountId")
                .ReplaceCaseInsensitive("'{role}'", "?role").ReplaceCaseInsensitive("{role}", "?role").ReplaceCaseInsensitive("'{basketId}'", "?basketId").ReplaceCaseInsensitive("{basketId}", "?basketId");
        }

        /// <summary>
        /// Do default replacements, such as field names, on a string.
        /// </summary>
        /// <param name="template">The value to do the replacements on.</param>
        /// <returns>The new value with replacements done.</returns>
        private string DoDefaultAccountHtmlReplacements(string template)
        {
            var logoutUrl = QueryHelpers.AddQueryString(HttpContextHelpers.GetOriginalRequestUri(HttpContext).PathAndQuery, $"{Constants.LogoutQueryStringKey}{ComponentId}", "true");
            return template.ReplaceCaseInsensitive("{loginFieldName}", Settings.LoginFieldName).
                ReplaceCaseInsensitive("{passwordFieldName}", Settings.PasswordFieldName).
                ReplaceCaseInsensitive("{newPasswordFieldName}", Settings.NewPasswordFieldName).
                ReplaceCaseInsensitive("{newPasswordConfirmationFieldName}", Settings.NewPasswordConfirmationFieldName).
                ReplaceCaseInsensitive("{entityType}", Settings.EntityType).
                ReplaceCaseInsensitive("{contentId}", ComponentId.ToString()).
                ReplaceCaseInsensitive("{logoutUrl}", logoutUrl).
                ReplaceCaseInsensitive("{emailAddressFieldName}", Settings.EmailAddressFieldName).
                ReplaceCaseInsensitive("<jform", "<form").
                ReplaceCaseInsensitive("</jform", "</form");
        }

        private int? GetAmountOfDaysToRememberCookie()
        {
            if (HttpContext == null || String.IsNullOrWhiteSpace(Settings.RememberMeCheckboxName))
            {
                return Settings.AmountOfDaysToRememberCookie;
            }

            var formValue = Request.HasFormContentType ? Request.Form[Settings.RememberMeCheckboxName] : StringValues.Empty;
            return String.IsNullOrWhiteSpace(formValue) || formValue == "0" ? null : Settings.AmountOfDaysToRememberCookie;
        }
    }
}