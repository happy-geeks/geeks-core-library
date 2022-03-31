using System.ComponentModel;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    internal class OrderProcessCheckoutSettingsModel
    {
        #region Tab layout properties

        [DefaultValue(@"<h1>Bestelproces</h1>
{progress}
{step}")]
        internal string Template { get; }

        [DefaultValue(@"<form method='POST' id='step{activeStep}' data-active-step='{activeStep}' class='step-container'>
    <h2>{title}</h2>
    <div id='step_{title:Seo}' class='step-content'>
        <div class='step-header'>{header}</div>
        {error}
        <div class='step-groups'>{groups}</div>
        <div class='step-footer'>{footer}</div>
    </div>
    [if({activeStep}>1)]<a href='{previousStepUrl}'>{previousStepLinkText}</a>[endif]
    <button type='submit' id='confirmButton'>{confirmButtonText}</button>
</form>")]
        internal string TemplateStep { get; }

        [DefaultValue(@"<div class='error'>
    [if({errorType}=Client)]
    <p>[T{Niet alle gegevens zijn correct ingevuld, controleer de gegevens en probeer het opnieuw.}]</p>
    [endif]
    [if({errorType}=Server)]
    <p>[T{Er is een onbekende fout opgetreden. Probeer het a.u.b. nogmaals of neem contact op met ons.}]</p>
    [endif]
</div>")]
        internal string TemplateStepError { get; }

        [DefaultValue(@"<fieldset class='group-container'>
    <legend>{title}</legend>
    <div class='group-header'>{header}</div>
    <div class='group-fields'>{fields}</div>
    <div class='group-footer'>{footer}</div>
</fieldset>")]
        internal string TemplateFieldsGroup { get; }

        [DefaultValue(@"<fieldset class='group-container'>
    <legend>{title}</legend>
    <div class='group-header'>{header}</div>
    {error}
    <ul class='group-payment-methods'>{paymentMethods}</ul>
    <div class='group-footer'>{footer}</div>
</fieldset>")]
        internal string TemplatePaymentMethodsGroup { get; }

        [DefaultValue(@"<span class='field-error'>[if({errorMessage}=)][T{Vul a.u.b. een geldige waarde in}][else]{errorMessage}[endif]</span>")]
        internal string TemplateFieldError { get; }

        [DefaultValue(@"<div id='container_{fieldId}' class='field-container {errorClass}'>
    [if({label}!)]<label for='{fieldId}'>{label}</label>[endif]
    <input type='{inputType}' id='{fieldId}' name='{fieldId}' placeholder='{placeholder}' {required} {pattern} value='{value}' />
    {error}
</div>")]
        internal string TemplateInputField { get; }

        [DefaultValue(@"<div id='container_{fieldId}' class='field-container {errorClass}'>
    [if({label}!)]<label>{label}</label>[endif]
    {options}
    {error}
</div>")]
        internal string TemplateRadioButtonField { get; }
        
        [DefaultValue(@"<label>
    <input type='radio' id='{fieldId}_{optionValue}' name='{fieldId}' value='{optionValue}' {required} {checked} />
    <span class='label'>{optionText}</span>
</label>")]
        internal string TemplateRadioButtonFieldOption { get; }
        
        [DefaultValue(@"<div id='container_{fieldId}' class='field-container {errorClass}'>
    [if({label}!)]<label for='{fieldId}'>{label}</label>[endif]
    <select id='{fieldId}' name='{fieldId}' {required}>
        {options}
    </select>
    {error}
</div>")]
        internal string TemplateSelectField { get; }
        
        [DefaultValue(@"<option id='{fieldId}_{optionValue}' value='{optionValue}' {selected} /> <span class='label'>{optionText}</span></label>")]
        internal string TemplateSelectFieldOption { get; }
        
        [DefaultValue(@"<div id='container_{fieldId}' class='field-container {errorClass}'>
    <label>
        <input type='checkbox' id='{fieldId}' name='{fieldId}' {required} {checked} value='1' />
        <span class='label'>{label}</span>
    </label>
    {error}
</div>")]
        internal string TemplateCheckboxField { get; }

        [DefaultValue(@"<div id='progress'>
    {steps}
</div>")]
        internal string TemplateProgress { get; }

        [DefaultValue(@"<div class='progress-step {active}' id='progressStep{number}' data-active-step='{activeStep}' data-id='{id}'>
    <span class='number'>{number}</span>
    <span class='name'>{title}</span>
</div>")]
        internal string TemplateProgressStep { get; }

        [DefaultValue(@"<li>
    <label>
        <input type='radio' id='paymentMethod_{id}' value='{id}' name='{paymentMethodFieldName}' required />
        <img class='logo' src='/image/wiser2/{id}/{logoPropertyName}/crop/32/32/{title:Seo}.png' alt='{title}' />
        <span class='label'>{title}</span>
    </label>
</li>")]
        internal string TemplatePaymentMethod { get; }

        #endregion
    }
}
