using System.ComponentModel;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    internal class OrderProcessAutomaticSettingsModel
    {
        #region Tab layout properties

        [DefaultValue(@"<h1>Bestelproces</h1>
{progress}
<div id='steps'>{step}</div>")]
        internal string Template { get; }

        [DefaultValue(@"<h2>{title}</h2>
<div id='step_{title:Seo}'>
    {header}
    {groups}
    {footer}
</div>")]
        internal string TemplateStep { get; }

        [DefaultValue(@"<fieldset>
    <legend>{title}</legend>
    {header}
    {fields}
    {footer}
</fieldset>")]
        internal string TemplateGroup { get; }

        [DefaultValue(@"<div id='container_{fieldId}' class='field-container'>
    [if({label}!)]<label for='{fieldId}'>{label}</label>[endif]
    <input type='{inputType}' id='{fieldId}' name='{fieldId}' placeholder='{placeholder}' {required} pattern='{pattern}' value='{value}' />
</div>")]
        internal string TemplateInputField { get; }

        [DefaultValue(@"<div id='container_{fieldId}' class='field-container'>
    [if({label}!)]<label>{label}</label>[endif]
    {options}
</div>")]
        internal string TemplateRadioButtonField { get; }
        
        [DefaultValue(@"<label>
    <input type='radio' id='{fieldId}_{optionValue}' name='{fieldId}' value='{optionValue}' {required} {checked} />
    <span class='label'>{optionText}</span>
</label>")]
        internal string TemplateRadioButtonFieldOption { get; }
        
        [DefaultValue(@"<div id='container_{fieldId}' class='field-container'>
    [if({label}!)]<label for='{fieldId}'>{label}</label>[endif]
    <select id='{fieldId}' name='{fieldId}' {required}>
        {options}
    </select>
</div>")]
        internal string TemplateSelectField { get; }
        
        [DefaultValue(@"<option id='{fieldId}_{optionValue}' value='{optionValue}' {selected} /> <span class='label'>{optionText}</span></label>")]
        internal string TemplateSelectFieldOption { get; }
        
        [DefaultValue(@"<div id='container_{fieldId}' class='field-container'>
    <label>
        <input type='checkbox' id='{fieldId}' name='{fieldId}' {required} {checked} value='1' />
        <span class='label'>{label}</span>
    </label>
</div>")]
        internal string TemplateCheckboxField { get; }

        [DefaultValue(@"<div id='progress'>
    {steps}
</div>")]
        internal string TemplateProgress { get; }

        [DefaultValue(@"<div class='progress-step' id='progressStep{number}' data-active-step='{activeStep}'>
    <span class='number {active}'>{number}</span>
    <span class='name'>{name}</span>
</div>")]
        internal string TemplateProgressStep { get; }

        [DefaultValue(@"<label>
    <input type='radio' id='paymentMethod_{id}' value='{id}' name='paymentMethod' />
    <img class='logo' src='{logo}' alt='{title}' />
    <span class='label'>{title}</span>
</label>")]
        internal string TemplatePaymentMethod { get; }

        #endregion
    }
}
