using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueStepDataModel
{
    /// <summary>
    /// Gets or sets the step's Wiser ID.
    /// </summary>
    [JsonProperty("stepId")]
    public ulong StepId { get; set; }

    /// <summary>
    /// Gets or sets the parent step ID.
    /// </summary>
    [JsonProperty("parentStepId")]
    public ulong ParentStepId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the step.
    /// </summary>
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the position of the step.
    /// </summary>
    [JsonProperty("position")]
    public string Position { get; set; }

    /// <summary>
    /// Gets or sets the name of the step.
    /// </summary>
    [JsonProperty("stepName")]
    public string StepName { get; set; }

    /// <summary>
    /// Gets or sets whether the step is available. This supersedes the step's dependencies.
    /// </summary>
    [JsonProperty("available")]
    public bool Available { get; set; } = true;

    /// <summary>
    /// Gets or sets the step's options. While it can contain any number of properties, the following are required: "id", "value", "name".
    /// </summary>
    [JsonProperty("options")]
    public IEnumerable<VueStepOptionDataModel> Options { get; set; }

    /// <summary>
    /// Gets or sets the step's dependencies. The dependencies determine the step's visibility.
    /// </summary>
    [JsonProperty("dependencies")]
    public IEnumerable<VueStepDependencyModel> Dependencies { get; set; }

    /// <summary>
    /// Gets or sets whether the step is required.
    /// </summary>
    [JsonProperty("isRequired")]
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether the step is required only when there are options available.
    /// </summary>
    [JsonProperty("isRequiredOnlyWithOptions")]
    public bool IsRequiredOnlyWithOptions { get; set; }

    /// <summary>
    /// Gets or sets the minimum value for the step. This is used for validation.
    /// </summary>
    /// <remarks>
    /// This is only used for steps that have a numeric value.
    /// </remarks>
    [JsonProperty("minimumValue")]
    public string MinimumValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for the step. This is used for validation.
    /// </summary>
    /// <remarks>
    /// This is only used for steps that have a numeric value.
    /// </remarks>
    [JsonProperty("maximumValue")]
    public string MaximumValue { get; set; }

    /// <summary>
    /// Gets or sets the validation regex for the step. This is used for validation.
    /// </summary>
    /// <remarks>
    /// This is only used for steps that have a custom input value.
    /// </remarks>
    [JsonProperty("validationRegex")]
    public string ValidationRegex { get; set; }

    /// <summary>
    /// Gets or sets the error message to display when the step is required and the user has not selected an option.
    /// </summary>
    [JsonProperty("requiredErrorMessage")]
    public string RequiredErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message to display when the step's value is less than the minimum value.
    /// </summary>
    [JsonProperty("minimumValueErrorMessage")]
    public string MinimumValueErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message to display when the step's value is greater than the maximum value.
    /// </summary>
    [JsonProperty("maximumValueErrorMessage")]
    public string MaximumValueErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message to display when the step's value does not match the validation regex.
    /// </summary>
    [JsonProperty("validationRegexErrorMessage")]
    public string ValidationRegexErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the conditions that must be met in order for the step to be required.
    /// </summary>
    [JsonProperty("requiredConditions")]
    public IEnumerable<VueStepDependencyModel> RequiredConditions { get; set; }

    /// <summary>
    /// Gets or sets the current value of the step.
    /// </summary>
    [JsonProperty("value")]
    public string CurrentValue { get; set; }

    /// <summary>
    /// Gets or sets the display name of the value of the step.
    /// </summary>
    [JsonProperty("valueDisplayName")]
    public string CurrentValueDisplayName { get; set; }

    /// <summary>
    /// Gets or sets whether the step's options should open a modal when selected.
    /// </summary>
    [JsonProperty("optionsOpenModal")]
    public bool OptionsOpenModal { get; set; }

    /// <summary>
    /// Gets or sets the content (HTML) of the modal.
    /// </summary>
    [JsonProperty("modalContent")]
    public string ModalContent { get; set; }

    /// <summary>
    /// Gets or sets the query selector for the modal container.
    /// </summary>
    [JsonProperty("modalContainerSelector")]
    public string ModalContainerSelector { get; set; }

    /// <summary>
    /// Gets or sets the datasource type being used for the step.
    /// </summary>
    [JsonProperty("datasource")]
    public string Datasource { get; set; }
    
    /// <summary>
    /// Gets or sets whether the step needs to send its answer to an API.
    /// </summary>
    [JsonProperty("isApiAnswer")]
    public bool IsApiAnswer { get; set; }

    /// <summary>
    /// Gets or sets extra data which are retrieved from the database.
    /// </summary>
    [JsonProperty("extraData")]
    public IDictionary<string, JToken> ExtraData = new Dictionary<string, JToken>();

    #region Server-side only properties

    /// <summary>
    /// Gets or sets the HTML template for the step.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string StepTemplate { get; set; }

    /// <summary>
    /// Gets or sets the HTML template for a step option.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string StepOptionTemplate { get; set; }

    /// <summary>
    /// Gets or sets the query that retrieves the step's extension data.
    /// </summary>
    [JsonIgnore]
    public string ExtraDataQuery { get; set; }

    /// <summary>
    /// Gets or sets the regular expression that is used to determine whether the step is visible based on the current URL.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string UrlRegex { get; set; }

    #endregion
}