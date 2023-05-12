using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueStepDataModel
{
    /// <summary>
    /// Gets or sets the position of the step as a dash separated string.
    /// </summary>
    [JsonProperty("position")]
    public string Position { get; set; }

    /// <summary>
    /// Gets or sets the name of the step.
    /// </summary>
    [JsonProperty("stepName")]
    public string StepName { get; set; }

    /// <summary>
    /// Gets or sets the step's options. While it can contain any number of properties, the following are required: "id", "value", "name".
    /// </summary>
    [JsonProperty("options")]
    public IEnumerable<Dictionary<string, object>> Options { get; set; }

    /// <summary>
    /// Gets or sets the step's dependencies. The dependencies determine the step's visibility.
    /// </summary>
    [JsonProperty("dependencies")]
    public IEnumerable<VueStepDependencyModel> Dependencies { get; set; }

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
    /// Gets or sets whether the step is required.
    /// </summary>
    [JsonProperty("isRequired")]
    public bool IsRequired { get; set; }

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
    /// Gets or sets the query that retrieves the step data and step options data.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it is not needed in the client-side and because it would be a security risk.
    /// </remarks>
    [JsonIgnore]
    public string DataQuery { get; set; }

    #endregion
}