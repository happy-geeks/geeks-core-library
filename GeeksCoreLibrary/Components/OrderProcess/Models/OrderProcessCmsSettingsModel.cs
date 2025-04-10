using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.OrderProcess.Models;

public class OrderProcessCmsSettingsModel : CmsSettings
{
    public OrderProcess.ComponentModes ComponentMode { get; set; } = OrderProcess.ComponentModes.Checkout;

    #region Tab DataSource properties

    /// <summary>
    /// The Wiser item ID of the order process that should be retrieved.
    /// </summary>
    [CmsProperty(
        PrettyName = "Order process item ID",
        Description = "The Wiser item ID of the order process that should be retrieved.",
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.DataSource,
        GroupName = CmsAttributes.CmsGroupName.Basic,
        DisplayOrder = 10
    )]
    public ulong OrderProcessId { get; set; }

    #endregion

    #region Tab layout properties

    /// <summary>
    /// The main template.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template",
        Description = "The main template. You can use the variables '{progress}' (which will be replaced by TemplateProgress) and '{step}' (which will be replaced by TemplateStep for the active step).",
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 10
    )]
    public string Template { get; set; }

    /// <summary>
    /// The template for a step in the order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template step",
        Description = """
                      The template for a step in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the Wiser item with the settings for the step.</li>
                          <li><strong>{title}</strong> The title of the Wiser item with the settings for the step.</li>
                          <li><strong>{error}</strong> If any error occurred in this step, this will be replaced by the value of 'TemplateStepError', otherwise it will be replaced by an empty string. If this variable is not found in the template and an error occurs, then only the contents of 'TemplateStepError' wil be shown (which means that no fields will be visible on the page).</li>
                          <li><strong>{header}</strong> The header of the step, which can be edited by the customer in the Wiser item for the step.</li>
                          <li><strong>{groups}</strong> The groups of this step, will be replaced by TemplateGroup for each step.</li>
                          <li><strong>{footer}</strong> The footer of the step, which can be edited by the customer in the Wiser item for the step.</li>
                          <li><strong>{activeStep}</strong> The number of the active step.</li>
                          <li><strong>{confirmButtonText}</strong> The text for the button to go to the next step. This will be retrieved from the translations module from Wiser.</li>
                          <li><strong>{previousStepLinkText}</strong> The text on the link to go back to the previous step.</li>
                          <li><strong>{previousStepUrl}</strong> The URL of the previous step.</li>
                          <li><strong>{type}</strong> The type of step. Can be 'GroupsAndFields', 'Summary', 'OrderConfirmation' or 'orderPending'.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 20
    )]
    public string TemplateStep { get; set; }

    /// <summary>
    /// The HTML template that will be shown after a failed action.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template step error",
        Description = """
                      The HTML template that will be shown after a failed action. You can use the variable '{errorType}' to check which error was thrown. We have the following error types available:
                      <ul>
                          <li><strong>Server:</strong> This is any exception that occurred on the server. The actual exception will be written to the logs.</li>
                          <li><strong>Client:</strong> This is any error from the client, when they entered invalid or no values in required fields. This is just to show a generic message that they can't procedure, specific errors will be shown near the field(s) with the problem(s).</li>
                          <li><strong>Payment:</strong> This is any error that occurred during the payment. This could be that the user cancelled their payment, or the API of the PSP returned an error etc.</li>
                      </ul>
                      """,
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 25
    )]
    public string TemplateStepError { get; set; }

    /// <summary>
    /// The template for a group of fields in the order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template fields group",
        Description = """
                      The template for a group of fields in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the Wiser item with the settings for the group.</li>
                          <li><strong>{title}</strong> The title of the Wiser item with the settings for the group.</li>
                          <li><strong>{header}</strong> The header of the group, which can be edited by the customer in the Wiser item for the group.</li>
                          <li><strong>{fields}</strong> The fields of this group, will be replaced by the templates for the different fields.</li>
                          <li><strong>{footer}</strong> The footer of the group, which can be edited by the customer in the Wiser item for the group.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 30
    )]
    public string TemplateFieldsGroup { get; set; }

    /// <summary>
    /// The template for a group of payment methods in the order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template payment methods group",
        Description = """
                      The template for a group of payment methods in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the Wiser item with the settings for the group.</li>
                          <li><strong>{title}</strong> The title of the Wiser item with the settings for the group.</li>
                          <li><strong>{header}</strong> The header of the group, which can be edited by the customer in the Wiser item for the group.</li>
                          <li><strong>{paymentMethods}</strong> The fields of this group, will be replaced by the templates for the different fields.</li>
                          <li><strong>{footer}</strong> The footer of the group, which can be edited by the customer in the Wiser item for the group.</li>
                          <li><strong>{groupClass}</strong> Any extra CSS classes for the group, which can be edited by the customer in the Wiser item for the group.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 35
    )]
    public string TemplatePaymentMethodsGroup { get; set; }

    /// <summary>
    /// The HTML template that will be shown with a field when there was a validation error for that field.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template field error",
        Description = "The HTML template that will be shown with a field when there was a validation error for that field. You can use the variable '{errorMessage}' on the location where you want to show the message.",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        DisplayOrder = 35
    )]
    public string TemplateFieldError { get; set; }

    /// <summary>
    /// The template for a normal input field in the order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template input field",
        Description = """
                      The template for a normal input field in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the Wiser item with the settings for the field.</li>
                          <li><strong>{title}</strong> The title of the Wiser item with the settings for the field.</li>
                          <li><strong>{error}</strong> If a validation error occurred with this field, this variable will be replaced by the value of 'TemplateFieldError', otherwise it will be replaced by an empty string.</li>
                          <li><strong>{errorClass}</strong> If a validation error occurred with this field, this variable will be replaced by the literal text 'error', otherwise it will be replaced by en empty string.</li>
                          <li><strong>{fieldId}</strong> The ID of the field as it's set in the settings for the field. This value should be used in the 'name' and 'id' attributes of the input and the 'for' attribute of the label.</li>
                          <li><strong>{label}</strong> The label for this field.</li>
                          <li><strong>{inputType}</strong> The input type of this field, this value should be used in the 'type' attribute of the input.</li>
                          <li><strong>{placeholder}</strong> The placeholder for the field, should be used in the 'placeholder' attribute of the input.</li>
                          <li><strong>{required}</strong> This will be replaced with the 'required' attribute if the field is required, or with an empty string if it isn't.</li>
                          <li><strong>{pattern}</strong> The regex validation pattern for the field. This will be replaced with the entire attribute (pattern='the pattern') if there is a pattern, or with an empty string if there isn't.</li>
                          <li><strong>{value}</strong> The current value of the field, retrieved from the basket or logged in user, or POST variables.</li>
                          <li><strong>{fieldClass}</strong> Any extra CSS classes that are set in the settings of the a field in Wiser.</li>
                          <li><strong>{tabIndex}</strong> The tabindex attribute with the value that is set in the settings of this field in Wiser. If no tab index has been set in Wiser, this will be replaced by an empty string.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 40
    )]
    public string TemplateInputField { get; set; }

    /// <summary>
    /// The template for a normal input field in the order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template textarea field",
        Description = """
                      The template for a textarea field in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the Wiser item with the settings for the field.</li>
                          <li><strong>{title}</strong> The title of the Wiser item with the settings for the field.</li>
                          <li><strong>{error}</strong> If a validation error occurred with this field, this variable will be replaced by the value of 'TemplateFieldError', otherwise it will be replaced by an empty string.</li>
                          <li><strong>{errorClass}</strong> If a validation error occurred with this field, this variable will be replaced by the literal text 'error', otherwise it will be replaced by en empty string.</li>
                          <li><strong>{fieldId}</strong> The ID of the field as it's set in the settings for the field. This value should be used in the 'name' and 'id' attributes of the input and the 'for' attribute of the label.</li>
                          <li><strong>{label}</strong> The label for this field.</li>
                          <li><strong>{placeholder}</strong> The placeholder for the field, should be used in the 'placeholder' attribute of the input.</li>
                          <li><strong>{required}</strong> This will be replaced with the 'required' attribute if the field is required, or with an empty string if it isn't.</li>
                          <li><strong>{pattern}</strong> The regex validation pattern for the field. This will be replaced with the entire attribute (pattern='the pattern') if there is a pattern, or with an empty string if there isn't.</li>
                          <li><strong>{value}</strong> The current value of the field, retrieved from the basket or logged in user, or POST variables.</li>
                          <li><strong>{fieldClass}</strong> Any extra CSS classes that are set in the settings of the a field in Wiser.</li>
                          <li><strong>{tabIndex}</strong> The tabindex attribute with the value that is set in the settings of this field in Wiser. If no tab index has been set in Wiser, this will be replaced by an empty string.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 45
    )]
    public string TemplateTextareaField { get; set; }

    /// <summary>
    /// The template for a radio button field in the order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template radio button field",
        Description = """
                      The template for a radio button field in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the Wiser item with the settings for the field.</li>
                          <li><strong>{title}</strong> The title of the Wiser item with the settings for the field.</li>
                          <li><strong>{error}</strong> If a validation error occurred with this field, this variable will be replaced by the value of 'TemplateFieldError', otherwise it will be replaced by an empty string.</li>
                          <li><strong>{errorClass}</strong> If a validation error occurred with this field, this variable will be replaced by the literal text 'error', otherwise it will be replaced by en empty string.</li>
                          <li><strong>{fieldId}</strong> The ID of the field as it's set in the settings for the field. This value should be used in the 'name' and 'id' attributes of the input and the 'for' attribute of the label.</li>
                          <li><strong>{label}</strong> The label for this field.</li>
                          <li><strong>{placeholder}</strong> The placeholder for the field, should be used in the 'placeholder' attribute of the input.</li>
                          <li><strong>{value}</strong> The current value of the field, retrieved from the basket or logged in user, or POST variables.</li>
                          <li><strong>{options}</strong> The options for the radio button.</li>
                          <li><strong>{fieldClass}</strong> Any extra CSS classes that are set in the settings of the a field in Wiser.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 50
    )]
    public string TemplateRadioButtonField { get; set; }

    /// <summary>
    /// The template for a single option in a radio button field in the order process
    /// </summary>
    [CmsProperty(
        PrettyName = "Template radio button option",
        Description = """
                      The template for a single option in a radio button field in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{fieldId}</strong> The ID of the field as it's set in the settings for the field. This value should be used in the 'name' and 'id' attributes of the input and the 'for' attribute of the label.</li>
                          <li><strong>{required}</strong> This will be replaced with the 'required' attribute if the field is required, or with an empty string if it isn't.</li>
                          <li><strong>{checked}</strong> Whether this option should be checked, retrieved from the basket or logged in user, or POST variables. This will be replaced with the 'checked' attribute if it should, or an empty string if it shouldn't.</li>
                          <li><strong>{optionText}</strong> The text for the option that the user should see.</li>
                          <li><strong>{optionValue}</strong> The value of the option that should be saved to database.</li>
                          <li><strong>{tabIndex}</strong> The tabindex attribute with the value that is set in the settings of this field in Wiser. If no tab index has been set in Wiser, this will be replaced by an empty string.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 55
    )]
    public string TemplateRadioButtonFieldOption { get; set; }

    /// <summary>
    /// The template for a select / combobox field in the order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template select field",
        Description = """
                      The template for a select / combobox field in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the Wiser item with the settings for the field.</li>
                          <li><strong>{title}</strong> The title of the Wiser item with the settings for the field.</li>
                          <li><strong>{error}</strong> If a validation error occurred with this field, this variable will be replaced by the value of 'TemplateFieldError', otherwise it will be replaced by an empty string.</li>
                          <li><strong>{errorClass}</strong> If a validation error occurred with this field, this variable will be replaced by the literal text 'error', otherwise it will be replaced by en empty string.</li>
                          <li><strong>{fieldId}</strong> The ID of the field as it's set in the settings for the field. This value should be used in the 'name' and 'id' attributes of the input and the 'for' attribute of the label.</li>
                          <li><strong>{required}</strong> This will be replaced with the 'required' attribute if the field is required, or with an empty string if it isn't.</li>
                          <li><strong>{label}</strong> The label for this field.</li>
                          <li><strong>{value}</strong> The current value of the field, retrieved from the basket or logged in user, or POST variables.</li>
                          <li><strong>{options}</strong> The options for the radio button.</li>
                          <li><strong>{fieldClass}</strong> Any extra CSS classes that are set in the settings of the a field in Wiser.</li>
                          <li><strong>{tabIndex}</strong> The tabindex attribute with the value that is set in the settings of this field in Wiser. If no tab index has been set in Wiser, this will be replaced by an empty string.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 60
    )]
    public string TemplateSelectField { get; set; }

    /// <summary>
    /// The template for a single option in a select field in the order process
    /// </summary>
    [CmsProperty(
        PrettyName = "Template select option",
        Description = """
                      The template for a single option in a select field in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{fieldId}</strong> The ID of the field as it's set in the settings for the field. This value should be used in the 'name' and 'id' attributes of the input and the 'for' attribute of the label.</li>
                          <li><strong>{required}</strong> This will be replaced with the 'required' attribute if the field is required, or with an empty string if it isn't.</li>
                          <li><strong>{selected}</strong> Whether this option should be selected, retrieved from the basket or logged in user, or POST variables. This will be replaced with the 'selected' attribute if it should, or an empty string if it shouldn't.</li>
                          <li><strong>{optionText}</strong> The text for the option that the user should see.</li>
                          <li><strong>{optionValue}</strong> The value of the option that should be saved to database.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 65
    )]
    public string TemplateSelectFieldOption { get; set; }

    /// <summary>
    /// The template for a checkbox field in the order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template checkbox field",
        Description = """
                      The template for a checkbox field in the order process. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the Wiser item with the settings for the field.</li>
                          <li><strong>{title}</strong> The title of the Wiser item with the settings for the field.</li>
                          <li><strong>{error}</strong> If a validation error occurred with this field, this variable will be replaced by the value of 'TemplateFieldError', otherwise it will be replaced by an empty string.</li>
                          <li><strong>{errorClass}</strong> If a validation error occurred with this field, this variable will be replaced by the literal text 'error', otherwise it will be replaced by en empty string.</li>
                          <li><strong>{fieldId}</strong> The ID of the field as it's set in the settings for the field. This value should be used in the 'name' and 'id' attributes of the input and the 'for' attribute of the label.</li>
                          <li><strong>{label}</strong> The label for this field.</li>
                          <li><strong>{required}</strong> This will be replaced with the 'required' attribute if the field is required, or with an empty string if it isn't.</li>
                          <li><strong>{checked}</strong> Whether this option should be checked, retrieved from the basket or logged in user, or POST variables. This will be replaced with the 'checked' attribute if it should, or an empty string if it shouldn't.</li>
                          <li><strong>{fieldClass}</strong> Any extra CSS classes that are set in the settings of the a field in Wiser.</li>
                          <li><strong>{tabIndex}</strong> The tabindex attribute with the value that is set in the settings of this field in Wiser. If no tab index has been set in Wiser, this will be replaced by an empty string.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 70
    )]
    public string TemplateCheckboxField { get; set; }

    /// <summary>
    /// The template for showing the progress of the user in a multi step order process.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template progress",
        Description = "The template for showing the progress of the user in a multi step order process. The variable '{steps}' can be used here to render the steps on that location.",
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 80
    )]
    public string TemplateProgress { get; set; }

    /// <summary>
    /// The template for a single step for 'TemplateProgress'.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template progress step",
        Description = """
                      The template for a single step for 'TemplateProgress'. You can use the following variables here:
                      <ul>
                          <li><strong>{number}</strong> The number of the step. This is not the active step, but a different number for each step in order (1,2,3 etc).</li>
                          <li><strong>{activeStep}</strong> The number of the step that is currently active.</li>
                          <li><strong>{active}</strong> This will be replaced by the value 'active' if the current step is the active step, or an empty string if it isn't. This can be used to add the 'active' CSS class the the element of the active step.</li>
                          <li><strong>{name}</strong> The name of the step.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 80
    )]
    public string TemplateProgressStep { get; set; }

    /// <summary>
    /// The template for a single payment method.
    /// </summary>
    [CmsProperty(
        PrettyName = "Template payment method",
        Description = """
                      The template for a single payment method. You can use the following variables here:
                      <ul>
                          <li><strong>{id}</strong> The ID of the payment method.</li>
                          <li><strong>{title}</strong> This name of the payment method.</li>
                          <li><strong>{logoPropertyName}</strong> The name of the field in wiser where the logo is saved for the payment method.</li>
                          <li><strong>{paymentMethodFieldName}</strong> The name of the input element for the selected payment method. Don't use any other name, otherwise the GCL will not know which payment method the user selected and throw an exception.</li>
                          <li><strong>{checked}</strong> Whether this option should be checked, retrieved from the basket or logged in user, or POST variables. This will be replaced with the 'checked' attribute if it should, or an empty string if it shouldn't.</li>
                      </ul>
                      """,
        DeveloperRemarks = "",
        TabName = CmsAttributes.CmsTabName.Layout,
        GroupName = CmsAttributes.CmsGroupName.Templates,
        TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
        ComponentMode = "Checkout",
        DisplayOrder = 90
    )]
    public string TemplatePaymentMethod { get; set; }

    #endregion
}