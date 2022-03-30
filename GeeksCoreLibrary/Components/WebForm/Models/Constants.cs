namespace GeeksCoreLibrary.Components.WebForm.Models
{
    public static class Constants
    {
        internal const string DefaultFormJavaScript = @"async function webFormRequest{contentId}(container, method, postData, extraQueryStringParameters) {
    const url = `/GclComponent.gcl?contentId={contentId}&callMethod=${method}&type=WebForm&ombouw=false${extraQueryStringParameters || ''}`;

    const headers = {};

    // Retrieve anti forgery token.
    const requestVerificationToken = document.getElementById('RequestVerificationToken');
    if (requestVerificationToken && requestVerificationToken.value !== '') {
        headers['X-CSRF-TOKEN'] = requestVerificationToken.value;
    } else {
        console.error('Could not find hidden input with ID ""RequestVerificationToken""! Without this token the request will fail, so make sure the body contains a hidden input with the ID ""RequestVerificationToken"" that contains the verficication code.');
    }

    const options = {
        method: 'POST',
        cache: 'no-cache',
        credentials: 'same-origin',
        headers: headers,
        body: postData
    };

    const response = await fetch(url, options);
    if (!response.ok) {
        console.error('Form post resulted in an error.');
        return null;
    }

    const responseText = await response.text();

    if (container instanceof HTMLElement) {
        const div = document.createElement('div');
        div.innerHTML = responseText;

        const newContainer = div.querySelector('#GclWebFormContainer{contentId}');

        if (newContainer) {
            container.innerHTML = div.querySelector('#GclWebFormContainer{contentId}').innerHTML;
        } else {
            container.innerHTML = div.innerHTML;
        }

        initializeWebForm{contentId}();
    }

    return responseText;
}

async function initializeWebForm{contentId}() {
    const form = document.getElementById('GclWebForm{contentId}');
    if (!form) {
        return;
    }

    form.addEventListener('submit', async function (event) {
        event.preventDefault();

        if (!this.checkValidity()) {
            return;
        }

        const container = document.getElementById('GclWebFormContainer{contentId}');
        const fields = this.querySelectorAll('input, select, checkbox, textarea');

        // Validate reCAPTCHA v2 field (if one is present on the form).
        const recaptchaV2ResponseField = [...fields].find(e => e.getAttribute('name') === 'g-recaptcha-response');
        if (recaptchaV2ResponseField && recaptchaV2ResponseField.value.trim() === '') {
            return;
        }

        // Handle reCAPTCHA v3 (if one is present on the form).
        const recaptchaV3ResponseField = [...fields].find(e => e.getAttribute('name') === 'g-recaptcha-response-v3');
        if (recaptchaV3ResponseField && typeof gclExecuteReCaptcha === 'function') {
            const token = await gclExecuteReCaptcha('Form_{WebFormName}_submit');
            if (!token) {
                return;
            }

            recaptchaV3ResponseField.value = token;
        }

        const postData = new FormData();

        for (let i = 0; i < fields.length; i++) {
            const fieldType = fields[i].getAttribute('type');

            switch (fieldType) {
                case 'radio':
                    continue;
                case 'file':
                    addFilesToFormData(fields[i], postData);
                    break;
                default:
                    postData.append(fields[i].name, fields[i].value);
                    break;
            }
        }

        const radioButtons = this.querySelectorAll('input[type=radio]:checked');
        for (let i = 0; i < radioButtons.length; i++) {
            postData.append(radioButtons[i].name, radioButtons[i].value);
        }

        const request = await webFormRequest{contentId}(container, 'SubmitForm', postData);
    });
}

function addFilesToFormData(fileField, formData) {
    if (fileField.files.length === 0) {
        return;
    }

    for (let i = 0; i < fileField.files.length; i++) {
        formData.append(fileField.name, fileField.files[i], fileField.files[i].name);
    }
}

initializeWebForm{contentId}();";
    }
}
