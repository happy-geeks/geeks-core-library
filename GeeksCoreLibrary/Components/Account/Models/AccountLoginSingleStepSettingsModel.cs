using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Account.Models
{
    /// <summary>
    /// Model with all default settings for the account component with mode LoginSingleStep.
    /// </summary>
    internal class AccountLoginSingleStepSettingsModel
    {
        [DefaultValue(Constants.CookieName)]
        internal string CookieName { get; set; }
        
        [DefaultValue(@"<!-- There must always be a element with ID GclAccountContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id='GclAccountContainer{contentId}'>
    <jform id='GclLoginForm{contentId}' method='POST'>
        <div class='formPanel'>
            <h2>Inloggen</h2>
            {error}
            [if({stepNumber}=1)]
            <div class='formRow'>
                <label for='email{contentId}'>E-mailadres</label>
                <input type='email' name='{emailAddressFieldName}' id='email{contentId}'>
            </div>
            <div class='formRow passwordRow'>
                <label for='password{contentId}'>Wachtwoord</label>
                <input type='password' name='{passwordFieldName}' class='password' id='password{contentId}'>
                <ins class='icon-eye-show'></ins>
            </div>
            [endif]
            [if({stepNumber}=3)]
            <div class='formRow'>
                <img class='qrCode' alt='AuthenticationCode' src='{googleAuthenticationQrImageUrl}'>
            </div>
            <div class='formRow passwordRow'>
                <label for='googleAuthenticationPin{contentId}'>Scan de QR code met de google authenticator app en toets de code in</label>
                <input type='password' name='googleAuthenticationPin' id='googleAuthenticationPin{contentId}' class='password'>
                <ins class='icon-eye-show'></ins>
                <input type='hidden' name='googleAuthenticationVerificationId' value='{googleAuthenticationVerificationId}' />
            </div>
            [endif]
            [if({stepNumber}=4)]
            <div class='formRow passwordRow'>
                <label for='googleAuthenticationPin{contentId}'>Toets uw pincode in</label>
                <input type='password' name='googleAuthenticationPin' id='googleAuthenticationPin{contentId}' class='password'>
                <ins class='icon-eye-show'></ins>
                <input type='hidden' name='googleAuthenticationVerificationId' value='{googleAuthenticationVerificationId}' />
            </div>
            [endif]
            <div class='formRow'>
                <button type='submit' class='btn btnSend'>Inloggen</button>
            </div>
            [if({stepNumber}=1)]
            <div class='formRow center'>
                <a href='#' id='GclResetPasswordLink{contentId}'>Wachtwoord vergeten?</a>
            </div>
            [endif]
        </div>

        [if({stepNumber}=1)]
        <div class='formPanel'>
            <h2><span>Nieuw bij {siteName}?</span></h2>
            
            <div class='formRow'>
                <button type='button' class='btn btnBack'>Maak een account</button>
            </div>
        </div>
        [endif]
    </jform>
    [if({stepNumber}=1)]
    <!-- TODO Still in development.
    <jform id='GclExternalLoginForm{contentId}' method='POST'>
        <button type='submit' name='externalLogin' value='Google'>Inloggen via Google</button>
    </jform>
    -->
    [endif]
</div>")]
        internal string Template { get; }

        [DefaultValue(Constants.DefaultLoginSuccessTemplate)]
        internal string TemplateSuccess { get; }

        [DefaultValue(Constants.DefaultLoginErrorTemplate)]
        internal string TemplateError { get; }

        [DefaultValue(Constants.DefaultLoginJavascript)]
        internal string TemplateJavaScript { get; }

        [DefaultValue(Constants.DefaultResetPasswordSubject)]
        internal string SubjectResetPasswordEmail { get; }

        [DefaultValue(Constants.DefaultResetPasswordMailBody)]
        internal string BodyResetPasswordEmail { get; }

        [DefaultValue(Constants.DefaultEntityType)]
        internal string EntityType { get; }

        [DefaultValue(Constants.DefaultEmailFieldName)]
        internal string LoginFieldName { get; }

        [DefaultValue(Constants.DefaultPasswordFieldName)]
        internal string PasswordFieldName { get; }

        [DefaultValue(Constants.DefaultFailedLoginAttemptsFieldName)]
        internal string FailedLoginAttemptsFieldName { get; }

        [DefaultValue(Constants.DefaultLastLoginAttemptFieldName)]
        internal string LastLoginAttemptFieldName { get; }

        [DefaultValue(Constants.DefaultEmailFieldName)]
        internal string EmailAddressFieldName { get; }
        
        [DefaultValue(Constants.DefaultResetPasswordTokenFieldName)]
        internal string ResetPasswordTokenFieldName { get; }

        [DefaultValue(Constants.DefaultResetPasswordExpireDateFieldName)]
        internal string ResetPasswordExpireDateFieldName { get; }

        [DefaultValue(Constants.DefaultRoleFieldName)]
        internal string RoleFieldName { get; }

        [DefaultValue(Constants.DefaultNewPasswordFieldName)]
        internal string NewPasswordFieldName { get; }

        [DefaultValue(Constants.DefaultNewPasswordConfirmationFieldName)]
        internal string NewPasswordConfirmationFieldName { get; }

        [DefaultValue(Constants.DefaultGoogleCidFieldName)]
        internal string GoogleClientIdFieldName { get; }

        [DefaultValue(Constants.DefaultSubAccountEntityType)]
        internal string SubAccountEntityType { get; }

        [DefaultValue(Constants.DefaultLoginMainQuery)]
        internal string MainQuery { get; }

        [DefaultValue(Constants.DefaultLoginQuery)]
        internal string LoginQuery { get; }

        [DefaultValue(@"SELECT
    account.id,
    IFNULL(linkToMainAccount.mainAccountId, 0) AS mainAccountId,
    role.value AS role
FROM wiser_item AS account

LEFT JOIN wiser_itemdetail AS role ON role.item_id = account.id AND role.`key` = ?roleFieldName

LEFT JOIN (
	SELECT mainAccount.id AS mainAccountId, linkToMainAccount.item_id AS subAccountId
	FROM wiser_item AS mainAccount
	JOIN wiser_itemlink AS linkToMainAccount ON linkToMainAccount.destination_item_id = mainAccount.id AND linkToMainAccount.type = ?subAccountLinkTypeNumber
	WHERE mainAccount.entity_type = ?entityType
) AS linkToMainAccount ON linkToMainAccount.subAccountId = account.id

WHERE account.id = ?userId
AND account.entity_type IN(?entityType, ?subAccountEntityType)")]
        internal string AutoLoginQuery { get; }

        [DefaultValue(Constants.DefaultSaveLoginQuery)]
        internal string SaveLoginAttemptQuery { get; }

        [DefaultValue(Constants.DefaultSaveResetPasswordValuesQuery)]
        internal string SaveResetPasswordValuesQuery { get; }

        [DefaultValue(Constants.DefaultValidateResetPasswordTokenQuery)]
        internal string ValidateResetPasswordTokenQueryQuery { get; }

        [DefaultValue(Constants.DefaultChangePasswordQuery)]
        internal string ChangePasswordQuery { get; }

        [DefaultValue(Constants.DefaultGetUserIdViaEmailAddressQuery)]
        internal string GetUserIdViaEmailAddressQuery { get; }

        [DefaultValue(Constants.DefaultAmountOfDaysToRememberCookie)]
        internal int? AmountOfDaysToRememberCookie { get; }

        [DefaultValue(Constants.DefaultMaximumAmountOfFailedLoginAttempts)]
        internal int? MaximumAmountOfFailedLoginAttempts { get; }

        [DefaultValue(Constants.DefaultLockoutTime)]
        internal int? DefaultLockoutTime { get; }

        [DefaultValue(Constants.DefaultResetPasswordTokenValidity)]
        internal int ResetPasswordTokenValidity { get; }

        [DefaultValue(Constants.DefaultGoogleAuthenticatorSiteId)]
        internal string GoogleAuthenticatorSiteId { get; }

        [DefaultValue(Constants.DefaultOciUsernameKey)]
        internal string OciUsernameKey { get; }

        [DefaultValue(Constants.DefaultOciPasswordKey)]
        internal string OciPasswordKey { get; }

        [DefaultValue(Constants.DefaultOciHookUrlKey)]
        internal string OciHookUrlKey { get; }

        [DefaultValue("i")]
        internal string WiserLoginUserIdKey { get; }

        [DefaultValue("t")]
        internal string WiserLoginTokenKey { get; }
    }
}