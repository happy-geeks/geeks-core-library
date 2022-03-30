using System.ComponentModel;

namespace GeeksCoreLibrary.Components.WebForm.Models
{
    internal class WebFormBasicFormSettingsModel
    {
        [DefaultValue(@"<!-- There must always be an element with ID GclWebFormContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id='GclWebFormContainer{contentId}'>
    <jform id='GclWebForm{contentId}' method='POST'>
        <div class='formPanel'>
            <h2>My Form</h2>
            <div class='formRow'>
                <label for='name{contentId}'>Naam</label>
                <input type='text' name='name' id='name{contentId}' />
            </div>
            <div class='formRow'>
                <button type='submit' class='btn btnSend'>Verzenden</button>
            </div>
        </div>
    </jform>
</div>")]
        internal string FormHtmlTemplate { get; }

        [DefaultValue(Constants.DefaultFormJavaScript)]
        internal string TemplateJavaScript { get; }

        [DefaultValue(0.5)]
        internal decimal ReCaptchaV3ScoreThreshold { get; }
    }
}
