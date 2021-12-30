using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Account.Models
{
    internal class AccountCreateOrUpdateAccountSettingsModel
    {
        [DefaultValue(@"<!-- There must always be a element with ID GclAccountContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id='GclAccountContainer{contentId}'>
    <jform id='GclCreateOrUpdateAccountForm{contentId}' method='POST'>
        <div class='formPanel'>
            <h2>[if({Account_UserId}>0)]Gegevens wijzigen[else]Account maken[endif]</h2>
            {error}
            {repeat:fields}
            <div class='formRow'>
                <label for='{property_name}{contentId}'>{display_name}</label>
                <input type='{inputtype}' name='{property_name}' id='{property_name}{contentId}' value='{value:htmlencode}'>
            </div>
            {/repeat:fields}
            <div class='formRow'>
                <label for='new-password{contentId}'>Nieuw wachtwoord</label>
                <input type='password' name='new-password' id='new-password{contentId}'>
            </div>
            <div class='formRow'>
                <label for='new-password-confirmation{contentId}'>Nieuw wachtwoord herhalen</label>
                <input type='password' name='new-password-confirmation' id='new-password-confirmation{contentId}'>
            </div>
            <div class='formRow'>
                <button type='submit' class='btn btnSend'>Opslaan</button>
            </div>
        </div>
    </jform>
</div>")]
        internal string Template { get; }

        [DefaultValue(@"<!-- There must always be a element with ID GclAccountContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id='GclAccountContainer{contentId}'>
    [if({isLoggedIn}=true)]
    <p>Jouw account is aangemaakt en je ben automatisch ingelogd.</p>
    [else]
    <p>Jouw account is gewijzigd/aangemaakt.</p>
    [endif]
</div>")]
        internal string TemplateSuccess { get; }

        [DefaultValue(@"<div class='error'>
    [if({errorType}=)]
    <p>{error}</p>
    [endif]
    [if({errorType}=PasswordsNotTheSame)]
    <p>Vul a.u.b. 2 keer hetzelfde wachtwoord in.</p>
    [endif]
    [if({errorType}=EmptyPassword)]
    <p>Vul a.u.b. alle velden in.</p>
    [endif]
    [if({errorType}=PasswordNotSecure)]
    <p>Wachtwoorden moeten minimaal 1 kleine letter, 1 hoofdletter en 1 cijfer bevatten en minimaal 8 tekens in totaal bevatten.</p>
    [endif]
    [if({errorType}=UserAlreadyExists)]
    <p>Er bestaat al een gebruiker met deze gebruikersnaam en/of e-mailadres.</p>
    [endif]
    [if({errorType}=Server)]
    <p>Er is een onbekende fout opgetreden. Probeer het a.u.b. nogmaals of neem contact op met ons.</p>
    [endif]
</div>")]
        internal string TemplateError { get; }

        [DefaultValue("Nieuw account aangemaakt")]
        internal string SubjectNewAccountNotificationEmail { get; }

        [DefaultValue(Constants.DefaultNewAccountNotificationsMailBody)]
        internal string BodyNewAccountNotificationEmail { get; }

        [DefaultValue(Constants.DefaultEntityType)]
        internal string EntityType { get; }

        [DefaultValue(Constants.DefaultEmailFieldName)]
        internal string LoginFieldName { get; }

        [DefaultValue(Constants.DefaultPasswordFieldName)]
        internal string PasswordFieldName { get; }
        
        [DefaultValue(Constants.DefaultEmailFieldName)]
        internal string EmailAddressFieldName { get; }
        
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

        [DefaultValue(@"SELECT 
	CASE field.inputtype
		WHEN 'secure-input' THEN 'password'
		WHEN 'numeric-input' THEN 'number'
		ELSE 'text'
	END AS inputtype,
	field.property_name,
	field.display_name,
	CASE field.inputtype
		WHEN 'secure-input' THEN ''
		ELSE IFNULL(detail.value, '')
	END AS value
FROM wiser_entityproperty AS field
LEFT JOIN wiser_itemdetail AS detail ON detail.`key` = field.property_name AND detail.groupname = field.group_name AND detail.language_code = field.language_code AND detail.item_id = ?userId
WHERE field.entity_name = ?entityType
AND field.inputtype IN ('input', 'secure-input', 'numeric-input')
ORDER BY field.ordering ASC")]
        internal string MainQuery { get; }

        [DefaultValue(Constants.DefaultLoginQuery)]
        internal string LoginQuery { get; }

        [DefaultValue(Constants.DefaultSaveLoginQuery)]
        internal string SaveLoginAttemptQuery { get; }

        [DefaultValue(Constants.DefaultChangePasswordQuery)]
        internal string ChangePasswordQuery { get; }

        [DefaultValue(@"SELECT 'Dit account bestaat al' AS error
FROM wiser_item AS account
JOIN wiser_itemdetail AS login ON login.item_id = account.id AND login.`key` = ?loginFieldName
JOIN wiser_itemdetail AS email ON email.item_id = account.id AND email.`key` = ?emailAddressFieldName
WHERE account.removed = 0
AND account.entity_type IN(?entityType, ?subAccountEntityType)
AND (login.`value` = ?login OR email.`value` = ?emailAddress)
AND account.id <> ?userId")]
        internal string CheckIfAccountExistsQuery { get; }

        [DefaultValue(@"SELECT password.value AS password
FROM wiser_item AS account

JOIN wiser_itemdetail AS login ON login.item_id = account.id AND login.`key` = ?loginFieldName
LEFT JOIN wiser_itemdetail AS password ON password.item_id = account.id AND password.`key` = ?passwordFieldName

WHERE account.entity_type IN(?entityType, ?subAccountEntityType)
AND account.removed = 0
AND account.id = ?userId")]
        internal string ValidatePasswordQuery { get; }

        [DefaultValue(@"INSERT INTO wiser_item (entity_type, moduleid, title, added_by) VALUES (?entityType, 600, ?login, 'website');
SELECT LAST_INSERT_ID() AS id;")]
        internal string CreateAccountQuery { get; }

        [DefaultValue(@"UPDATE wiser_item SET title = ?login, changed_by = 'website' WHERE id = ?userId")]
        internal string UpdateAccountQuery { get; }

        [DefaultValue(@"INSERT INTO wiser_itemdetail (item_id, `key`, value) VALUES (?userId, ?name, ?value)
ON DUPLICATE KEY UPDATE value = VALUES(value)")]
        internal string SetValueInWiserEntityPropertyQuery { get; }

        [DefaultValue(Constants.DefaultPasswordValidationRegex)]
        internal string PasswordValidationRegex { get; }

        [DefaultValue(Constants.DefaultAmountOfDaysToRememberCookie)]
        internal int? AmountOfDaysToRememberCookie { get; }

        [DefaultValue(Constants.DefaultLastLoginAttemptFieldName)]
        internal string LastLoginAttemptFieldName { get; }
    }
}
