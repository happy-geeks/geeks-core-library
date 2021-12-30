using System.ComponentModel;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.Account.Models
{
    public class AccountCmsSettingsModel : CmsSettings
    {
        public Account.ComponentModes ComponentMode { get; set; } = Account.ComponentModes.LoginSingleStep;

        #region Tab Layout properties

        /// <summary>
        /// The main HTML template for the selected mode.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template",
            Description = "The main HTML template for the selected mode.",
            DeveloperRemarks = @"<p>You can use the following variables here:</p>
                                <ul>
                                    <li><strong>{error}:</strong> Put this in the position of where you want to show any potential errors, such as invalid credentials. This variable will be replaced by the value of the property 'TemplateError'.</li>
                                    <li><strong>{success}:</strong> Put this in the position of where you want to show any success message. If you leave out this variable, the GCL will return ONLY the TemplateSuccess after a successful action, otherwise this variable will be replaced by the value of the property 'TemplateSuccess'.</li>
                                    <li><strong>{step}:</strong> The name of the login step.</li>
                                    <li><strong>{stepNumber}:</strong> The number of the login step.</li>
                                    <li><strong>{googleAuthenticationVerificationId}:</strong> Only required if Google 2FA is enabled. If it is, you should add a hidden field with the name 'googleAuthenticationVerificationId' and value '{googleAuthenticationVerificationId}'.</li>
                                    <li><strong>{googleAuthenticationQrImageUrl}:</strong> Only required if Google 2FA is enabled. If it is, add an image that is only visible if the {step} = 'SetupTwoFactorAuthentication' (or {stepNumber} = 3).</li>
                                </ul>
                                <p>If you have a mode that has multiple steps for the user to go through, you can use the variable '{step}' or '{stepNumber}' to check for the step name or number and use if statements to show different HTML for each step. Also always add a hidden input with the name 'accountStepNumber' and a value with the current step number to the form, so that the component knows which step the user is submitting.</p>
                                <p>Possible steps are:</p>
                                <ul>
                                    <li><strong>Initial or 1:</strong> The first / initial step, where the user has to enter their username or e-mail address (and password, if ComponentMode is set to LoginSingleStep).</li>
                                    <li><strong>Password or 2:</strong> The step where the user has to enter their password. Only used when ComponentMode is set to LoginMultipleSteps).</li>
                                    <li><strong>SetupTwoFactorAuthentication or 3:</strong>  The step where the user logged in for the first time since enabling 2FA.</li>
                                    <li><strong>LoginWithTwoFactorAuthentication or 4:</strong>  The step where the user needs to enter their code for 2FA.</li>
                                </ul>
                                <p>For creating and updating accounts, it's possible to loop through all the fields that you return from your query by using '{repeat:fields}{/repeat:fields}' and adding the HTML for a single field between those replacements.</p>
                                <p>For SubAccountsManagement, you can loop through all sub accounts by using '{repeat:subAccounts}{/repeat:subAccounts}' and through all fields of an account by using '{repeat:fields}{/repeat:fields}', and adding the HTML for a single field between those replacements. You can also use the variable '{selectedSubAccount}' if you need to do something with the ID of the selected sub account, or the variable '{amountOfSubAccounts}' if you need to check how many sub accounts the user has..</p>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 10
        )]
        public string Template { get; set; }

        /// <summary>
        /// The HTML template that will be shown after a successful action.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template success",
            Description = "The HTML template that will be shown after a successful action.",
            DeveloperRemarks = @"<p>When using one of the login modes, this template will be used if someone is already logged in. So you can add a logout link in this template.</p>
                                <p>You can use the variable '{logoutUrl}' for adding a logout link. The variable '{sentActivationMail}' will contain 'true' if the user attempted to login with an account that hasn't been activated yet, then an e-mail will be sent to the user to activate it. You can also use any values from the 'MainQuery' as variables in here.</p>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 20
        )]
        public string TemplateSuccess { get; set; }

        /// <summary>
        /// The HTML template that will be shown after a failed action.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template error",
            Description = "The HTML template that will be shown after a failed action.",
            DeveloperRemarks = @"<p>You can use the variable '{errorType}' to check which error was thrown. We have the following error types available, based on the chosen component mode:</p>
                                <h2>All modes:</h2>
                                <ul>
                                    <li><strong>Server:</strong> This is any exception that occurred on the server. The actual exception will be written to the trace.</li>
                                </ul>
                                <h2>LoginSingleStep:</h2>
                                <ul>
                                    <li>InvalidUsernameOrPassword</li>
                                    <li>TooManyAttempts</li>
                                    <li>UserNotActivated</li>
                                    <li>InvalidValidationToken</li>
                                    <li>InvalidUserId</li>
                                </ul>
                                <h2>LoginMultipleSteps:</h2>
                                <ul>
                                    <li>UserDoesNotExist</li>
                                    <li>InvalidPassword</li>
                                    <li>TooManyAttempts</li>
                                    <li>UserNotActivated</li>
                                </ul>
                                <p>ResetPassword:</p>
                                <ul>
                                    <li>InvalidTokenOrUser</li>
                                    <li>PasswordsNotTheSame</li>
                                    <li>EmptyPassword</li>
                                    <li>PasswordNotSecure</li> 
                                    <li>OldPasswordInvalid</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 30
        )]
        public string TemplateError { get; set; }

        /// <summary>
        /// If this component requires any javascript, you can write that here.
        /// </summary>
        [CmsProperty(
            PrettyName = "Template JavaScript",
            Description = "If this component requires any javascript, you can write that here.",
            DeveloperRemarks = "This javascript will always be added, whether an action was successful or not.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.JsEditor,
            DisplayOrder = 40
        )]
        public string TemplateJavaScript { get; set; }

        #endregion

        #region E-mail properties

        /// <summary>
        /// The subject for the e-mail that will be sent to the user when they request a new password.
        /// </summary>
        [CmsProperty(
            PrettyName = "Subject for reset password email",
            Description = "The subject for the e-mail that will be sent to the user when they request a new password.",
            DeveloperRemarks = "If your query of 'QueryPasswordForgottenEmail' returns a subject, then this property will be ignored.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            DisplayOrder = 10,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string SubjectResetPasswordEmail { get; set; }

        /// <summary>
        /// The HTML body for the e-mail that will be sent to the user when they request a new password. Make sure that you use the variable '{url}', which will be replaced by the URL to the reset password page.
        /// </summary>
        [CmsProperty(
            PrettyName = "Body for reset password email",
            Description = "The HTML body for the e-mail that will be sent to the user when they request a new password. Make sure that you use the variable '{url}', which will be replaced by the URL to the reset password page.",
            DeveloperRemarks = "If your query of 'QueryPasswordForgottenEmail' returns a body, then this property will be ignored.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 20,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string BodyResetPasswordEmail { get; set; }

        /// <summary>
        /// The query that returns the subject, body and sender for the password forgotten e-mail.
        /// </summary>
        [CmsProperty(
            PrettyName = "Query for password forgotten email",
            Description = "The query that returns the subject, body and sender for the password forgotten e-mail.",
            DeveloperRemarks = @"<p>For this to work, you need to make sure that the query returns the following columns: subject, body, senderName and senderEmail.</p>
                                <p>You can use the variable '?userId' or '{userId}' if you want to get a template that is based on the user.</p>
                                <p>Make sure your body contains the variable '{url}', otherwise the user will have no way to actually reset their password.</p>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 30,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string QueryPasswordForgottenEmail { get; set; }

        /// <summary>
        /// The query that returns the subject, body and sender for the notification e-mail.
        /// </summary>
        [CmsProperty(
            PrettyName = "Query notification email",
            Description = "The query that returns the subject, body and sender for the notification e-mail.",
            DeveloperRemarks = @"<p>For this to work, you need to make sure that the query returns the following columns: subject, body, senderName and senderEmail.</p>
                                 <p>You can use the variable '?userId' or '{userId}' if you want to get a template that is based on the user.</p>",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 70,
            ComponentMode = "CreateOrUpdateAccount"
        )]
        public string QueryNotificationEmail { get; set; }

        /// <summary>
        /// The e-mail address(es) that should receive notifications when a new account is created. You can add multiple e-mail addresses by separating them by a semicolon (;).
        /// </summary>
        [CmsProperty(
            PrettyName = "Notifications receiver",
            Description = "The e-mail address(es) that should receive notifications when a new account is created. You can add multiple e-mail addresses by separating them by a semicolon (;).",
            DeveloperRemarks = "Make sure you also enable the option 'SendNotificationsForNewAccounts' to use this.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            DisplayOrder = 80,
            ComponentMode = "CreateOrUpdateAccount"
        )]
        public string NotificationsReceiver { get; set; }

        /// <summary>
        /// BCC e-mail address for notifications of new accounts, in case the website owners also want to see when a new account is created.
        /// </summary>
        [CmsProperty(
            PrettyName = "Notifications BCC",
            Description = "BCC e-mail address for notifications of new accounts, in case the website owners also want to see when a new account is created.",
            DeveloperRemarks = "Make sure you also enable the option 'SendNotificationsForNewAccounts' to use this.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            DisplayOrder = 90,
            ComponentMode = "CreateOrUpdateAccount"
        )]
        public string NotificationsBcc { get; set; }

        /// <summary>
        /// The subject for the e-mail that will be sent to admins when someone creates a new account on the website.
        /// </summary>
        [CmsProperty(
            PrettyName = "Subject for new account notification email",
            Description = "The subject for the e-mail that will be sent to admins when someone creates a new account on the website.",
            DeveloperRemarks = "You can use any values that are in the form for creating an account and '{userId}' as replacement.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            DisplayOrder = 100,
            ComponentMode = "CreateOrUpdateAccount"
        )]
        public string SubjectNewAccountNotificationEmail { get; set; }

        /// <summary>
        /// The HTML body for the e-mail that will be sent to admins when someone creates a new account on the website.
        /// </summary>
        [CmsProperty(
            PrettyName = "Body for new account notification email",
            Description = "The HTML body for the e-mail that will be sent to admins when someone creates a new account on the website.",
            DeveloperRemarks = "You can use any values that are in the form for creating an account and '{userId}' as replacement.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.MailTemplate,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 110,
            ComponentMode = "CreateOrUpdateAccount"
        )]
        public string BodyNewAccountNotificationEmail { get; set; }

        #endregion

        #region Tab datasource properties

        /// <summary>
        /// The Wiser 2.0 entity type that is used for users that need to be able to login on the website.
        /// </summary>
        [CmsProperty(
            PrettyName = "Entity type",
            Description = "The Wiser 2.0 entity type that is used for users that need to be able to login on the website.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10
        )]
        public string EntityType { get; set; }

        /// <summary>
        /// The field/property name that contains the value users login with, such as e-mail address or username. This should be the same in the HTML as in wiser_entityproperty.
        /// </summary>
        [CmsProperty(
            PrettyName = "Login field name",
            Description = "The field/property name that contains the value users login with, such as e-mail address or username. This should be the same in the HTML as in wiser_entityproperty.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 20
        )]
        public string LoginFieldName { get; set; }

        /// <summary>
        /// The Wiser 2.0 field/property name that contains the SHA512 hashed password of the user. This should be the same in the HTML as in wiser_entityproperty.
        /// </summary>
        [CmsProperty(
            PrettyName = "Password field name",
            Description = "The Wiser 2.0 field/property name that contains the SHA512 hashed password of the user. This should be the same in the HTML as in wiser_entityproperty.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 30
        )]
        public string PasswordFieldName { get; set; }

        /// <summary>
        /// The Wiser 2.0 field/property name that should be used to remember the amount of failed login attempts of a user.
        /// </summary>
        [CmsProperty(
            PrettyName = "Failed login attempts field name",
            Description = "The Wiser 2.0 field/property name that should be used to remember the amount of failed login attempts of a user.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 40,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public string FailedLoginAttemptsFieldName { get; set; }

        /// <summary>
        /// The Wiser 2.0 field/property name that should be used to remember the date and time of the user's last login attempt.
        /// </summary>
        [CmsProperty(
            PrettyName = "Last login attempt field name",
            Description = "The Wiser 2.0 field/property name that should be used to remember the date and time of the user's last login attempt.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 50,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public string LastLoginAttemptFieldName { get; set; }

        /// <summary>
        /// The Wiser 2.0 field/property name where the user's e-mail address is saved. This can be the same as 'LoginFieldName', if you use e-mail address for logging in.
        /// </summary>
        [CmsProperty(
            PrettyName = "Email address field name",
            Description = "The Wiser 2.0 field/property name where the user's e-mail address is saved. This can be the same as 'LoginFieldName', if you use e-mail address for logging in.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 60
        )]
        public string EmailAddressFieldName { get; set; }

        /// <summary>
        /// The Wiser 2.0 field/property name that should be used to save the date and time that the reset token will expire.
        /// </summary>
        [CmsProperty(
            PrettyName = "Reset password expire date field name",
            Description = "The Wiser 2.0 field/property name that should be used to save the date and time that the reset token will expire.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 80,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string ResetPasswordTokenFieldName { get; set; }

        /// <summary>
        /// The Wiser 2.0 field/property name that should be used to save the date and time that the reset token will expire.
        /// </summary>
        [CmsProperty(
            PrettyName = "Reset password expire date field name",
            Description = "The Wiser 2.0 field/property name that should be used to save the date and time that the reset token will expire.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 80,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string ResetPasswordExpireDateFieldName { get; set; }

        /// <summary>
        /// The Wiser 2.0 field/property name that contains the role of the user.
        /// </summary>
        [CmsProperty(
            PrettyName = "Role field name",
            Description = "The Wiser 2.0 field/property name that contains the role of the user.",
            DeveloperRemarks = "Leave empty if you don't use roles.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 85
        )]
        public string RoleFieldName { get; set; }

        /// <summary>
        /// The name of the field where the user has to enter their password new password, when they want to change it.
        /// </summary>
        [CmsProperty(
            PrettyName = "New password field name",
            Description = "The name of the field where the user has to enter their password new password, when they want to change it.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 90
        )]
        public string NewPasswordFieldName { get; set; }

        /// <summary>
        /// The name of the field where the user has to enter their new password a second time, for confirmation, in places where they can change their password.
        /// </summary>
        [CmsProperty(
            PrettyName = "New password confirmation field name",
            Description = "The name of the field where the user has to enter their new password a second time, for confirmation, in places where they can change their password.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 100
        )]
        public string NewPasswordConfirmationFieldName { get; set; }

        /// <summary>
        /// The name of the Wiser 2.0 property/field where the Client ID for Google Analytics should be saved.
        /// </summary>
        [CmsProperty(
            PrettyName = "Google client ID field name",
            Description = "The name of the Wiser 2.0 property/field where the Client ID for Google Analytics should be saved.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 100
        )]
        public string GoogleClientIdFieldName { get; set; }

        /// <summary>
        /// The entity type for sub accounts
        /// </summary>
        [CmsProperty(
            PrettyName = "Sub account entity type",
            Description = "The entity type for sub accounts",
            DeveloperRemarks = "Only applicable for websites that use sub accounts.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 10
        )]
        public string SubAccountEntityType { get; set; }

        /// <summary>
        /// The type number that should be used for linking a sub account to a main account.
        /// </summary>
        [CmsProperty(
            PrettyName = "Sub account link type number",
            Description = "The type number that should be used for linking a sub account to a main account.",
            DeveloperRemarks = "Only applicable for websites that use sub accounts.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 20
        )]
        public int SubAccountLinkTypeNumber { get; set; }

        /// <summary>
        /// The data table name for the session variables for OCI cXML punch out to store in.
        /// </summary>
        [CmsProperty(
            PrettyName = "Punch-Out session store table",
            Description = "The data table name for the session variables for OCI cXML punch out to store in.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomDatabase,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextField,
            DisplayOrder = 10,
            ComponentMode = "CXmlPunchOutLogin,CXmlPunchOutContinueSession"
        )]
        public string PunchOutSessionTable { get; set; }

        /// <summary>
        /// The query string parameter name where the session token will be stored in.
        /// </summary>
        [CmsProperty(
            PrettyName = "Punch-Out session query string parameter",
            Description = "The query string parameter name where the session token will be stored in.",
            DeveloperRemarks = "For OCI.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomDatabase,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextField,
            DisplayOrder = 20,
            ComponentMode = "CXmlPunchOutLogin,CXmlPunchOutContinueSession"
        )]
        public string PunchOutSessionQueryStringParameterName { get; set; }

        /// <summary>
        /// The main query for the selected component mode.
        /// </summary>
        [CmsProperty(
            PrettyName = "Main query",
            Description = "The main query for the selected component mode.",
            DeveloperRemarks = @"<p>You can use the variable '?userId' or '{userId}' if you need the ID of the logged in user in the query.</p>
                                <p>For all login modes, this is the query that gets user information to show in the success template. </p>
                                <p>For reset password mode, this needs to return the columns 'id', so we can verify that the e-mail address exists. You can use the following variables in this query, for mode ResetPassword:</p>
                                <ul>
                                    <li><strong>?email or {email}:</strong> Required: The e-mail address that the user entered in the reset password form.</li>
                                    <li><strong>?emailAddressFieldName or {emailAddressFieldName}:</strong> Optional: The Wiser 2.0 field name where the e-mail address is stored.</li>
                                    <li><strong>?emailAddress or {emailAddress}:</strong> Required: The e-mail address that the user entered.</li>
                                    <li><strong>?entityType or {entityType}:</strong> Optional: The Wiser 2.0 entity type that is used for users that login on the website.</li>
                                </ul>
                                <p>For CreateOrUpdateAccount, if the query returns one row for each field/property and at least a column with the name 'property_name', it will be secured so that nobody can save custom values. If your query doesn't contain a column with that name, there will be no security, unless you also write a custom create and update query.</p>
                                <p>You can use all data from this query in the main template and you can use the following variables in this query:</p>
                                <ul>
                                    <li><strong>{loginFieldName} or ?loginFieldName:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{passwordFieldName} or ?passwordFieldName:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{emailAddressFieldName} or ?emailAddressFieldName:</strong> The Wiser 2.0 field name where the e-mail address is stored.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The value of the property in this component with the same name.</li>
                                </ul>
                                <p>For SubAccountsManagement this query should get the list of sub accounts for the logged in user. Please make sure you only get items that are of the correct entity type.</p>
                                <p> This query should return a column 'id' and 'name' for each sub account and optionally any other values that you want to show on the page. You can use the following variables in this query:</p>
                                <ul>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the logged in user.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{subAccountEntityType} or ?subAccountEntityType:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{subAccountLinkTypeNumber} or ?subAccountLinkTypeNumber:</strong> The value of the property in this component with the same name.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 10
        )]
        public string MainQuery { get; set; }

        /// <summary>
        /// The query that gets the hashed password and login, to check whether a login is successful.
        /// </summary>
        [CmsProperty(
            PrettyName = "Login Query",
            Description = "The query that gets the ID and hashed password of the user that is trying to login, to check if it can login.",
            DeveloperRemarks = @"<p>Please make sure to only check for items with the correct entity type and username/e-mail address.</p>
                                <p>You can use any fields in the query that you add to the HTML.</p>
                                <p>Also make sure the query returns at least the following columns:</p>
                                <ul>
                                    <li><strong>id:</strong> The item ID of the user in wiser_item.</li>
                                    <li><strong>password:</strong> The password should contain a SHA512 hash with salt, generated by the GCL.</li>
                                    <li><strong>email:</strong> The e-mail address of the user. This is only required if the mode is 'LoginMultipleSteps', so that we can send an activation link to the user if they're not activated yet.</li>
                                    <li><strong>failedLoginAttempts:</strong> The amount of times this user has failed to login.</li>
                                    <li><strong>lastLoginAttempt:</strong> The date and time when the user last had a failed login attempt.</li>
                                    <li><strong>mainAccountId:</strong> The ID of the main account, if the user is logging in with a sub account. This value should be 0 or the same as the user ID, if the user is logging in as a main account.</li>
                                    <li><strong>role:</strong> Optional: The role of the user. Only applicable when you do anything with roles.</li>
                                </ul>
                                <p>You can also use the following default variables (but they are not mandatory to use, if you use a custom query):<p>
                                <ul>
                                    <li><strong>{loginFieldName} or ?loginFieldName:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{passwordFieldName} or ?passwordFieldName:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{emailAddressFieldName} or ?emailAddressFieldName:</strong> The Wiser 2.0 field name where the e-mail address is stored.</li>
                                    <li><strong>{roleFieldName} or ?roleFieldName:</strong> The Wiser 2.0 field name where the role of the user is stored.</li>
                                    <li><strong>{login} or ?login:</strong> The value that the user entered in the HTML field with the same name as the value in the property 'loginFieldName'.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{subAccountEntityType} or ?subAccountEntityType:</strong> The value of the property in this component with the same name.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 20,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword,CreateOrUpdateAccount"
        )]
        public string LoginQuery { get; set; }

        /// <summary>
        /// The query for saving a login attempt, whether is was successful or not.
        /// </summary>
        [CmsProperty(
            PrettyName = "Save login attempt query",
            Description = "The query for saving a login attempt, whether is was successful or not.",
            DeveloperRemarks = @"<p>You can also use the following default variables (but they are not mandatory to use, except for userId, if you use a custom query):<p>
                                <ul>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the user.</li>
                                    <li><strong>{success} or ?success:</strong> This will contain TRUE if the login attempt was successful, or FALSE if it wasn't.</li>
                                    <li><strong>{failedLoginAttemptsFieldName} or ?failedLoginAttemptsFieldName:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{lastLoginAttemptFieldName} or ?lastLoginAttemptFieldName:</strong> The value of the property in this component with the same name.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 30,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public string SaveLoginAttemptQuery { get; set; }

        /// <summary>
        /// The query for saving information for resetting a user's password.
        /// </summary>
        [CmsProperty(
            PrettyName = "Save reset password values query",
            Description = "The query for saving information for resetting a user's password.",
            DeveloperRemarks = @"<p>You can also use the following default variables in your query:<p>
                                <ul>
                                    <li><strong>{userId} or ?userId:</strong> Required: The ID of the user.</li>
                                    <li><strong>{resetPasswordToken} or ?resetPasswordToken:</strong> Required: The newly generated token that will be sent in the e-mail to the user.</li>
                                    <li><strong>{resetPasswordTokenFieldName} or ?resetPasswordTokenFieldName:</strong> Optional: The Wiser 2.0 field/property name for the above value.</li>
                                    <li><strong>{resetPasswordExpireDate} or ?resetPasswordExpireDate:</strong> Required: The date and time that the reset token will expire.</li>
                                    <li><strong>{resetPasswordExpireDateFieldName} or ?resetPasswordExpireDateFieldName:</strong> Optional: The Wiser 2.0 field/property name for the above value.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 40,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string SaveResetPasswordValuesQuery { get; set; }

        /// <summary>
        /// The query for validating the reset password token of a user.
        /// </summary>
        [CmsProperty(
            PrettyName = "Validate reset password token query",
            Description = "The query for validating the reset password token of a user.",
            DeveloperRemarks = @"<p>The query should return the column 'login' with the username or e-mail address (whichever you use for logging in users). It should return a row if the token is valid or nothing if it's not valid.</p>
                                <p>You can also use the following default variables:<p>
                                <ul>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the user.</li>
                                    <li><strong>{token} or ?token:</strong> The value of the token in the query string.</li>
                                    <li><strong>{resetPasswordTokenFieldName} or ?resetPasswordTokenFieldName:</strong> The Wiser 2.0 property name that is used to save the reset password token.</li>
                                    <li><strong>{resetPasswordExpireDateFieldName} or ?resetPasswordExpireDateFieldName:</strong> The Wiser 2.0 property name that is used to save the expire date of the reset password token.</li>
                                    <li><strong>{loginFieldName} or ?loginFieldName:</strong> Optional: The Wiser 2.0 field name where the value is stored that the user logins with (ie username or e-mail).</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The entity type that is set in this component, in the property 'EntityType'.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 50,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string ValidateResetPasswordTokenQuery { get; set; }

        /// <summary>
        /// The query for changing the user's password.
        /// </summary>
        [CmsProperty(
            PrettyName = "Change password query",
            Description = "The query for changing the user's password",
            DeveloperRemarks = @"<p>Make sure that this query updates the password and also that it clears the reset password token, so that the reset password link will be invalidated.</p>
                                <p>You can also use the following default variables:<p>
                                <ul>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the user.</li>
                                    <li><strong>{newPasswordHash} or ?newPasswordHash:</strong> The SHA512 hash of the new password.</li>
                                    <li><strong>{passwordFieldName} or ?passwordFieldName:</strong> The Wiser 2.0 property name that is used to save the password of the user.</li>
                                    <li><strong>{resetPasswordTokenFieldName} or ?resetPasswordTokenFieldName:</strong> The Wiser 2.0 property name that is used to save the reset password token.</li>
                                    <li><strong>{resetPasswordExpireDateFieldName} or ?resetPasswordExpireDateFieldName:</strong> The Wiser 2.0 property name that is used to save the expire date of the reset password token.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 55,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string ChangePasswordQuery { get; set; }

        /// <summary>
        /// The query for getting the user ID from an e-mail address.
        /// </summary>
        [CmsProperty(
            PrettyName = "Get user id via email address query",
            Description = "The query for getting the user ID from an e-mail address",
            DeveloperRemarks = @"<p>Make sure that this query returns a column named 'id', which contains the ID of the user, or no results if the user doesn't exist.</p>
                                <p>You can also use the following default variables:<p>
                                <ul>
                                    <li><strong>{emailAddressFieldName} or ?emailAddressFieldName:</strong> The Wiser 2.0 property name that is used to save the e-mail address of the user.</li>
                                    <li><strong>{emailAddress} or ?emailAddress:</strong> The e-mail address that the user entered.</li>
                                    <li><strong>{emailAddressGclAesEncrypted} or ?emailAddressGclAesEncrypted:</strong> The e-mail address that the user entered, encrypted using the AESEncode function.</li>
                                    <li><strong>{emailAddressAesEncrypted} or ?emailAddressAesEncrypted:</strong> The e-mail address that the user entered, encrypted using the EncryptWithAes function.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The Wiser 2.0 entity type that is used for users that can login on the website.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 60,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string GetUserIdViaEmailAddressQuery { get; set; }

        /// <summary>
        /// The query for checking whether an account already exists, while creating or updating an account.
        /// </summary>
        [CmsProperty(
            PrettyName = "Check if account exists query",
            Description = "The query for checking whether an account already exists, while creating or updating an account.",
            DeveloperRemarks =
                @"<p>Make sure that this query returns anything (this can be NULL, as long as it returns a row) if the user exists, or no results if the user doesn't exist. You can also return a custom error message in any column and use that column as variable in the TemplateError.</p>
                                <p>Also make sure that you exclude the logged in user in your check, like the default query does, so that they don't get an error when they change something in their account.</p>
                                <p>You can also use the following default variables:<p>
                                <ul>
                                    <li><strong>{emailAddressFieldName} or ?emailAddressFieldName:</strong> The Wiser 2.0 property name that is used to save the e-mail address of the user.</li>
                                    <li><strong>{emailAddress} or ?emailAddress:</strong> The e-mail address that the user entered.</li>
                                    <li><strong>{loginFieldName} or ?loginFieldName:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{login} or ?login:</strong> The value that the user entered in the HTML field with the same name as the value in the property 'loginFieldName'.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The Wiser 2.0 entity type that is used for users that can login on the website.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 70,
            ComponentMode = "CreateOrUpdateAccount,SubAccountsManagement"
        )]
        public string CheckIfAccountExistsQuery { get; set; }

        /// <summary>
        /// The query for getting the logged in user's password hash, so we can validate it. This is required when the user wants to change their password.
        /// </summary>
        [CmsProperty(
            PrettyName = "Validate password query",
            Description = "The query for getting the logged in user's password hash, so we can validate it. This is required when the user wants to change their password.",
            DeveloperRemarks = @"<p>Please make sure to only check for items with the correct entity type and user ID.</p>
                                <p>Also make sure the query returns at least the following columns:</p>
                                <ul>
                                    <li><strong>password:</strong> The password should contain a SHA512 hash with salt, generated by the GCL.</li>
                                </ul>
                                <p>You can also use the following default variables (but they are not mandatory to use, if you use a custom query):<p>
                                <ul>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the logged in user.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The value of the property in this component with the same name.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 80,
            ComponentMode = "CreateOrUpdateAccount"
        )]
        public string ValidatePasswordQuery { get; set; }

        /// <summary>
        /// The query for creating a new account.
        /// </summary>
        [CmsProperty(
            PrettyName = "Create account query",
            Description = "The query for creating a new (sub) account",
            DeveloperRemarks = @"<p>Make sure this query returns a column named 'id', which contains the ID of the newly created user. You can also use any value in the submitted form as a variable.<p>
                                <p>When the mode is set to 'SubAccountsManagement', this query will be used for creating a new sub account.</p>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 80,
            ComponentMode = "CreateOrUpdateAccount,SubAccountsManagement"
        )]
        public string CreateAccountQuery { get; set; }

        /// <summary>
        /// The query for updating a(n) (sub) account.
        /// </summary>
        [CmsProperty(
            PrettyName = "Update account query",
            Description = "The query for updating a(n) (sub) account",
            DeveloperRemarks = @"<p>You can also use any value in the submitted form as a variable and {userId} or ?userId for the ID of the logged in User.<p>
                                <p>When the mode is set to 'SubAccountsManagement', this query will be used for updating a sub account. <strong>Make sure that you check whether the sub account belongs to the logged in user!</strong></p>
                                <p>You can also use the following variables then:</p>
                                <ul>
                                    <li><strong>{subAccountEntityType} or ?subAccountEntityType:</strong> The entity type that is used for sub accounts.</li>
                                    <li><strong>{subAccountId} or ?subAccountId:</strong> The ID selected sub account. Will be 0 if no sub account has been selected.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The entity type that is used for sub accounts.</li>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the logged in user (the main account)</li>
                                    <li><strong>{subAccountLinkTypeNumber} or ?subAccountLinkTypeNumber:</strong> The value of the component property with the same name.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 90,
            ComponentMode = "CreateOrUpdateAccount,SubAccountsManagement"
        )]
        public string UpdateAccountQuery { get; set; }

        /// <summary>
        /// The query for updating a new account.
        /// </summary>
        [CmsProperty(
            PrettyName = "Delete account query",
            Description = "The query for deleting a(n) (sub) account",
            DeveloperRemarks = @"<p>You can use the variable {userId} or ?userId for the ID of the logged in User.<p>
                                <p>When the mode is set to 'SubAccountsManagement', this query will be used for updating a sub account. <strong>Make sure that you check whether the sub account belongs to the logged in user!</strong></p>
                                <p>You can also use the following variables then:</p>
                                <ul>
                                    <li><strong>{subAccountEntityType} or ?subAccountEntityType:</strong> The entity type that is used for sub accounts.</li>
                                    <li><strong>{subAccountId} or ?subAccountId:</strong> The ID selected sub account. Will be 0 if no sub account has been selected.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The entity type that is used for sub accounts.</li>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the logged in user (the main account)</li>
                                    <li><strong>{subAccountLinkTypeNumber} or ?subAccountLinkTypeNumber:</strong> The value of the component property with the same name.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 100,
            ComponentMode = "SubAccountsManagement"
        )]
        public string DeleteAccountQuery { get; set; }

        /// <summary>
        /// The query for inserting/updating a value for a Wiser 2.0 property/field. This query will be executed for every submitted field in the form, which was originally on the page.
        /// </summary>
        [CmsProperty(
            PrettyName = "Set value in Wiser entity property query",
            Description = "The query for inserting/updating a value for a Wiser 2.0 property/field. <strong>This query will be executed for every submitted field in the form that was originally on the page.</strong>",
            DeveloperRemarks = @"<p>Leave this property empty if you're not using the Wiser 2.0 data model, or if you're using a custom create/update query that already sets all values correctly.<p>
                                <p>You can use the following variables:</p>
                                <ul>
                                    <li><strong>{value} or ?value:</strong> The submitted value</li>
                                    <li><strong>{name} or ?name:</strong> The name of the property/field</li>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the logged in or newly created user</li>
                                    <li><strong>{subAccountId} or ?subAccountId:</strong> (When mode is 'SubAccountsManagement'.) The ID selected sub account. Will be 0 if no sub account has been selected.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 110,
            ComponentMode = "CreateOrUpdateAccount,SubAccountsManagement"
        )]
        public string SetValueInWiserEntityPropertyQuery { get; set; }

        /// <summary>
        /// The query for inserting/updating a value for a Wiser 2.0 property/field. This query will be executed for every submitted field in the form, which was originally on the page.
        /// </summary>
        [CmsProperty(
            PrettyName = "Get sub account query",
            Description =
                "The query for getting all values and fields for a sub account. If you use the default HTML template, this query should return a row for each field that needs to be shown on the page. If you use custom HTML, this query can return whatever you need in that HTML.",
            DeveloperRemarks = @"<p>You can use the following variables:</p>
                                <ul>
                                    <li><strong>{subAccountEntityType} or ?subAccountEntityType:</strong> The entity type that is used for sub accounts.</li>
                                    <li><strong>{subAccountId} or ?subAccountId:</strong> The ID selected sub account. Will be 0 if no sub account has been selected.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The entity type that is used for sub accounts.</li>
                                    <li><strong>{userId} or ?userId:</strong> The ID of the logged user</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.CustomSql,
            DisplayOrder = 120,
            ComponentMode = "SubAccountsManagement"
        )]
        public string GetSubAccountQuery { get; set; }

        /// <summary>
        /// The query that validates an ID for auto login via Wiser.
        /// </summary>
        [CmsProperty(
            PrettyName = "Auto login Query",
            Description = "The query that validates an ID for auto login via Wiser.",
            DeveloperRemarks = @"<p>Please make sure to only check for items with the correct entity type(s).</p>
                                <p>Also make sure the query returns at least the following columns:</p>
                                <ul>
                                    <li><strong>id:</strong> The item ID of the user in wiser_item.</li>
                                    <li><strong>mainAccountId:</strong> The ID of the main account, if the user is logging in with a sub account. This value should be 0 or the same as the user ID, if the user is logging in as a main account.</li>
                                    <li><strong>role:</strong> Optional: The role of the user. Only applicable when you do anything with roles.</li>
                                </ul>
                                <p>You can also use the following default variables (but they are not mandatory to use, if you use a custom query):<p>
                                <ul>
                                    <li><strong>{loginFieldName} or ?loginFieldName:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{passwordFieldName} or ?passwordFieldName:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{emailAddressFieldName} or ?emailAddressFieldName:</strong> The Wiser 2.0 field name where the e-mail address is stored.</li>
                                    <li><strong>{roleFieldName} or ?roleFieldName:</strong> The Wiser 2.0 field name where the role of the user is stored.</li>
                                    <li><strong>{entityType} or ?entityType:</strong> The value of the property in this component with the same name.</li>
                                    <li><strong>{subAccountEntityType} or ?subAccountEntityType:</strong> The value of the property in this component with the same name.</li>
                                </ul>",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Wiser,
            TextEditorType = CmsAttributes.CmsTextEditorType.QueryEditor,
            DisplayOrder = 10,
            ComponentMode = "LoginSingleStep"
        )]
        public string AutoLoginQuery { get; set; }

        #endregion

        #region Tab Behavior properties

        /// <summary>
        /// This is the amount of time the user will stay logged in on the website.
        /// </summary>
        [CmsProperty(
            PrettyName = "Amount of days to remember cookie",
            Description = "This is the amount of time the user will stay logged in on the website.",
            DeveloperRemarks = "Set to 0 if the cookie needs to be deleted when the user closes the browser.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public int? AmountOfDaysToRememberCookie { get; set; }

        /// <summary>
        /// The name of the remember me checkbox. Only enter a value if you want to add a checkbox for the user to indicate whether or not they want to stay logged in after closing the browser.
        /// </summary>
        [CmsProperty(
            PrettyName="Remember me checkbox name",
            Description="The name of the remember me checkbox. Only enter a value if you want to add a checkbox for the user to indicate whether or not they want to stay logged in after closing the browser.",
            DeveloperRemarks="You need to add the HTML for the checkbox yourself, to the main template. Example:<br>&lt;input type=&quot;checkbox&quot; name=&quot;rememberMe&quot; id=&quot;rememberMe&quot; value=&quot;0&quot; /&gt;<br>&lt;label for=&quot;rememberMe&quot;&gt;Stay logged in&lt;/label&gt;",
            TabName=CmsAttributes.CmsTabName.Behavior,
            GroupName=CmsAttributes.CmsGroupName.Basic,
            DisplayOrder=15,
            ComponentMode="LoginSingleStep,LoginMultipleSteps"
            )]
        public string RememberMeCheckboxName { get; set; }

        /// <summary>
        /// When the user exceeds this number, they will not be able to login for the amount of time set in <see cref="DefaultLockoutTime" />.
        /// Set to 0 to disable this functionality.
        /// </summary>
        [CmsProperty(
            PrettyName = "Maximum amount of failed login attempts",
            Description = "When the user exceeds this number, they will not be able to login for the amount of time set in 'DefaultLockoutTime'.",
            DeveloperRemarks = "Set to 0 to disable this functionality.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 20,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public int? MaximumAmountOfFailedLoginAttempts { get; set; }

        /// <summary>
        /// The amount of time (in minutes) that the user needs to wait before being able to attempt another login, after exceeding the maximum amount of failed login attempts.
        /// Set to 0 to disable this functionality.
        /// </summary>
        [CmsProperty(
            PrettyName = "Default lockout time",
            Description = "The amount of time (in minutes) that the user needs to wait before being able to attempt another login, after exceeding the maximum amount of failed login attempts.",
            DeveloperRemarks = "Set to 0 to disable this functionality.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 30,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public int? DefaultLockoutTime { get; set; }

        /// <summary>
        /// This is the amount of time the user will stay logged in on the website.
        /// </summary>
        [CmsProperty(
            PrettyName = "Reset password token validity",
            Description = "This is the amount of time (in days) a reset password token will stay valid, before the user has to request a new one.",
            DeveloperRemarks = "Set to 0 if the token should stay valid forever (until the user changed their password).",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 40,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public int? ResetPasswordTokenValidity { get; set; }

        /// <summary>
        /// The URL to the reset password page. Leave empty to use the current page.
        /// </summary>
        [CmsProperty(
            PrettyName = "Reset password url",
            Description = "The URL to the reset password page. Leave empty to use the current page.",
            DeveloperRemarks = @"<p>This will be used in the e-mail that the user received after indicating they want to reset their password.
                                This can be a relative url such as '/reset-password/' or an absolute URL such as 'https://google.com/reset-password/'.</p>
                                <p>The required query string parameters will be automatically added to this URL, you don't need to add place holders for them.
                                That URL will then be used to replace the variable '{url}' in the e-mail body.</p>",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 50,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword"
        )]
        public string ResetPasswordUrl { get; set; }

        /// <summary>
        /// After the user successfully did certain actions, such as resetting their password or creating an account, log them in automatically.
        /// </summary>
        [DefaultValue(true),
         CmsProperty(
             PrettyName = "Auto login user after action",
             Description = "After the user successfully did certain actions, such as resetting their password or creating an account, log them in automatically.",
             DeveloperRemarks = "",
             TabName = CmsAttributes.CmsTabName.Behavior,
             GroupName = CmsAttributes.CmsGroupName.Basic,
             DisplayOrder = 60,
             ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword,CreateOrUpdateAccount"
         )]
        public bool AutoLoginUserAfterAction { get; set; } = true;

        /// <summary>
        /// After the user successfully did certain actions, such as resetting their password or creating an account, redirect them to an URL.
        /// </summary>
        [CmsProperty(
            PrettyName = "Redirect user after action",
            Description = "After the user successfully did certain actions, such as resetting their password or creating an account, redirect them to an URL.",
            DeveloperRemarks = "Leave empty for no redirect. The redirect will only happen if the action was successful.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 65,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps,ResetPassword,CreateOrUpdateAccount"
        )]
        public string RedirectAfterAction { get; set; }

        /// <summary>
        /// If the user leaves a field empty when submitting it, ignore it so that empty values will never be saved.
        /// </summary>
        [DefaultValue(true),
         CmsProperty(
             PrettyName = "Ignore empty values",
             Description = "If the user leaves a field empty when submitting it, ignore it so that empty values will never be saved.",
             DeveloperRemarks = "",
             TabName = CmsAttributes.CmsTabName.Behavior,
             GroupName = CmsAttributes.CmsGroupName.Basic,
             DisplayOrder = 70,
             ComponentMode = "CreateOrUpdateAccount"
         )]
        public bool IgnoreEmptyValues { get; set; } = true;

        /// <summary>
        /// If this s set to true, the user must enter their current password to be allowed to change their login.
        /// </summary>
        [DefaultValue(true),
         CmsProperty(
             PrettyName = "Require current password for changing login",
             Description = "If this s set to true, the user must enter their current password to be allowed to change their login.",
             DeveloperRemarks = "",
             TabName = CmsAttributes.CmsTabName.Behavior,
             GroupName = CmsAttributes.CmsGroupName.Basic,
             DisplayOrder = 80,
             ComponentMode = "CreateOrUpdateAccount"
         )]
        public bool RequireCurrentPasswordForChangingLogin { get; set; } = true;
        
        /// <summary>
        /// If this is set to true, an user can be created without a password. This should only be used in a checkout process, so the user can place an order without creating an account.
        /// </summary>
        [DefaultValue(true),
         CmsProperty(
             PrettyName = "Allow empty password",
             Description = "If this is set to true, an user can be created without a password. This should only be used in a checkout process, so the user can place an order without creating an account.",
             DeveloperRemarks = "",
             TabName = CmsAttributes.CmsTabName.Behavior,
             GroupName = CmsAttributes.CmsGroupName.Basic,
             DisplayOrder = 90,
             ComponentMode = "CreateOrUpdateAccount"
         )]
        public bool RequireCurrentPasswordForChangingPassword { get; set; } = true;

        /// <summary>
        /// If this is set to true, an user can be created without a password. This should only be used in a checkout process, so the user can place an order without creating an account.
        /// </summary>
        [CmsProperty(
            PrettyName="Allow empty password",
            Description="If this is set to true, an user can be created without a password. This should only be used in a checkout process, so the user can place an order without creating an account.",
            DeveloperRemarks="",
            TabName=CmsAttributes.CmsTabName.Behavior,
            GroupName=CmsAttributes.CmsGroupName.Basic,
            DisplayOrder=95,
            ComponentMode="CreateOrUpdateAccount"
        )]
        public bool AllowEmptyPassword { get;set; }

        /// <summary>
        /// If this s set to true, a notification will be sent to the e-mail address(es) entered in 'NotificationsReceiver' whenever someone creates a new account via this component.
        /// </summary>
        [CmsProperty(
            PrettyName = "Send notifications for new accounts",
            Description = "If this s set to true, a notification will be sent to the e-mail address(es) entered in 'NotificationsReceiver' whenever someone creates a new account via this component.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 100,
            ComponentMode = "CreateOrUpdateAccount"
        )]
        public bool SendNotificationsForNewAccounts { get; set; }

        /// <summary>
        /// Enable this option to force users to login with 2FA, by using Google Authenticator.
        /// </summary>
        [CmsProperty(
            PrettyName = "Enable Google authenticator",
            Description = "Enable this option to force users to login with 2FA, by using Google Authenticator.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 10,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public bool EnableGoogleAuthenticator { get; set; }

        /// <summary>
        /// Enable this option to force users to login with 2FA, by using Google Authenticator.
        /// </summary>
        [CmsProperty(
            PrettyName = "Google authenticator site id",
            Description = "If the option 'Enable Google authenticator' is enabled, you need to enter a value here to use as the site ID for Google Authenticator.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 20,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public string GoogleAuthenticatorSiteId { get; set; }

        /// <summary>
        /// Enable users to login via an OCI environment.
        /// </summary>
        [CmsProperty(
            PrettyName = "Enable OCI login",
            Description = "Enable users to login via an OCI environment.",
            DeveloperRemarks = "Only enable this option if the customer actually wants this!",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 30,
            ComponentMode = "LoginSingleStep"
        )]
        public bool EnableOciLogin { get; set; }

        /// <summary>
        /// The key in the query string or POST variable that will contain the username of the user that is logging in via OCI.
        /// </summary>
        [CmsProperty(
            PrettyName = "OCI username key",
            Description = "The key in the query string or POST variable that will contain the username of the user that is logging in via OCI.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 40,
            ComponentMode = "LoginSingleStep,CXmlPunchOutLogin"
        )]
        public string OciUsernameKey { get; set; }

        /// <summary>
        /// The key in the query string or POST variable that will contain the password of the user that is logging in via OCI.
        /// </summary>
        [CmsProperty(
            PrettyName = "OCI password key",
            Description = "The key in the query string or POST variable that will contain the password of the user that is logging in via OCI.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 50,
            ComponentMode = "LoginSingleStep,CXmlPunchOutLogin"
        )]
        public string OciPasswordKey { get; set; }

        /// <summary>
        /// The key in the query string or POST variable that will contain the hook URL for OCI.
        /// </summary>
        [CmsProperty(
            PrettyName = "OCI hook URL key",
            Description = "The key in the query string or POST variable that will contain the hook URL for OCI.",
            DeveloperRemarks = "The hook URL is the URL that we need to send the order too when the user places an order.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Common,
            DisplayOrder = 60,
            ComponentMode = "LoginSingleStep,CXmlPunchOutLogin"
        )]
        public string OciHookUrlKey { get; set; }

        /// <summary>
        /// Enable users to login via Wiser.
        /// </summary>
        [CmsProperty(
            PrettyName = "Enable Wiser login",
            Description = "Enable users to login via Wiser.",
            DeveloperRemarks = "Only enable this option if the customer actually wants this!",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Wiser,
            DisplayOrder = 10,
            ComponentMode = "LoginSingleStep"
        )]
        public bool EnableWiserLogin { get; set; }

        /// <summary>
        /// The key in the query string or POST variable that will contain the encrypted ID of the user, for logging in via Wiser.
        /// </summary>
        [CmsProperty(
            PrettyName = "Wiser login user ID key",
            Description = "The key in the query string or POST variable that will contain the encrypted ID of the user, for logging in via Wiser.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Wiser,
            DisplayOrder = 20,
            ComponentMode = "LoginSingleStep"
        )]
        public string WiserLoginUserIdKey { get; set; }

        /// <summary>
        /// The key in the query string or POST variable that will contain the validation token, for logging in via Wiser.
        /// </summary>
        [CmsProperty(
            PrettyName = "Wiser login token key",
            Description = "The key in the query string or POST variable that will contain the validation token, for logging in via Wiser.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Wiser,
            DisplayOrder = 30,
            ComponentMode = "LoginSingleStep"
        )]
        public string WiserLoginTokenKey { get; set; }

        /// <summary>
        /// The validation token, for logging in via Wiser. The same token should be used in Wiser, in the button for logging in as that user.
        /// </summary>
        [CmsProperty(
            PrettyName = "Wiser login token",
            Description = "The validation token, for logging in via Wiser. The same token should be used in Wiser, in the button for logging in as that user.",
            DeveloperRemarks = "Please generate a unique token for every Happy Horizon customer. The longer this string is, the better (but it still needs to fit in an URL). Use passwordgenerator.net or something similar to generate your token.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Wiser,
            DisplayOrder = 40,
            ComponentMode = "LoginSingleStep"
        )]
        public string WiserLoginToken { get; set; }

        /// <summary>
        /// A regex for validating the password of the user. This will be done server side and optionally client side too.
        /// </summary>
        [CmsProperty(
            PrettyName = "Password validation regex",
            Description = "A regex for validating the password of the user. This will be done server side and optionally client side too.",
            DeveloperRemarks = @"If you want to use this for client side validation in your custom HTML, you can add the property 'pattern=""{PasswordValidationRegex}""' to your password field(s).",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Advanced,
            DisplayOrder = 10,
            ComponentMode = "ResetPassword,CreateOrUpdateAccount,SubAccountsManagement"
        )]
        public string PasswordValidationRegex { get; set; }

        #endregion

        #region Tab Developer properties

        /// <summary>
        /// You can enter a comma separated list of cookie names to delete after the user logs out.
        /// </summary>
        [CmsProperty(
            PrettyName = "Cookies to delete after logout",
            Description = "You can enter a comma separated list of cookie names to delete after the user logs out.",
            DeveloperRemarks = "The cookie that is used in this component will always be deleted, not matter what you enter here.",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.Advanced,
            DisplayOrder = 10,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public string CookiesToDeleteAfterLogout { get; set; }

        /// <summary>
        /// You can enter a comma separated list of session keys to delete after the user logs out.
        /// </summary>
        [CmsProperty(
            PrettyName = "Session keys delete after logout",
            Description = "You can enter a comma separated list of session keys to delete after the user logs out.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.Developer,
            GroupName = CmsAttributes.CmsGroupName.Advanced,
            DisplayOrder = 20,
            ComponentMode = "LoginSingleStep,LoginMultipleSteps"
        )]
        public string SessionKeysToDeleteAfterLogout { get; set; }

        #endregion
    }
}