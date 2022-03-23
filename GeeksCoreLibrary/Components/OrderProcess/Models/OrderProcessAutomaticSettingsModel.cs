using System.ComponentModel;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    internal class OrderProcessAutomaticSettingsModel
    {
        #region Tab layout properties

        [DefaultValue(@"<h1>Bestelproces</h1>
{progress}
<div id='steps'>{steps}</div>")]
        internal string Template { get; }

        [DefaultValue(@"<h2>{title}</h2>
<div id='step_{title:Seo}'>
    {groups}
</div>")]
        internal string TemplateStep { get; }

        [DefaultValue(@"<fieldset>
    <legend>{title}</legend>
    {fields}
</fieldset>")]
        internal string TemplateGroup { get; }

        [DefaultValue(@"<div id='container_{formfieldid}' class='field-container'>
    [if({formfieldlabel}!)]<label for='{formfieldid}'>{formfieldlabel}</label>[endif]
    <input type='{formfieldinputtype}' id='{formfieldid}' name='{formfieldid}' placeholder='{formfieldplaceholder}' {required} pattern='{formfieldregexcheck}' value='{value}' />
</div>")]
        internal string TemplateInputField { get; }

        [DefaultValue(@"<div id='container_{formfieldid}' class='field-container'>
    [if({formfieldlabel}!)]<label>{formfieldlabel}</label>[endif]
    {options}
</div>")]
        internal string TemplateRadioButtonField { get; }
        
        [DefaultValue(@"<label>
    <input type='radio' id='{formfieldid}_{option}' name='{formfieldid}' value='{option}' {required} {checked} />
    <span class='label'>{option}</span>
</label>")]
        internal string TemplateRadioButtonFieldOption { get; }
        
        [DefaultValue(@"<div id='container_{formfieldid}' class='field-container'>
    [if({formfieldlabel}!)]<label for='{formfieldid}'>{formfieldlabel}</label>[endif]
    <select id='{formfieldid}' name='{formfieldid}' {required}>
        {options}
    </select>
</div>")]
        internal string TemplateSelectField { get; }
        
        [DefaultValue(@"<option value='{formfieldid}_{option}' name='{formfieldid}' {required} {checked} /> <span class='label'>{option}</span></label>")]
        internal string TemplateSelectFieldOption { get; }
        
        [DefaultValue(@"<div id='container_{formfieldid}' class='field-container'>
    <label>
        <input type='checkbox' id='{formfieldid}' name='{formfieldid}' {required} {checked} value='1' />
        <span class='label'>{formfieldlabel}</span>
    </label>
</div>")]
        internal string TemplateCheckboxField { get; }

        [DefaultValue(@"<div id='progress'>
    {steps}
</div>")]
        internal string TemplateProgress { get; }

        [DefaultValue(@"<div class='progress-step' id='progressStep{number}'>
    <span class='number'>{number}</span>
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
