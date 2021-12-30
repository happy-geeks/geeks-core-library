using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Account.Models
{
    internal class AccountResetPasswordSettingsModel
    {
        [DefaultValue(@"<!-- There must always be a element with ID GclAccountContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id='GclAccountContainer{contentId}'>
    <jform id='GclResetPasswordForm{contentId}' method='POST'>
        <div class='formPanel'>
            [if({user}%{user)]
            <h2>Wachtwoord vergeten</h2>
            <p>Weet je het wachtwoord niet meer? Vul hieronder je e-mailadres in. We sturen dan binnen enkele minuten een e-mail waarmee een nieuw wachtwoord kan worden aangemaakt.</p>
            <div class='formRow'>
                <label for='email{contentId}'>E-mailadres</label>
                <input type='email' name='{emailAddressFieldName}' id='email{contentId}'>
            </div>
            <div class='formRow formButtons'>
                <button type='button' class='btn btnBack'>Terug</button>
                <button type='submit' class='btn btnSend'>Verzenden</button>
            </div>
            [else]
            <h2>Kies een nieuw wachtwoord</h2>
            {error}
            [if({errorType}!InvalidTokenOrUser)]
            <div class='formRow passwordRow'>
                <label for='newPassword{contentId}'>Wachtwoord</label>
                <input type='password' name='{newPasswordFieldName}' class='password' id='newPassword{contentId}' pattern='{PasswordValidationRegex}'>
                <ins class='icon-eye-show'></ins>
            </div>
            <div class='formRow passwordRow'>
                <label for='newPasswordConfirmation{contentId}'>Wachtwoord bevestiging</label>
                <input type='password' name='{newPasswordConfirmationFieldName}' class='password' id='newPasswordConfirmation{contentId}' pattern='{PasswordValidationRegex}'>
                <ins class='icon-eye-show'></ins>
            </div>
            <div class='formRow'>
                <button type='submit' class='btn btnSend'>Opslaan</button>
            </div>
            [endif]
            [endif]
        </div>
    </jform>
</div>")]
        internal string Template { get; }

        [DefaultValue(@"<!-- There must always be a element with ID GclAccountContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id='GclAccountContainer{contentId}'>
    [if({user}%{user)]
    <h2>Wachtwoord vergeten</h2>
    <p>Je ontvangt binnen enkele ogenblikken een e-mail met instructies om jouw wachtwoord te resetten.</p>
    [else]
    <h2>Kies een nieuw wachtwoord</h2>
    [if({isLoggedIn}=true)]
    <p>Je wachtwoord is gewijzigd en je bent automatisch ingelogd.</p>
    [else]
    <p>Je wachtwoord is gewijzigd, je kunt nu <a href='#' class='btnBack'>direct inloggen</a> met dit nieuwe wachtwoord.</p>
    [endif]
    [endif]
</div>")]
        internal string TemplateSuccess { get; }

        [DefaultValue(@"<div class='error'>
    [if({errorType}=)]
    <p>{error}</p>
    [endif]
    [if({errorType}=InvalidTokenOrUser)]
    <p>Deze link is niet (meer) geldig. Gebruik a.u.b. de wachtwoord vergeten optie opnieuw.</p>
    [endif]
    [if({errorType}=PasswordsNotTheSame)]
    <p>Vul a.u.b. 2 keer hetzelfde wachtwoord in.</p>
    [endif] 
    [if({errorType}=OldPasswordInvalid)]
    <p>U heeft een verkeerd wachtwoord ingevuld.</p>
    [endif]
    [if({errorType}=EmptyPassword)]
    <p>Vul a.u.b. alle velden in.</p>
    [endif]
    [if({errorType}=PasswordNotSecure)]
    <p>Wachtwoorden moeten minimaal 1 kleine letter, 1 hoofdletter en 1 cijfer bevatten en minimaal 8 tekens in totaal bevatten.</p>
    [endif]
    [if({errorType}=Server)]
    <p>Er is een onbekende fout opgetreden. Probeer het a.u.b. nogmaals of neem contact op met ons.</p>
    [endif]
</div>")]
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

        [DefaultValue("")]
        internal string MainQuery { get; }

        [DefaultValue(Constants.DefaultLoginQuery)]
        internal string LoginQuery { get; }

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

        [DefaultValue(Constants.DefaultResetPasswordTokenValidity)]
        internal int? ResetPasswordTokenValidity { get; }

        [DefaultValue(Constants.DefaultGoogleAuthenticatorSiteId)]
        internal int GoogleAuthenticatorSiteId { get; }

        [DefaultValue(Constants.DefaultPasswordValidationRegex)]
        internal string PasswordValidationRegex { get; }
    }
}
