namespace GeeksCoreLibrary.Components.Account.Models
{
    public static class Constants
    {
        internal const string ComponentIdFormKey = "__account";

        // Query column names.
        internal const string PasswordColumn = "password";
        internal const string LastLoginDateColumn = "lastLoginAttempt";
        internal const string FailedLoginAttemptsColumn = "failedLoginAttempts";
        internal const string UserIdColumn = "id";
        internal const string LoginColumn = "login";
        internal const string EmailAddressColumn = "email";
        internal const string MainAccountIdColumn = "mainAccountId";
        internal const string PropertyNameColumn = "property_name";
        internal const string RoleIdColumn = "roleId";
        
        // Table names.
        internal const string AuthenticationTokensTableName = "gcl_user_auth_token";
        
        // Query string keys.
        internal const string LogoutQueryStringKey = "logoutUser";
        internal const string UserIdQueryStringKey = "user";
        internal const string ResetPasswordTokenQueryStringKey = "token";
        internal const string SelectedSubAccountQueryStringKey = "selectedSubAccount";
        internal const string DeleteSubAccountQueryStringKey = "deleteSubAccount";

        // HTML field names.
        internal const string StepNumberFieldName = "accountStepNumber";
        internal const string GoogleAuthenticationPinFieldName = "googleAuthenticationPin";
        internal const string GoogleAuthenticationVerificationIdFieldName = "googleAuthenticationVerificationId";
        internal const string ExternalLoginButtonOrFieldName = "externalLogin";
        
        // Cookies and sessions.
        public const string CookieName = "gcl_user_cookie";
        internal const string GoogleAnalyticsCookieName = "_ga";
        public const string OciHookUrlCookieName = "gcl_oci_hook_url";
        public const string CreatedAccountCookieName = "gcl_user_created";

        public const string LoginValueSessionKey = "AccountSavedLogin";
        public const string UserIdSessionKey = "AccountUserId";

        public const string UserDataCachingKey = "GCLAccountUser";

        #region Default settings

        public const string DefaultEntityType = "account";
        internal const string DefaultResetPasswordSubject = "Wachtwoord vergeten";
        internal const string DefaultEmailFieldName = "email";
        internal const string DefaultPasswordFieldName = "password";
        internal const string DefaultFailedLoginAttemptsFieldName = "failed-login-attempts";
        internal const string DefaultLastLoginAttemptFieldName = "last-login-attempt";
        internal const string DefaultResetPasswordTokenFieldName = "reset-password-token";
        internal const string DefaultResetPasswordExpireDateFieldName = "reset-password-expire-date";
        internal const string DefaultRoleFieldName = "role";
        internal const string DefaultNewPasswordFieldName = "new-password";
        internal const string DefaultNewPasswordConfirmationFieldName = "new-password-confirmation";
        internal const string DefaultGoogleCidFieldName = "google-cid";
        public const string DefaultSubAccountEntityType = "sub-account";
        internal const string DefaultPunchOutSessionTableName = "punchout_sessions";
        internal const string DefaultPunchOutSessionQueryStringParameterName = "ociSessionToken";
        internal const int DefaultAmountOfDaysToRememberCookie = 7;
        internal const int DefaultMaximumAmountOfFailedLoginAttempts = 25;
        internal const int DefaultLockoutTime = 60;
        internal const int DefaultResetPasswordTokenValidity = 7;
        internal const string DefaultGoogleAuthenticatorSiteId = "WiserGlobal";
        internal const string DefaultOciUsernameKey = "username";
        internal const string DefaultOciPasswordKey = "password";
        internal const string DefaultOciHookUrlKey = "HOOK_URL";
        internal const string TotpFieldName = "User2FAKey";
        internal const string DefaultPasswordValidationRegex = "^(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z]).{8,}$";

        internal const string DefaultLoginMainQuery = @"SELECT account.title
FROM wiser_item AS account
WHERE account.id = ?userId";

        internal const string DefaultLoginQuery = @"SELECT
    account.id,
    password.value AS password,
    lastLoginAttempt.value AS lastLoginAttempt,
    IFNULL(failedLoginAttempts.value, 0) AS failedLoginAttempts,
    email.value AS email,
    IFNULL(linkToMainAccount.mainAccountId, 0) AS mainAccountId,
    role.value AS role
FROM wiser_item AS account

JOIN wiser_itemdetail AS login ON login.item_id = account.id AND login.`key` = ?loginFieldName AND login.value = ?login
LEFT JOIN wiser_itemdetail AS password ON password.item_id = account.id AND password.`key` = ?passwordFieldName
LEFT JOIN wiser_itemdetail AS email ON email.item_id = account.id AND email.`key` = ?emailAddressFieldName
LEFT JOIN wiser_itemdetail AS lastLoginAttempt ON lastLoginAttempt.item_id = account.id AND lastLoginAttempt.`key` = ?lastLoginAttemptFieldName
LEFT JOIN wiser_itemdetail AS failedLoginAttempts ON failedLoginAttempts.item_id = account.id AND failedLoginAttempts.`key` = ?failedLoginAttemptsFieldName
LEFT JOIN wiser_itemdetail AS role ON role.item_id = account.id AND role.`key` = ?roleFieldName

LEFT JOIN (
	SELECT mainAccount.id AS mainAccountId, linkToMainAccount.item_id AS subAccountId
	FROM wiser_item AS mainAccount
	JOIN wiser_itemlink AS linkToMainAccount ON linkToMainAccount.destination_item_id = mainAccount.id AND linkToMainAccount.type = ?subAccountLinkTypeNumber
	WHERE mainAccount.entity_type = ?entityType
) AS linkToMainAccount ON linkToMainAccount.subAccountId = account.id

WHERE account.entity_type IN(?entityType, ?subAccountEntityType)";

        internal const string DefaultSaveLoginQuery = @"INSERT INTO wiser_itemdetail (item_id, `key`, `value`)
VALUES (?userId, ?failedLoginAttemptsFieldName, IF(?success, 0, 1))
ON DUPLICATE KEY UPDATE `value` = IF(?success, 0, `value` + 1);

INSERT INTO wiser_itemdetail (item_id, `key`, `value`)
VALUES (?userId, ?lastLoginAttemptFieldName, NOW())
ON DUPLICATE KEY UPDATE `value` = NOW();";
        
        internal const string DefaultLoginJavascript = @"function setupHttpRequest{contentId}(container, method, extraQueryStringParameters) {
    var url = '/GclComponent.gcl?contentId={contentId}&callMethod=' + method + '&trace=false&ombouw=false&type=Account' + (extraQueryStringParameters || '');
    
    var xhr = new XMLHttpRequest();
    xhr.open('POST', url);
    xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
    xhr.onload = function() {
        if (xhr.status !== 200) {
            alert('Request failed');
        } else {
            var div = document.createElement('div');
            div.innerHTML = xhr.responseText;
            container.innerHTML = div.querySelector('#GclAccountContainer{contentId}').innerHTML;
            initializeLoginForm{contentId}();
            initializeLogoutLink{contentId}();
            initializeResetPasswordLink{contentId}();
            initializeResetPasswordForm{contentId}();
        }
    };
    
    return xhr;
}

function initializeLogoutLink{contentId}() {
    var logoutLink = document.getElementById('GclLogoutLink{contentId}');
    if (!logoutLink) {
        return;
    }
    
    logoutLink.addEventListener('click', function(event) {
        event.preventDefault();
        
        var container = document.getElementById('GclAccountContainer{contentId}');
        var xhr = setupHttpRequest{contentId}(container, 'HandleLoginMode', '&logoutUser{contentId}=true');
        xhr.send();
    });
}

function initializeLoginForm{contentId}() {
    var loginForm = document.getElementById('GclLoginForm{contentId}');
    if (!loginForm) {
        return;
    }
    
    loginForm.addEventListener('submit', function(event) {
        event.preventDefault();

        if (!this.checkValidity()) {
            return;
        }

        var container = document.getElementById('GclAccountContainer{contentId}');
        var xhr = setupHttpRequest{contentId}(container, 'HandleLoginMode');

        var fields = this.querySelectorAll('input, select, checkbox, textarea');
        var postdata = '';
        for (i = 0; i < fields.length; i++) {
            if (fields[i].getAttribute('type') === 'radio') {
                continue;
            }

            postdata += encodeURIComponent(fields[i].name) + '=' + encodeURIComponent(fields[i].value) + '&';
        }

        var radioButtons = this.querySelectorAll('input[type=radio]:checked');
        for (i = 0; i < radioButtons.length; i++) {
            postdata += encodeURIComponent(radioButtons[i].name) + '=' + encodeURIComponent(radioButtons[i].value) + '&';
        }

        xhr.send(postdata);
    });
}

function initializeResetPasswordLink{contentId}() {
    var resetPasswordLink = document.getElementById('GclResetPasswordLink{contentId}');
    if (!resetPasswordLink) {
        return;
    }
    
    resetPasswordLink.addEventListener('click', function(event) {
        event.preventDefault();
        
        var container = document.getElementById('GclAccountContainer{contentId}');
        var xhr = setupHttpRequest{contentId}(container, 'HandleResetPasswordMode', '&componentMode=3');
        xhr.send();
    });
}

function initializeResetPasswordForm{contentId}() {
    var container = document.getElementById('GclAccountContainer{contentId}');
    var backButton = container.querySelector('.btnBack');
    if (backButton) {
        backButton.addEventListener('click', function(event) {
            event.preventDefault();
            
            var xhr = setupHttpRequest{contentId}(container, 'HandleLoginMode');
            xhr.send();
        });
    }
    
    var resetPasswordForm = document.getElementById('GclResetPasswordForm{contentId}');
    if (!resetPasswordForm) {
        return;
    }
    
    resetPasswordForm.addEventListener('submit', function(event) {
        event.preventDefault();

        if (!this.checkValidity()) {
            return;
        }

        var xhr = setupHttpRequest{contentId}(container, 'HandleResetPasswordMode', '&componentMode=3' + location.search.replace('?', '&'));

        var fields = this.querySelectorAll('input, select, checkbox, textarea');
        var postdata = '';
        for (i = 0; i < fields.length; i++) {
            if (fields[i].getAttribute('type') === 'radio') {
                continue;
            }

            postdata += encodeURIComponent(fields[i].name) + '=' + encodeURIComponent(fields[i].value) + '&';
        }

        var radioButtons = this.querySelectorAll('input[type=radio]:checked');
        for (i = 0; i < radioButtons.length; i++) {
            postdata += encodeURIComponent(radioButtons[i].name) + '=' + encodeURIComponent(radioButtons[i].value) + '&';
        }

        xhr.send(postdata);
    });
}

initializeLoginForm{contentId}();
initializeLogoutLink{contentId}();
initializeResetPasswordLink{contentId}();
initializeResetPasswordForm{contentId}();";

        internal const string DefaultLoginSuccessTemplate = @"<!-- There must always be a element with ID GclAccountContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
    <div id='GclAccountContainer{contentId}'>
    [if({sentActivationMail}=true)]
    <h2>Account activeren</h2>
    <p>Dit account is nog niet geactiveerd. Je ontvangt binnen enkele ogenblikken een e-mail met instructies om een wachtwoord in te stellen.</p>
    [else]
    <h2>Ingelogd als {title}</h2>
    <p><a href='{logoutUrl}' id='GclLogoutLink{contentId}'>Uitloggen</a></p>
    [endif]
</div>";

        internal const string DefaultLoginErrorTemplate = @"<div class='error'>
    [if({errorType}=InvalidUsernameOrPassword)]
    <p>Je hebt een ongeldige gebruikersnaam of wachtwoord ingevuld.</p>
    [endif]
    [if({errorType}=UserDoesNotExist)]
    <p>Deze gebruiker bestaat niet.</p>
    [endif]
    [if({errorType}=InvalidPassword)]
    <p>Je hebt een ongeldige wachtwoord ingevuld.</p>
    [endif]
    [if({errorType}=TooManyAttempts)]
    <p>Je hebt te vaak in een korte tijd foutief ingelogd. Je kunt het over een uur nogmaals proberen.</p>
    [endif]
    [if({errorType}=UserNotActivated)]
    <p>Jouw account is nog niet actief. Je krijgt binnen enkele ogenblikken een e-mail met instructies om uw account te activeren.</p>
    [endif]
    [if({errorType}=InvalidTwoFactorAuthentication)]
    <p>Je hebt een foutieve PIN ingevuld voor de Google authenticatie.</p>
    [endif]
    [if({errorType}=InvalidValidationToken)]
    <p>De validatietoken voor automatische login is ongeldig.</p>
    [endif]
    [if({errorType}=InvalidUserId)]
    <p>De user ID voor automatische login is ongeldig.</p>
    [endif]
    [if({errorType}=Server)]
    <p>Er is een onbekende fout opgetreden. Probeer het a.u.b. nogmaals of neem contact op met ons.</p>
    [endif]
</div>";

        internal const string DefaultSaveResetPasswordValuesQuery = @"INSERT INTO wiser_itemdetail (item_id, `key`, `value`)
VALUES (?userId, ?resetPasswordTokenFieldName, ?resetPasswordToken),
       (?userId, ?resetPasswordExpireDateFieldName, ?resetPasswordExpireDate)
ON DUPLICATE KEY UPDATE `value` = VALUES(`value`);";

        internal const string DefaultResetPasswordMailBody = @"<p>U kunt een nieuw wachtwoord instellen middels onderstaande link:<p><p><a href='{url}'>{url}</a></p>";

        internal const string DefaultNewAccountNotificationsMailBody = @"<p>Er is een nieuw account aangemaakt met het e-mailadres '{emailAddress}'.</p>";

        internal const string DefaultValidateResetPasswordTokenQuery = @"SELECT login.value AS login
FROM wiser_item AS account
JOIN wiser_itemdetail AS token ON token.item_id = account.id AND token.`key` = ?resetPasswordTokenFieldName AND token.value = ?token
JOIN wiser_itemdetail AS expiration ON token.item_id = account.id AND expiration.`key` = ?resetPasswordExpireDateFieldName AND expiration.value > NOW()
JOIN wiser_itemdetail AS login ON login.item_id = account.id AND login.`key` = ?loginFieldName
WHERE account.id = ?userId
AND account.entity_type IN(?entityType, ?subAccountEntityType)";

        internal const string DefaultChangePasswordQuery = @"INSERT INTO wiser_itemdetail (item_id, `key`, `value`)
VALUES (?userId, ?passwordFieldName, ?newPasswordHash),
       (?userId, ?resetPasswordTokenFieldName, ''),
       (?userId, ?resetPasswordExpireDateFieldName, NOW())
ON DUPLICATE KEY UPDATE `value` = VALUES(`value`);";

        internal const string DefaultGetUserIdViaEmailAddressQuery = @"SELECT account.id AS id
FROM wiser_item AS account
JOIN wiser_itemdetail AS email ON email.item_id = account.id AND email.`key` = ?emailAddressFieldName AND email.`value` = ?emailAddress
WHERE account.entity_type IN(?entityType, ?subAccountEntityType)";

        #endregion
    }
}
