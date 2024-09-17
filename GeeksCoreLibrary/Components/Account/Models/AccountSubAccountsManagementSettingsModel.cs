using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Account.Models
{
    internal class AccountSubAccountsManagementSettingsModel
    {
        [DefaultValue(@"<!-- There must always be a element with ID GclAccountContainer{contentId}, unless you also overwrite the TemplateJavascript. -->
<div id='GclAccountContainer{contentId}' class='account-content'>
	<h1>Subaccounts</h1>

	<div class='row'>
		<div class='col col-md-4'>
            [if({amountOfSubAccounts}=0)]
            <strong>Er zijn nog geen accounts</strong>
            [else]
			<strong>Bestaande accounts</strong>
			<ul class='item-list'>
				{repeat:subAccounts}
				<li data-id='{subAccount_id}' class='[if({selectedSubAccount}={subAccount_id})]active[endif]'>{subAccount_name}</li>
				{/repeat:subAccounts}
			</ul>
            [endif]
			<div class='btn-row btn-flex'>
				<button class='btn btn-small btn-primary btn-add-sub-account'>Account toevoegen</button>
                [if({amountOfSubAccounts}!0)]
				<button class='btn btn-small btn-primary btn-delete-sub-account'>Account verwijderen</button>
                [endif]
			</div>
		</div>
		<div class='col col-md-8'>
            {success}
            {error}
            [if({selectedSubAccount{contentId}}%{selectedSubAccount)]
            [else]
			<!-- There must always be a element with ID GclCreateOrUpdateSubAccountForm{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
			<jform class='subaccount-edit' id='GclCreateOrUpdateSubAccountForm{contentId}' method='POST'>
				{repeat:fields}
				<div class='form-row'>
					<label for='account-{property_name}'>{display_name}</label>
					<div class='form-container'>
						<div class='form-item'>
							<input type='{inputtype}' id='account-{property_name}' placeholder='{display_name}' name='{property_name}' value='{value}' />
						</div>
					</div>
				</div>
				{/repeat:fields}
				<div class='form-row btn-row'>
					<button type='submit' class='btn btn-small btn-primary'>Opslaan</button>
				</div>
			</jform>
            [endif]
		</div>
	</div>
</div>")]
        internal string Template { get; }

		[DefaultValue(@"<!-- There must always be a element with ID GclAccountContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id='GclAccountContainer{contentId}'>
    [if({deleteSubAccount{contentId}}>0)]
    <p>Het subaccount is verwijderd.</p>
    [else]
    <p>Het subaccount is opgeslagen.</p>
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
    [if({errorType}=Server)]
    <p>Er is een onbekende fout opgetreden. Probeer het a.u.b. nogmaals of neem contact op met ons.</p>
    [endif]
</div>")]
		internal string TemplateError { get; }

		[DefaultValue(@"(() => {
	let selectedSubAccount = 0;
	
	function setupHttpRequest(container, method, extraQueryStringParameters, selector) {
		const url = '/GclComponent.gcl?contentId={contentId}&callMethod=' + method + '&ombouw=false&type=Account' + (extraQueryStringParameters || '');
		
		const xhr = new XMLHttpRequest();
		xhr.open('POST', url);
		xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
		xhr.onload = () => {
			if (xhr.status !== 200) {
				alert('Request failed');
			} else {
				const div = document.createElement('div');
				div.innerHTML = xhr.responseText;
				container.innerHTML = div.querySelector(selector || '#GclAccountContainer{contentId}').innerHTML;
				initializeSubAccountsManagement();
			}
		};
		
		return xhr;
	}

	function initializeSubAccountsManagement() {
		const container = document.getElementById('GclAccountContainer{contentId}');
		if (!container) {
			return;
		}
		
		// Initalize the list of sub accounts.
		const subAccountsList = container.querySelector('ul.item-list');
		let allSubAccountItems;
		if (subAccountsList) {
			let subAccountItem;
			if (selectedSubAccount) {
				subAccountItem = subAccountsList.querySelector('li[data-id=""' + selectedSubAccount + '""]');
				if (subAccountItem) {
					subAccountItem.classList.add('active');
				}
			} else {
				subAccountItem = subAccountsList.querySelector('li.active');
				if (subAccountItem) {
					selectedSubAccount = subAccountItem.dataset.id || 0;
				}
			}
			
			allSubAccountItems = subAccountsList.querySelectorAll('li');
			allSubAccountItems.forEach(item => {
				item.addEventListener('click', event => {
					selectedSubAccount = event.currentTarget.dataset.id;
					allSubAccountItems.forEach(x => x.classList.remove('active'));
					event.currentTarget.classList.add('active');
					
					const xhr = setupHttpRequest(container, 'HandleSubAccountsManagementMode', '&selectedSubAccount{contentId}=' + selectedSubAccount);
					xhr.send();
				});
			});
		
			const deleteSubAccountButton = container.querySelector('.btn-delete-sub-account');
			if (deleteSubAccountButton) {
				if (!selectedSubAccount) {
					deleteSubAccountButton.style.display = 'none';
				} else {
					deleteSubAccountButton.style.display = '';
					deleteSubAccountButton.addEventListener('click', event => {
						event.preventDefault();
						
						if (!confirm('Weet u zeker dat u het geselecteerde subaccount wilt verwijderen?')) {
							return;
						}
						
						allSubAccountItems.forEach(x => x.classList.remove('active'));
						
						const xhr = setupHttpRequest(container, 'HandleSubAccountsManagementMode', '&deleteSubAccount{contentId}=' + selectedSubAccount);
						selectedSubAccount = 0;
						xhr.send();
					});
				}
			}
		}
		
		const addSubAccountButton = container.querySelector('.btn-add-sub-account');
		if (addSubAccountButton) {
			addSubAccountButton.addEventListener('click', event => {
				event.preventDefault();
				selectedSubAccount = 0;
				if (allSubAccountItems) {
					allSubAccountItems.forEach(x => x.classList.remove('active'));
				}
				
				const xhr = setupHttpRequest(container, 'HandleSubAccountsManagementMode', '&selectedSubAccount{contentId}=' + selectedSubAccount);
				xhr.send();
			});
		}
		
		// Initialize the sub account form.
		const subAccountForm = document.getElementById('GclCreateOrUpdateSubAccountForm{contentId}');
		if (!subAccountForm) {
			return;
		}
		
		const firstField = subAccountForm.querySelector('input');
		if (firstField) {
			firstField.focus();
		}
		
		subAccountForm.addEventListener('submit', function(event) {
			event.preventDefault();

			if (!this.checkValidity()) {
				return;
			}

			var xhr = setupHttpRequest(container, 'HandleSubAccountsManagementMode', '&selectedSubAccount{contentId}=' + selectedSubAccount);

			var fields = this.querySelectorAll('input, select, checkbox, textarea');
			var postdata = '';
			for (i = 0; i < fields.length; i++) {
				if (fields[i].getAttribute('type') === 'radio' || !fields[i].name) {
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


	initializeSubAccountsManagement();
})();")]
		internal string TemplateJavaScript { get; }

        [DefaultValue(Constants.DefaultEntityType)]
        internal string EntityType { get; }

        [DefaultValue("role")]
        internal string RoleFieldName { get; }

        [DefaultValue("new-password")]
        internal string NewPasswordFieldName { get; }

        [DefaultValue("new-password-confirmation")]
        internal string NewPasswordConfirmationFieldName { get; }

        [DefaultValue(Constants.DefaultGoogleCidFieldName)]
        internal string GoogleClientIdFieldName{ get; }

        [DefaultValue("sub-account")]
        internal string SubAccountEntityType { get; }

        [DefaultValue(@"SELECT
	subAccount.id AS subAccount_id,
	subAccount.title AS subAccount_name
FROM wiser_item AS account
JOIN wiser_itemlink AS subAccountToAccount ON subAccountToAccount.destination_item_id = account.id AND subAccountToAccount.type = ?subAccountLinkTypeNumber
JOIN wiser_item AS subAccount ON subAccount.id = subAccountToAccount.item_id AND subAccount.entity_type = ?subAccountEntityType
WHERE account.id = ?userId
AND account.entity_type = ?entityType")]
        internal string MainQuery { get; }

        [DefaultValue(@"SELECT 'Dit subaccount bestaat al' AS error
FROM wiser_item AS account
JOIN wiser_itemdetail AS login ON login.item_id = account.id AND login.`key` = ?loginFieldName
JOIN wiser_itemdetail AS email ON email.item_id = account.id AND email.`key` = ?emailAddressFieldName
WHERE account.entity_type IN(?entityType, ?subAccountEntityType)
AND (login.`value` = ?login OR email.`value` = ?emailAddress)
AND account.id <> ?subAccountId")]
        internal string CheckIfAccountExistsQuery { get; }

        [DefaultValue(@"INSERT INTO wiser_item (entity_type, moduleid, title, added_by) VALUES (?subAccountEntityType, 600, ?login, 'website');
SET @newSubAccountId = LAST_INSERT_ID();
SET @ordering = (SELECT IFNULL(MAX(ordering), 0) FROM wiser_itemlink WHERE destination_item_id = ?userId AND type = ?subAccountLinkTypeNumber);
INSERT INTO wiser_itemlink (item_id, destination_item_id, ordering, type)
VALUES (@newSubAccountId, ?userId, @ordering + 1, ?subAccountLinkTypeNumber);
SELECT @newSubAccountId AS id;")]
        internal string CreateAccountQuery { get; }

        [DefaultValue(@"UPDATE wiser_item AS subAccount
JOIN wiser_itemlink AS link ON link.item_id = subAccount.id AND link.destination_item_id = ?userId AND link.type = ?subAccountLinkTypeNumber
SET subAccount.title = ?login, subAccount.changed_by = 'website'
WHERE subAccount.id = ?subAccountId")]
        internal string UpdateAccountQuery { get; }

        [DefaultValue(@"# TODO")]
        internal string DeleteAccountQuery { get; }

        [DefaultValue(@"INSERT INTO wiser_itemdetail (item_id, `key`, value) VALUES (?subAccountId, ?name, ?value)
ON DUPLICATE KEY UPDATE value = VALUES(value)")]
        internal string SetValueInWiserEntityPropertyQuery { get; }

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
LEFT JOIN wiser_itemdetail AS detail ON detail.`key` = field.property_name AND detail.groupname = field.group_name AND detail.language_code = field.language_code AND detail.item_id = ?subAccountId
WHERE field.entity_name = ?subAccountEntityType
AND field.inputtype IN ('input', 'secure-input', 'numeric-input')
ORDER BY field.ordering ASC")]
        internal string GetSubAccountQuery { get; }

        [DefaultValue(Constants.DefaultPasswordValidationRegex)]
        internal string PasswordValidationRegex { get; }
    }
}