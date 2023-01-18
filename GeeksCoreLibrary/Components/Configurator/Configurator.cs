using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Interfaces;
using GeeksCoreLibrary.Components.Configurator.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace GeeksCoreLibrary.Components.Configurator
{
    [CmsObject(
        PrettyName = "Configurator",
        Description = "Component for generating a configurator based on settings from the configurator module"
    )]
    public class Configurator : CmsComponent<ConfiguratorCmsSettingsModel, Configurator.LegacyComponentMode>
    {
        #region Enums
        /// <summary>
        /// Modes that the <see cref="Configurator"/> component can be in.
        /// </summary>
        public enum ComponentModes
        {
            /// <summary>
            ///  A default configurator with main steps, steps, substeps and a summary.
            /// </summary>
            Default = 1
        }

        public enum LegacyComponentMode
        {
            NonLegacy,
            Default
        }

        #endregion

        #region Private fields

        private readonly IConfiguratorsService configuratorsService;
        private readonly IDataSelectorsService dataSelectorsService;
        private readonly IObjectsService objectsService;
        private readonly IWiserItemsService wiserItemsService;

        private readonly Dictionary<string, Dictionary<string, Tuple<string, Dictionary<string, string>>>> stepNumbers = new();

        #endregion

        #region Constructor
        public static LegacyComponentMode ParseComponentMode(string dynamicContentName)
        {
            return dynamicContentName switch
            {
                "Configurator" => LegacyComponentMode.NonLegacy,
                "JuiceControlLibrary.Configurator" => LegacyComponentMode.Default,
                _ => throw new ArgumentOutOfRangeException(nameof(dynamicContentName)),
            };
        }

        public Configurator(ILogger<Configurator> logger, IStringReplacementsService stringReplacementsService, IDatabaseConnection databaseConnection, IConfiguratorsService configuratorsService, IDataSelectorsService dataSelectorsService, ITemplatesService templatesService, IObjectsService objectsService, IWiserItemsService wiserItemsService)
        {
            this.configuratorsService = configuratorsService;
            this.dataSelectorsService = dataSelectorsService;
            this.objectsService = objectsService;
            this.wiserItemsService = wiserItemsService;
            Logger = logger;
            StringReplacementsService = stringReplacementsService;
            DatabaseConnection = databaseConnection;
            Settings = new ConfiguratorCmsSettingsModel();
            TemplatesService = templatesService;
        }
        #endregion

        #region Handling settings
        /// <inheritdoc />
        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            Settings = LegacyMode switch
            {
                LegacyComponentMode.NonLegacy => JsonConvert.DeserializeObject<ConfiguratorCmsSettingsModel>(settingsJson),
                LegacyComponentMode.Default => JsonConvert.DeserializeObject<ConfiguratorLegacySettingsModel>(settingsJson)?.ToSettingsModel(),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (Settings != null && forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
        }

        /// <inheritdoc />
        public override string GetSettingsJson()
        {
            return LegacyMode switch
            {
                LegacyComponentMode.NonLegacy => JsonConvert.SerializeObject(Settings),
                LegacyComponentMode.Default => JsonConvert.SerializeObject(new ConfiguratorLegacySettingsModel().FromSettingModel(Settings)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        #endregion

        #region Rendering

        /// <inheritdoc />
        public override async Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData)
        {
            ComponentId = dynamicContent.Id;
            LegacyMode = ParseComponentMode(dynamicContent.Name);
            ExtraDataForReplacements = extraData;
            ParseSettingsJson(dynamicContent.SettingsJson, forcedComponentMode);

            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }

            HandleDefaultSettingsFromComponentMode();

            // Check if we should actually render this component for the current user.
            var (renderHtml, debugInformation) = await ShouldRenderHtmlAsync();
            if (!renderHtml)
            {
                ViewBag.Html = debugInformation;
                return new HtmlString(debugInformation);
            }

            // Check if we need to call a specific method and then do so. Skip everything else, because we don't want to render the entire component then.
            if (!String.IsNullOrWhiteSpace(callMethod))
            {
                TempData["InvokeMethodResult"] = await InvokeMethodAsync(callMethod);
                return new HtmlString("");
            }

            var mainStepCount = 1;
            var stepCount = 0;
            var subStepCount = 0;

            var currentMainStepName = "";
            var currentStepName = "";

            // String builders for building the output HTML.
            var allStepsHtml = new StringBuilder();
            var currentMainStepHtml = new StringBuilder();
            var currentStepHtml = new StringBuilder();
            var currentStepsHtml = new StringBuilder();

            //var currentSubStepsHtml = new StringBuilder();
            var currentSubSteps = new List<SubStepHtmlModel>();

            var currentConfiguratorName = StringReplacementsService.DoHttpRequestReplacements(Settings.ConfiguratorName).ToLowerInvariant();
            // Deze regex haalt alle niet vervangen {} weg.
            currentConfiguratorName = Regex.Replace(currentConfiguratorName, "{[^}]*}", "");

            WriteToTrace($"currentConfiguratorName: {currentConfiguratorName}");
            if (String.IsNullOrWhiteSpace(currentConfiguratorName))
            {
                WriteToTrace("No configuration name found, so not rendering anything!", true);
                return new HtmlString("");
            }

            var configuratorData = await configuratorsService.GetConfiguratorDataAsync(currentConfiguratorName);
            var firstRow = configuratorData.Rows[0];

            // Basic templates.jjl_summary_template
            var progressHtml = Settings.SummaryHtml
                .ReplaceCaseInsensitive("{progress_template}", firstRow.Field<string>("progress_template"))
                .ReplaceCaseInsensitive("{progress_step_template}", firstRow.Field<string>("progress_step_template"))
                .ReplaceCaseInsensitive("{progress_substep_template}", firstRow.Field<string>("progress_substep_template"));
            var renderedFinalSummaryHtml = Settings.FinalSummaryHtml
                .ReplaceCaseInsensitive("{summary_template}", firstRow.Field<string>("summary_template"))
                .ReplaceCaseInsensitive("{summary_mainstep_template}", firstRow.Field<string>("summary_mainstep_template"))
                .ReplaceCaseInsensitive("{summary_step_template}", firstRow.Field<string>("summary_step_template"));

            // First add all step numbers to the "_stepNumbers" dictionary, so that we have information about all steps before we start generating HTML.
            // If we don't do this, we can't make a step dependent on a future step.
            await LoadStepNumbersAsync(currentConfiguratorName);

            var preRenderStepsQuery = firstRow.Field<string>("pre_render_steps_query");
            WriteToTrace($"preRenderStepsQuery 1: {preRenderStepsQuery}");

            if (!String.IsNullOrWhiteSpace(preRenderStepsQuery))
            {
                await DatabaseConnection.ExecuteAsync(preRenderStepsQuery);
            }

            // Regex to find any '{substeps}' variables, including ones that define the sub step IDs.
            var subStepsRegex = new Regex("\\{substeps(?:\\|(?<ids>.*?))?\\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            foreach (DataRow row in configuratorData.Rows)
            {
                var currentUrl = HttpContextHelpers.GetBaseUri(HttpContext).AbsoluteUri;
                var urlRegex = row.Field<string>("urlregex");
                if (!String.IsNullOrWhiteSpace(urlRegex) && !Regex.IsMatch(currentUrl, urlRegex))
                {
                    continue;
                }

                // Only render main step, step or sub step if URL regex is filled and a match.
                if (row.Field<string>("mainstepname") != currentMainStepName)
                {
                    // This is a new main step.
                    if (currentMainStepHtml.Length > 0)
                    {
                        // Add the last rendered step to the template
                        if (currentStepHtml.Length > 0)
                        {
                            var regexMatches = subStepsRegex.Matches(currentStepHtml.ToString()).ToList();

                            if (regexMatches.Count > 0)
                            {
                                foreach (var match in regexMatches)
                                {
                                    if (!String.IsNullOrWhiteSpace(match.Groups["ids"].Value))
                                    {
                                        var subStepValues = match.Groups["ids"].Value.Split('|');

                                        var currentSubStepsHtml = new StringBuilder();
                                        foreach (var subStepValue in subStepValues)
                                        {
                                            // Get sub step by either ID or name.
                                            var subStep = UInt64.TryParse(subStepValue, out var subStepId)
                                                ? currentSubSteps.FirstOrDefault(subStep => subStep.Id == subStepId)
                                                : currentSubSteps.FirstOrDefault(subStep => subStep.Name.Equals(subStepValue, StringComparison.OrdinalIgnoreCase));

                                            if (subStep == null) continue;

                                            currentSubStepsHtml.Append(subStep.Html);
                                        }

                                        currentStepsHtml.Append(currentStepHtml.Replace(match.Value, currentSubStepsHtml.ToString()));
                                    }
                                    else
                                    {
                                        var currentSubStepsHtml = String.Join("", currentSubSteps.Select(subStep => subStep.Html));
                                        currentStepsHtml.Append(currentStepHtml.Replace(match.Value, currentSubStepsHtml));
                                    }
                                }
                            }
                            else
                            {
                                // The HTML doesn't contain any "{substeps}" variables.
                                currentStepsHtml.Append(currentStepHtml);
                            }

                            currentStepHtml.Clear();
                        }

                        allStepsHtml.Append(Settings.MainStepHtml
                            .ReplaceCaseInsensitive("{componentId}", ComponentId.ToString())
                            .ReplaceCaseInsensitive("{contentId}", ComponentId.ToString())
                            .ReplaceCaseInsensitive("{mainStepCount}", mainStepCount.ToString())
                            .ReplaceCaseInsensitive("{currentMainStepName}", currentMainStepName)
                            .ReplaceCaseInsensitive("{mainStepContent}", currentMainStepHtml.ToString()
                                .Replace("{steps}", currentStepsHtml.ToString())
                                .Replace("{progress}", progressHtml)));

                        mainStepCount += 1;
                    }

                    // Create the new step template and clear variables.
                    stepCount = 0;
                    currentMainStepName = row.Field<string>("mainstepname");

                    WriteToTrace($"Starting HTML for new main step. Main step #{mainStepCount}, name: {currentMainStepName}");
                    var currentMainStepTemplate = row.Field<string>("mainstep_template");

                    if (currentMainStepTemplate != null)
                    {
                        currentMainStepTemplate = currentMainStepTemplate.Replace("{stepnumber}", mainStepCount.ToString());
                        currentMainStepTemplate = currentMainStepTemplate.Replace("{stepname}", row.Field<string>("mainstepname"));
                        currentMainStepTemplate = await StringReplacementsService.DoAllReplacementsAsync(currentMainStepTemplate, row, removeUnknownVariables: false);

                        currentMainStepHtml = new StringBuilder(currentMainStepTemplate);
                    }

                    currentStepsHtml.Clear();
                    currentSubSteps.Clear();
                }

                if (row.Field<string>("stepname") != currentStepName)
                {
                    subStepCount = 0;
                    stepCount += 1;
                    currentStepName = row.Field<string>("stepname");

                    WriteToTrace($"Starting HTML for new step. Step #{stepCount}, name: {currentStepName}");

                    if (currentStepHtml.Length > 0)
                    {
                        var regexMatches = subStepsRegex.Matches(currentStepHtml.ToString()).ToList();

                        if (regexMatches.Count > 0)
                        {
                            foreach (var match in regexMatches)
                            {
                                if (!String.IsNullOrWhiteSpace(match.Groups["ids"].Value))
                                {
                                    var subStepValues = match.Groups["ids"].Value.Split('|');

                                    var currentSubStepsHtml = new StringBuilder();
                                    foreach (var subStepValue in subStepValues)
                                    {
                                        // Get sub step by either ID or name.
                                        var subStep = UInt64.TryParse(subStepValue, out var subStepId)
                                            ? currentSubSteps.FirstOrDefault(subStep => subStep.Id == subStepId)
                                            : currentSubSteps.FirstOrDefault(subStep => subStep.Name.Equals(subStepValue, StringComparison.OrdinalIgnoreCase));

                                        if (subStep == null) continue;

                                        currentSubStepsHtml.Append(subStep.Html);
                                    }

                                    currentStepsHtml.Append(currentStepHtml.Replace(match.Value, currentSubStepsHtml.ToString()));
                                }
                                else
                                {
                                    var currentSubStepsHtml = String.Join("", currentSubSteps.Select(subStep => subStep.Html));
                                    currentStepsHtml.Append(currentStepHtml.Replace(match.Value, currentSubStepsHtml));
                                }
                            }
                        }
                        else
                        {
                            // The HTML doesn't contain any "{substeps}" variables.
                            currentStepsHtml.Append(currentStepHtml);
                        }

                        currentStepHtml.Clear();
                    }

                    currentStepHtml.Append(await RenderStepAsync(currentConfiguratorName, row, mainStepCount, stepCount));

                    WriteToTrace($"1 - Starting HTML for new sub step. Sub step #{subStepCount}, name: {row.Field<string>("substepname")}");

                    currentSubSteps.Clear();
                    subStepCount += 1;
                    currentSubSteps.Add(new SubStepHtmlModel
                    {
                        Id = Convert.ToUInt64(row["subStepId"]),
                        Name = row.Field<string>("substepname"),
                        Html = await DoRenderingOfSubStepAsync(currentConfiguratorName, row, mainStepCount, stepCount, subStepCount)
                    });
                }
                else
                {
                    WriteToTrace($"2 - Starting HTML for new sub step. Sub step #{subStepCount}, name: {row.Field<string>("substepname")}");

                    subStepCount += 1;
                    currentSubSteps.Add(new SubStepHtmlModel
                    {
                        Id = Convert.ToUInt64(row["subStepId"]),
                        Name = row.Field<string>("substepname"),
                        Html = await DoRenderingOfSubStepAsync(currentConfiguratorName, row, mainStepCount, stepCount, subStepCount)
                    });
                }
            }

            // Add final main step.
            if (currentStepHtml.Length > 0)
            {
                var regexMatches = subStepsRegex.Matches(currentStepHtml.ToString()).ToList();

                if (regexMatches.Count > 0)
                {
                    foreach (var match in regexMatches)
                    {
                        if (!String.IsNullOrWhiteSpace(match.Groups["ids"].Value))
                        {
                            var subStepValues = match.Groups["ids"].Value.Split('|');

                            var currentSubStepsHtml = new StringBuilder();
                            foreach (var subStepValue in subStepValues)
                            {
                                // Get sub step by either ID or name.
                                var subStep = UInt64.TryParse(subStepValue, out var subStepId)
                                    ? currentSubSteps.FirstOrDefault(subStep => subStep.Id == subStepId)
                                    : currentSubSteps.FirstOrDefault(subStep => subStep.Name.Equals(subStepValue, StringComparison.OrdinalIgnoreCase));

                                if (subStep == null) continue;

                                currentSubStepsHtml.Append(subStep.Html);
                            }

                            currentStepsHtml.Append(currentStepHtml.Replace(match.Value, currentSubStepsHtml.ToString()));
                        }
                        else
                        {
                            var currentSubStepsHtml = String.Join("", currentSubSteps.Select(subStep => subStep.Html));
                            currentStepsHtml.Append(currentStepHtml.Replace(match.Value, currentSubStepsHtml));
                        }
                    }
                }
                else
                {
                    // The HTML doesn't contain any "{substeps}" variables.
                    currentStepsHtml.Append(currentStepHtml);
                }

                currentStepHtml.Clear();
            }

            allStepsHtml.Append(Settings.MainStepHtml
                .ReplaceCaseInsensitive("{mainStepCount}", mainStepCount.ToString())
                .ReplaceCaseInsensitive("{currentMainStepName}", currentMainStepName)
                .ReplaceCaseInsensitive("{mainStepContent}", currentMainStepHtml.ToString()
                    .ReplaceCaseInsensitive("{steps}", currentStepsHtml.ToString())
                    .ReplaceCaseInsensitive("{progress}", progressHtml)
                    .ReplaceCaseInsensitive("{summary}", renderedFinalSummaryHtml)));

            var renderedMobilePreProgressHtml = Settings.MobilePreProgressHtml
                .ReplaceCaseInsensitive("{progress_pre_template}", firstRow.Field<string>("progress_pre_template"))
                .ReplaceCaseInsensitive("{progress_pre_step_template}", firstRow.Field<string>("progress_pre_step_template"))
                .ReplaceCaseInsensitive("{progress_pre_substep_template}", firstRow.Field<string>("progress_pre_substep_template"));

            var renderedMobilePostProgressHtml = Settings.MobilePostProgressHtml
                .ReplaceCaseInsensitive("{progress_post_template}", firstRow.Field<string>("progress_post_template"))
                .ReplaceCaseInsensitive("{progress_post_step_template}", firstRow.Field<string>("progress_post_step_template"))
                .ReplaceCaseInsensitive("{progress_post_substep_template}", firstRow.Field<string>("progress_post_substep_template"));

            allStepsHtml.Replace("{progress_pre}", renderedMobilePreProgressHtml);
            allStepsHtml.Replace("{progress_post}", renderedMobilePostProgressHtml);

            var resultHtml = new StringBuilder();
            resultHtml.Append("<div id=\"configurator\" data-customParameter=\"{customParam}|{customParamDependencies}\">"
                .Replace("{customParam}", firstRow.Field<string>("custom_param_name"))
                .Replace("{customParamDependencies}", firstRow.Field<string>("custom_param_dependencies")));

            resultHtml.Append(await StringReplacementsService.DoAllReplacementsAsync(firstRow.Field<string>("template")
                .ReplaceCaseInsensitive("{mainsteps}", allStepsHtml.ToString())
                .ReplaceCaseInsensitive("{progress}", progressHtml)
                .ReplaceCaseInsensitive("{summary}", renderedFinalSummaryHtml)
                .ReplaceCaseInsensitive("{totalsteps}", mainStepCount.ToString())
                .ReplaceCaseInsensitive("{progress_post}", renderedMobilePostProgressHtml)
                .ReplaceCaseInsensitive("{progress_pre}", renderedMobilePreProgressHtml), removeUnknownVariables: false));

            resultHtml.Append("</div>");
            WriteToTrace("End generating HTML");
            return new HtmlString(await TemplatesService.DoReplacesAsync(resultHtml.ToString(), false, removeUnknownVariables: false));
        }

        /// <summary>
        /// First add all step numbers to the "_stepNumbers" dictionary, so that we have information about all steps before we start generating HTML. If we don't do this, we can't make a step dependent on a future step.
        /// </summary>
        /// <param name="configuratorName"></param>
        /// <returns></returns>
        private async Task LoadStepNumbersAsync(string configuratorName)
        {
            WriteToTrace("LoadStepNumbers");
            var mainStepCount = 0;
            var stepCount = 0;
            var subStepCount = 0;

            var currentMainStepName = "";
            var currentStepName = "";

            if (!stepNumbers.ContainsKey(configuratorName))
            {
                stepNumbers.Add(configuratorName, new Dictionary<string, Tuple<string, Dictionary<string, string>>>());
            }

            var configuratorData = await configuratorsService.GetConfiguratorDataAsync(configuratorName);
            foreach (DataRow row in configuratorData.Rows)
            {
                var currentUrl = HttpContextHelpers.GetBaseUri(HttpContext).AbsoluteUri;
                var urlRegex = row.Field<string>("urlregex");
                if (!String.IsNullOrWhiteSpace(urlRegex) && !Regex.IsMatch(currentUrl, urlRegex))
                {
                    continue;
                }

                // Only render main step, step or sub step if URL regex is filled and a match.
                if (row.Field<string>("mainstepname") != currentMainStepName)
                {
                    // This is a new main step.
                    currentMainStepName = row.Field<string>("mainstepname");
                    mainStepCount += 1;
                    stepCount = 1;
                }

                var variableName = row.Field<string>("variable_name");
                var subStepVariableName = row.Field<string>("substep_variable_name");
                if (row.Field<string>("stepname") != currentStepName)
                {
                    subStepCount = 1;
                    currentStepName = row.Field<string>("stepname");

                    if (!String.IsNullOrWhiteSpace(variableName) && !stepNumbers[configuratorName].ContainsKey(variableName))
                    {
                        stepNumbers[configuratorName].Add(variableName, new Tuple<string, Dictionary<string, string>>($"{mainStepCount}-{stepCount}", new Dictionary<string, string>()));
                    }

                    if (!String.IsNullOrWhiteSpace(variableName) && !String.IsNullOrWhiteSpace(subStepVariableName) && !stepNumbers[configuratorName][variableName].Item2.ContainsKey(subStepVariableName))
                    {
                        stepNumbers[configuratorName][variableName].Item2.Add(subStepVariableName, $"{mainStepCount}-{stepCount}-{subStepCount}");
                    }

                    // Always add one sub step
                    subStepCount += 1;
                    stepCount += 1;
                }
                else if (!String.IsNullOrWhiteSpace(row.Field<string>("substepname")))
                {
                    if (!String.IsNullOrWhiteSpace(variableName) && !String.IsNullOrWhiteSpace(subStepVariableName) && !stepNumbers[configuratorName][variableName].Item2.ContainsKey(subStepVariableName))
                    {
                        // We subtract one from step count, because the stepCount gets increased after adding the step, but that's too early for sub steps.
                        stepNumbers[configuratorName][variableName].Item2.Add(subStepVariableName, $"{mainStepCount}-{stepCount - 1}-{subStepCount}");
                    }
                    
                    subStepCount += 1;
                }
            }
        }

        /// <summary>
        /// render step
        /// </summary>
        /// <param name="currentConfiguratorName"></param>
        /// <param name="row"></param>
        /// <param name="mainStepNumber"></param>
        /// <param name="stepNumber"></param>
        /// <param name="dependentValue"></param>
        /// <param name="configurator"></param>
        /// <param name="subSteps"></param>
        /// <returns></returns>
        private async Task<string> RenderStepAsync(string currentConfiguratorName, DataRow row, int mainStepNumber, int stepNumber, string dependentValue = "-1", ConfigurationsModel configurator = null, List<DataRow> subSteps = null)
        {
            var connectedId = row.Field<string>("datasource_connectedid");

            // The replacement is because JS replaces choice-name with choiceName.
            var dataSourceConnectedIdName = connectedId?.Replace("-", "");

            // Get the correct id for rendering the values if this step is dependent on other steps.
            if (Int32.TryParse(connectedId, out var connectedIdNumber))
            {
                // If the parse is successful, the connected ID is a fixed value, like all products from category '5'.
                dependentValue = connectedIdNumber.ToString();
            }
            else if (dependentValue == "-1" && !String.IsNullOrEmpty(dataSourceConnectedIdName) && !String.IsNullOrEmpty(HttpContextHelpers.GetRequestValue(HttpContext, dataSourceConnectedIdName)))
            {
                // If the configurator is reloaded, then get the connected id from the URL.
                dependentValue = HttpContextHelpers.GetRequestValue(HttpContext, dataSourceConnectedIdName);
            }

            // Get the correct value if the value is in the format of 'number-1' or '1,2,3'.
            if (!Settings.ValuesCanContainDashes && dependentValue.Contains("-"))
            {
                // Value contains a - so get the value (x) from a input like 'value-x'.
                dependentValue = dependentValue.Split('-')[1];
            }
            else if (dependentValue.Contains(","))
            {
                // Value does not contain a -, so get the value.
                dependentValue = dependentValue.Split(',')[0];
            }
            else if (dependentValue == "-1")
            {
                dependentValue = "1";
            }

            WriteToTrace("RenderStep - RenderValues start");
            var renderedValuesAsync = await RenderValuesAsync(row.Field<string>("values_template"),
                row.Field<string>("variable_name"),
                row.Field<string>("datasource"),
                dependentValue,
                row.Field<string>("datasource_connectedtype"),
                row.Field<string>("fixed_valuelist"),
                row.Field<string>("custom_query"),
                row.Field<string>("check_connectedid"),
                row.Table.Columns.Contains("own_data_values") ? row.Field<string>("own_data_values") : "",
                configurator,
                row.Table.Columns.Contains("datasource_dataselectorid") ? row.Field<int?>("datasource_dataselectorid") ?? 0 : 0);

            var renderedValues = renderedValuesAsync.RenderedValues;
            WriteToTrace($"RenderStep {row.Field<string>("stepname")} - connected to: {dataSourceConnectedIdName} - mainstepnumber: {mainStepNumber} - stepnumber: {stepNumber} - dependentvalue: {dependentValue} - datasource connectedid: {connectedId}");

            // Replace data source_values with the rendered values and return the step template.
            var stepInlineCss = renderedValues == "" ? "display:none;" : "";

            var template = Settings.StepHtml
                .ReplaceCaseInsensitive("{style}", stepInlineCss)
                .ReplaceCaseInsensitive("{mainStepNumber}", mainStepNumber.ToString())
                .ReplaceCaseInsensitive("{stepNumber}", stepNumber.ToString());

            if (!stepNumbers.ContainsKey(currentConfiguratorName))
            {
                await LoadStepNumbersAsync(currentConfiguratorName);
            }

            var stepNumbersDictionary = stepNumbers[currentConfiguratorName];

            var dependsOnString = "";
            if (!String.IsNullOrEmpty(connectedId))
            {
                var dependsOnValues = new List<string>();
                var connectedItems = connectedId.Replace(",", ";").Split(";");

                foreach (var dependency in connectedItems)
                {
                    if (String.IsNullOrEmpty(dependency) || connectedIdNumber != 0 || !stepNumbersDictionary.ContainsKey(dependency))
                    {
                        continue;
                    }

                    dependsOnValues.Add($"jjl_configurator_step-{stepNumbersDictionary[dependency].Item1}");
                }

                dependsOnString = String.Join(";", dependsOnValues);
            }

            template = template.ReplaceCaseInsensitive("{dependsOn}", $"data-jconfigurator-depends-on='{dependsOnString}'");

            var stepContentBuilder = new StringBuilder();
            var subStepsRegex = new Regex("\\{substeps(?:\\|(?<ids>.*?))?\\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (renderedValues != "")
            {
                var stepContent = row.Field<string>("step_template")
                    .ReplaceCaseInsensitive("{datasource_values}", renderedValues)
                    .ReplaceCaseInsensitive("{datasource_count}", renderedValuesAsync.Count.ToString());

                var regexMatches = subStepsRegex.Matches(stepContent).ToList();
                if (regexMatches.Count > 0)
                {
                    WriteToTrace($"3 - Starting HTML for new sub step. Sub step name: {row.Field<string>("substepname")} - mainStepNumber: {mainStepNumber} - stepNumber: {stepNumber}");

                    if (subSteps != null)
                    {
                        foreach (var match in regexMatches)
                        {
                            var subStepContents = new StringBuilder();
                            var subStepCount = 0;

                            if (!String.IsNullOrWhiteSpace(match.Groups["ids"].Value))
                            {
                                var renderForSubStepValues = match.Groups["ids"].Value.Split('|');
                                foreach (var renderForSubStepValue in renderForSubStepValues)
                                {
                                    // Get sub step by either ID or name.
                                    var subStepRow = UInt64.TryParse(renderForSubStepValue, out var subStepId)
                                        ? subSteps.FirstOrDefault(subStep => Convert.ToUInt64(subStep["subStepId"]) == subStepId)
                                        : subSteps.FirstOrDefault(subStep => subStep.Field<string>("substepname").Equals(renderForSubStepValue, StringComparison.OrdinalIgnoreCase));

                                    if (subStepRow == null) continue;

                                    subStepCount += 1;
                                    subStepContents.Append(await DoRenderingOfSubStepAsync(currentConfiguratorName, subStepRow, mainStepNumber, stepNumber, subStepCount, configurator: configurator));
                                }
                            }
                            else
                            {
                                foreach (var subStepRow in subSteps)
                                {
                                    subStepCount += 1;
                                    subStepContents.Append(await DoRenderingOfSubStepAsync(currentConfiguratorName, subStepRow, mainStepNumber, stepNumber, subStepCount, configurator: configurator));
                                }
                            }

                            stepContent = stepContent.ReplaceCaseInsensitive(match.Value, subStepContents.ToString());
                        }
                    }
                }
                else
                {
                    WriteToTrace($"Sub step not generated, no variable 'substeps' found in main template. Sub step name: {row.Field<string>("substepname")} - mainStepNumber: {mainStepNumber} - stepNumber: {stepNumber}");
                }

                stepContent = stepContent
                    .ReplaceCaseInsensitive("{mainStepNumber}", mainStepNumber.ToString())
                    .ReplaceCaseInsensitive("{stepNumber}", stepNumber.ToString());

                stepContentBuilder.Append(stepContent);
            }
            else
            {
                WriteToTrace($"4 - Starting HTML for new sub step. Sub step name: {row.Field<string>("substepname")} - mainStepNumber: {mainStepNumber} - stepNumber: {stepNumber}");
                stepContentBuilder.Append("<!-- NoValues -->");
                var stepTemplate = row.Field<string>("step_template");
                var regexMatches = subStepsRegex.Matches(stepTemplate).ToList();
                foreach (var match in regexMatches)
                {
                    stepContentBuilder.Append(match.Value);
                }
            }

            WriteToTrace("End building stepContent");

            template = template.ReplaceCaseInsensitive("{stepContent}", stepContentBuilder.ToString());

            WriteToTrace("End ReplaceCaseInsensitive stepContent");

            template = await this.configuratorsService.ReplaceConfiguratorItemsAsync(template, configurator, false);

            WriteToTrace("End ReplaceConfiguratorItems (mainstep)");

            template = await StringReplacementsService.DoAllReplacementsAsync(template, row, removeUnknownVariables: false);

            WriteToTrace("End DoAllReplacementsAsync (mainstep)");

            template = await TemplatesService.HandleIncludesAsync(template, false, null, false);

            WriteToTrace("End HandleIncludesAsync (mainstep)");
            return template;
        }

        /// <summary>
        /// Render sub step.
        /// </summary>
        /// <param name="currentConfiguratorName"></param>
        /// <param name="row"></param>
        /// <param name="mainStepNumber"></param>
        /// <param name="stepNumber"></param>
        /// <param name="subStepNumber"></param>
        /// <param name="dependentValue"></param>
        /// <param name="configurator"></param>
        /// <returns></returns>
        private async Task<string> DoRenderingOfSubStepAsync(string currentConfiguratorName, DataRow row, int mainStepNumber, int stepNumber, int subStepNumber, string dependentValue = "-1", ConfigurationsModel configurator = null)
        {
            var connectedId = row.Field<string>("substep_datasource_connectedid");
            // The replacement is because JS replaces choice-name with choiceName.
            var datasourceConnectedIdName = connectedId?.Replace("-", "");

            // Get the correct id for rendering the values if this step is dependent on other steps.
            if (Int32.TryParse(connectedId, out var connectedIdNumber))
            {
                // If the parse is succesfull, the connected ID is a fixed value, like all products from category '5'.
                dependentValue = connectedIdNumber.ToString();
            }
            else if (dependentValue == "-1" && !String.IsNullOrEmpty(datasourceConnectedIdName) && !String.IsNullOrEmpty(HttpContextHelpers.GetRequestValue(HttpContext, datasourceConnectedIdName)))
            {
                // If the configurator is reloaded, then get the connected id from the URL.
                dependentValue = HttpContextHelpers.GetRequestValue(HttpContext, datasourceConnectedIdName);
            }

            // Get the correct value if the value is in the format of 'number-1' or '1,2,3'.
            if (!Settings.ValuesCanContainDashes && dependentValue.Contains("-"))
            {
                // Value contains a - so get the value (x) from a input like 'value-x'.
                dependentValue = dependentValue.Split('-')[1];
            }
            else if (dependentValue.Contains(","))
            {
                // Value does not contain a -, so get the value.
                dependentValue = dependentValue.Split(',')[0];
            }
            else if (dependentValue == "-1")
            {
                dependentValue = "1";
            }
            var renderedValues = await RenderValuesAsync(
                row.Field<string>("substep_values_template"),
                row.Field<string>("substep_variable_name"),
                row.Field<string>("substep_datasource"),
                dependentValue, row.Field<string>("substep_datasource_connectedtype"),
                row.Field<string>("substep_fixed_valuelist"),
                row.Field<string>("substep_custom_query"),
                row.Field<string>("substep_check_connectedid"),
                row.Table.Columns.Contains("substep_own_data_values") ? row.Field<string>("substep_own_data_values") : "",
                configurator, row.Table.Columns.Contains("substep_datasource_dataselectorid") ? row.Field<int?>("substep_datasource_dataselectorid") ?? 0 : 0);

            WriteToTrace($"RenderSubStep {row.Field<string>("substepname")} - connected to: {datasourceConnectedIdName} - mainstepnumber: {mainStepNumber} - stepnumber: {stepNumber} - substepnumber: {subStepNumber} - dependentvalue: {dependentValue} - datasource connectedid: {connectedId}");

            var template = Settings.SubStepHtml
                .ReplaceCaseInsensitive("{mainStepNumber}", mainStepNumber.ToString())
                .ReplaceCaseInsensitive("{stepNumber}", stepNumber.ToString())
                .ReplaceCaseInsensitive("{subStepNumber}", subStepNumber.ToString());

            if (!stepNumbers.ContainsKey(currentConfiguratorName))
            {
                await LoadStepNumbersAsync(currentConfiguratorName);
            }

            var stepNumbersDictionary = stepNumbers[currentConfiguratorName];

            var dependsOnString = "";
            if (!String.IsNullOrEmpty(connectedId))
            {
                var dependsOnValues = new List<string>();
                var connectedItems = connectedId.Replace(",", ";").Split(";");

                foreach (var dependency in connectedItems)
                {
                    if (String.IsNullOrEmpty(dependency) || connectedIdNumber != 0)
                    {
                        continue;
                    }

                    string dependencyValue;
                    if (stepNumbersDictionary.ContainsKey(dependency))
                    {
                        dependencyValue = $"jjl_configurator_step-{stepNumbersDictionary[dependency].Item1}";
                    }
                    else
                    {
                        var step = stepNumbersDictionary.FirstOrDefault(step => step.Value.Item2.ContainsKey(dependency)).Value;
                        if (step == null)
                        {
                            continue;
                        }

                        dependencyValue = $"jjl_configurator_substep-{step.Item2[dependency]}";
                    }

                    if (String.IsNullOrWhiteSpace(dependencyValue))
                    {
                        continue;
                    }

                    dependsOnValues.Add(dependencyValue);
                }

                dependsOnString = String.Join(";", dependsOnValues);
            }

            template = template.ReplaceCaseInsensitive("{dependsOn}", $"data-jconfigurator-depends-on='{dependsOnString}'");

            var subStepContent = $"<!-- datasource: {row.Field<string>("substep_datasource")} - connectedId: {connectedId} {connectedIdNumber} -->";

            if (renderedValues.RenderedValues != "")
            {
                subStepContent += row.Field<string>("substep_template")
                    .ReplaceCaseInsensitive("{datasource_values}", renderedValues.RenderedValues)
                    .ReplaceCaseInsensitive("{datasource_count}", renderedValues.Count.ToString())
                    .ReplaceCaseInsensitive("{mainStepNumber}", mainStepNumber.ToString())
                    .ReplaceCaseInsensitive("{stepNumber}", stepNumber.ToString())
                    .ReplaceCaseInsensitive("{subStepNumber}", subStepNumber.ToString());
            }

            template = template.ReplaceCaseInsensitive("{subStepContent}", subStepContent);
            template = await configuratorsService.ReplaceConfiguratorItemsAsync(template, configurator, false);
            template = await StringReplacementsService.DoAllReplacementsAsync(template, row, removeUnknownVariables: false);
            template = await TemplatesService.HandleIncludesAsync(template, false, null, false);

            return template;

        }

        /// <summary>
        /// render values based on datasource
        /// </summary>
        /// <param name="template"></param>
        /// <param name="variableName"></param>
        /// <param name="dataSource"></param>
        /// <param name="connectedId"></param>
        /// <param name="connectedType"></param>
        /// <param name="fixedValueList"></param>
        /// <param name="customQuery"></param>
        /// <param name="checkConnectedId"></param>
        /// <param name="ownDataValues"></param>
        /// <param name="configuration"></param>
        /// <param name="dataSelectorId"></param>
        /// <returns></returns>
        private async Task<(int Count, string RenderedValues)> RenderValuesAsync(string template, string variableName, string dataSource, string connectedId, string connectedType, string fixedValueList = "", string customQuery = "", string checkConnectedId = "", string ownDataValues = "", ConfigurationsModel configuration = null, int dataSelectorId = 0)
        {
            var query = "";
            var renderedValues = new StringBuilder();
            var count = 0;
            template = template?.Replace("{variablename}", variableName);

            // Get values (from database or else).
            if (connectedId != "-1" || dataSource == "fixedvalues")
            {
                switch (dataSource)
                {
                    case "categories":
                        {
                            query = Settings.ProductCategoriesQuery;
                            break;
                        }
                    case "products":
                        {
                            query = Settings.ProductsQuery;
                            break;
                        }
                    case "variants":
                        {
                            query = Settings.ProductVariantsQuery;
                            break;
                        }
                    case "connectedproductsonproduct":
                        {
                            query = Settings.ConnectedProductsOnProductQuery;
                            break;
                        }
                    case "connectedproductsoncategory":
                        {
                            query = Settings.ConnectedProductsOnCategoryQuery;
                            break;
                        }
                    case "fixedvalues":
                        {
                            if (connectedId == checkConnectedId || checkConnectedId == "")
                            {
                                var fixedValues = new List<string>();
                                fixedValues.AddRange(fixedValueList?.Split(new[]
                                {
                                    '\r',
                                    '\n',
                                }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
                                count = fixedValues.Count;

                                foreach (var valuelist in fixedValues)
                                {
                                    var i = 1;
                                    var temporaryTemplate = template;

                                    foreach (var value in valuelist.Split(';'))
                                    {
                                        temporaryTemplate = temporaryTemplate?.Replace($"{{valuelist_{i}}}", value);

                                        i += 1;
                                    }

                                    renderedValues.Append(temporaryTemplate);
                                }
                            }
                            break;
                        }
                    case "customquery":
                        {
                            if (connectedId != "-1" || !customQuery.Contains("{connectedid}"))
                            {
                                DatabaseConnection.AddParameter("connectedId", connectedId);
                                query = customQuery.ReplaceCaseInsensitive("'{connectedId}'", "?connectedId");
                                query = await  this.configuratorsService.ReplaceConfiguratorItemsAsync(query, configuration, true);
                                query = await TemplatesService.HandleIncludesAsync(query, false, null, false, true);
                                query = query.ReplaceCaseInsensitive("{connectedId}", "?connectedId");
                                query = await StringReplacementsService.DoAllReplacementsAsync(query, null, true, true, false, true);
                            }
                            break;
                        }
                    case "ownarticles":
                        {
                            // Initial checks.
                            var apiBaseUrl = StringReplacementsService.DoHttpRequestReplacements(Settings.ProductsApiBaseUrl);
                            var getProductsUrl = StringReplacementsService.DoHttpRequestReplacements(Settings.ProductsApiGetProductsUrl);
                            if (String.IsNullOrWhiteSpace(apiBaseUrl) || String.IsNullOrWhiteSpace(getProductsUrl) || String.IsNullOrWhiteSpace(ownDataValues))
                            {
                                break;
                            }

                            // Get product numbers.
                            var productNumbers = new List<string>();
                            if (!ownDataValues.Contains("SELECT ", StringComparison.OrdinalIgnoreCase))
                            {
                                productNumbers.AddRange(ownDataValues.Split(new[]
                                {
                                    '\r',
                                    '\n',
                                }, StringSplitOptions.RemoveEmptyEntries));
                            }
                            else
                            {
                                ownDataValues = await this.configuratorsService.ReplaceConfiguratorItemsAsync(ownDataValues, configuration, true);
                                var dt = await DatabaseConnection.GetAsync(ownDataValues);

                                if (dt.Rows.Count == 0)
                                {
                                    break;
                                }
                                foreach (DataRow dataRow in dt.Rows)
                                {
                                    productNumbers.Add(Convert.ToString(dataRow[0]));
                                }
                            }

                            // Get data from API based on product numbers.
                            var restClient = new RestClient(apiBaseUrl);
                            var restRequest = new RestRequest(getProductsUrl);
                            restRequest.AddUrlSegment("productNumbers", String.Join(",", productNumbers));
                            var result = await restClient.ExecuteAsync(restRequest);

                            if (result.ErrorException != null)
                            {
                                WriteToTrace($"Products API caused an exception: {result.ErrorException}", true);
                                break;
                            }
                            if (result.StatusCode != HttpStatusCode.OK)
                            {
                                WriteToTrace($"Products API returned non-200 status code: {result.StatusCode} with description: {result.StatusDescription} and content: {result.Content}", true);
                                break;
                            }
                            if (String.IsNullOrWhiteSpace(result.Content))
                            {
                                WriteToTrace("Products API returned a 200 status code, but the content was empty.", true);
                                break;
                            }

                            var responseData = (JToken)JsonConvert.DeserializeObject(result.Content);
                            var responseType = responseData?.GetType();

                            if (responseType == typeof(JArray))
                            {
                                var resultsArray = (JArray)responseData;

                                foreach (var value in resultsArray)
                                {
                                    throw new NotImplementedException("TODO Replace JSON object in string, create new overload of StringReplacementsService.DoReplaces() for that.");
                                    //renderedValues.Append(JCLTemplate.ReplaceJsonPropertyValues(template, (JObject)value, usePath: false));
                                }
                            }
                            else if (responseType == typeof(JObject))
                            {
                                throw new NotImplementedException("TODO Replace JSON object in string, create new overload of StringReplacementsService.DoReplaces() for that.");
                                //renderedValues.Append(JCLTemplate.ReplaceJsonPropertyValues(template, (JObject)responseData, usePath: false));
                            }

                            break;
                        }
                    case "dataselector":
                        {
                            if (dataSelectorId <= 0)
                            {
                                WriteToTrace($"DataSelectorId is invalid: {dataSelectorId}", true);
                                break;
                            }

                            var dataSelectorJson = await dataSelectorsService.GetDataSelectorJsonAsync(dataSelectorId);
                            var dataSelector = JsonConvert.DeserializeObject<Modules.DataSelector.Models.DataSelector>(dataSelectorJson);

                            var itemsRequest = new Modules.DataSelector.Models.ItemsRequest
                            {
                                Selector = dataSelector
                            };

                            query = await dataSelectorsService.GetQueryAsync(itemsRequest);
                            query = await StringReplacementsService.DoAllReplacementsAsync(query, null, false, false, false, true);
                            query = await configuratorsService.ReplaceConfiguratorItemsAsync(query, configuration, true);
                            break;
                        }
                }
            }

            if (!String.IsNullOrEmpty(query))
            {
                DatabaseConnection.AddParameter("connectedId", connectedId);
                DatabaseConnection.AddParameter("connectedType", connectedType);
                var dataTable = await DatabaseConnection.GetAsync(query);

                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    count = dataTable.Rows.Count;
                    renderedValues.Append(String.Join("", StringReplacementsService.DoReplacements(template, dataTable)));
                }
            }

            return (count, renderedValues.ToString());
        }

        #endregion

        #region Web methods

        /// <summary>
        /// Calculates the deliveryTime
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public async Task<(string deliveryTime, string deliveryExtra)> GetDeliveryTime(ConfigurationsModel configuration)
        {
            return await this.configuratorsService.GetDeliveryTimeAsync(configuration);
        }

        /// <summary>
        ///  <para>Calculates the price and purchase price of a product.</para>
        ///  <para>Returns a <see cref="Tuple"/> where Item1 is the purchase price and Item2 is the customer price.</para>
        ///  </summary>
        ///  <param name="input"></param>
        ///  <returns>A <see cref="Tuple"/> where Item1 is the purchase price and Item2 is the customer price.</returns>
        public async Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePrice(ConfigurationsModel input)
        {
            var prices =  await this.configuratorsService.CalculatePriceAsync(input);
            // we dont want to return a purchaseprice when using the webmethod
            return (0, prices.customerPrice, prices.fromPrice);
        }

        /// <summary>
        ///  Renders multiple steps with one request.
        ///  </summary>
        ///  <param name="steps">A list of <see cref="RenderStepsModel"/> with all steps to render.</param>
        ///  <param name="configuration">The <see cref="ConfigurationsModel" />.</param>
        ///  <returns>A dictionary where the key is the step ID and the value is the rendered HTML of that step.</returns>
        public async Task<Dictionary<string, string>> RenderSteps(List<RenderStepsModel> steps, ConfigurationsModel configuration)
        {
            var result = new Dictionary<string, string>();

            WriteToTrace("Start render steps");

            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            var dataTable = await configuratorsService.GetConfiguratorDataAsync(configuration.Configurator);
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return result;
            }

            WriteToTrace("Configurator data cached");

            var preRenderStepsQuery = dataTable.Rows[0].Field<string>("pre_render_steps_query");
            WriteToTrace($"preRenderStepsQuery 2: {preRenderStepsQuery}");

            if (!String.IsNullOrWhiteSpace(preRenderStepsQuery))
            {
                await DatabaseConnection.ExecuteAsync(await this.configuratorsService.ReplaceConfiguratorItemsAsync(preRenderStepsQuery, configuration, true));
            }

            var customParam = await GetCustomParameters(configuration, dataTable);

            if (customParam != null && !configuration.QueryStringItems.ContainsKey(customParam.Name))
            {
                configuration.QueryStringItems.Add(customParam.Name, customParam.Value);
            }

            WriteToTrace("Custom param handled");

            foreach (var step in steps)
            {
                if (step.SubStep != -1)
                {
                    result.Add($"jjl_configurator_substep-{step.MainStep}-{step.Step}-{step.SubStep}", await TemplatesService.DoReplacesAsync(await RenderSubStep(configuration.Configurator, step.MainStep, step.Step, step.SubStep, step.DependentValue, configuration, dataTable), removeUnknownVariables: false));
                }
                else
                {
                    result.Add($"jjl_configurator_step-{step.MainStep}-{step.Step}", await TemplatesService.DoReplacesAsync(await RenderStep(configuration.Configurator, step.MainStep, step.Step, step.DependentValue, configuration, dataTable), removeUnknownVariables: false));
                }
            }
            
            var useAbsoluteImageUrls = String.Equals(HttpContextHelpers.GetRequestValue(HttpContext, "absoluteImageUrls"), "true", StringComparison.OrdinalIgnoreCase);
            var removeSvgUrlsFromIcons = String.Equals(HttpContextHelpers.GetRequestValue(HttpContext, "removeSvgUrlsFromIcons"), "true", StringComparison.OrdinalIgnoreCase);

            if (useAbsoluteImageUrls || removeSvgUrlsFromIcons)
            {
                // Variable html is a struct copy so changes do not apply to it. The key can be used to manipulate the correct index.
                foreach (var html in result)
                {
                    // Make relative image URls absolute to allow the template to show images when the HTML is placed inside another website.
                    if (useAbsoluteImageUrls)
                    {
                        var imagesDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain");
                        result[html.Key] = await wiserItemsService.ReplaceRelativeImagesToAbsoluteAsync(result[html.Key], imagesDomain);
                    }
                    
                    // Remove the URLs from SVG files to allow the template to load SVGs when the HTML is placed inside another website.
                    // To use this functionality the content of the SVG needs to be placed in the HTML, xlink can only load URLs from same domain, protocol and port.
                    if (removeSvgUrlsFromIcons)
                    {
                        var regex = new Regex(@"<svg(?:[^>]*)>(?:\s*)<use(?:[^>]*)xlink:href=""([^>""]*)#(?:[^>""]*)""(?:[^>]*)>");
                        foreach (Match match in regex.Matches(html.Value))
                        {
                            result[html.Key] = result[html.Key].Replace(match.Groups[1].Value, "");
                        }
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Renders the HTML for a certain step. 
        /// This method is meant to be called via AJAX, via jclcomponent.jcl.
        /// </summary>
        /// <param name="name">The name of the configurator.</param>
        /// <param name="mainStep">The number of the main step.</param>
        /// <param name="step">The number of the step.</param>
        /// <param name="dependentValue">Optional: The value that this step depends on.</param>
        /// <param name="configurator">Optional: The <see cref="ConfigurationsModel" /> to use.</param>
        /// <param name="dataTable"></param>
        /// <returns>The rendered HTML.</returns>
        public async Task<string> RenderStep(string name, int mainStep, int step, string dependentValue = null, ConfigurationsModel configurator = null, DataTable dataTable = null)
        {

            WriteToTrace($"RenderStep - name: {name}, mainStep: {mainStep}, step: {step}, dependentValue: {dependentValue ?? "NULL"}, configurator: {(configurator == null ? "NULL" : JsonConvert.SerializeObject(configurator))}");

            DataRow dataRow = null;
            dataTable ??= await configuratorsService.GetConfiguratorDataAsync(name);

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return "No result! (unknown name, main step or step)";
            }

            var preRenderStepsQuery = dataTable.Rows[0].Field<string>("pre_render_steps_query");
            WriteToTrace($"preRenderStepsQuery 3: {preRenderStepsQuery}");

            if (!String.IsNullOrWhiteSpace(preRenderStepsQuery))
            {
                await DatabaseConnection.ExecuteAsync(await this.configuratorsService.ReplaceConfiguratorItemsAsync(preRenderStepsQuery, configurator, true));
            }

            var mainStepCount = 0;
            var stepCount = 0;
            var currentStepName = "";
            var currentMainStepName = "";
            var subSteps = new List<DataRow>();

            var breakLoop = false;

            foreach (DataRow row in dataTable.Rows)
            {

                var currentUrl = HttpContextHelpers.GetBaseUri(HttpContext).AbsoluteUri;
                var urlRegex = row.Field<string>("urlregex");
                if (!String.IsNullOrWhiteSpace(urlRegex) && !Regex.IsMatch(currentUrl, urlRegex))
                {
                    continue;
                }

                // Only render main step, step or sub step if URL regex is filled and a match.
                if (row.Field<string>("mainstepname") != currentMainStepName)
                {
                    if (breakLoop)
                    {
                        break;
                    }

                    // This is a new main step.
                    currentMainStepName = row.Field<string>("mainstepname");
                    mainStepCount += 1;
                    stepCount = 0;
                    currentStepName = "";
                }

                if (row.Field<string>("stepname") != currentStepName)
                {
                    if (breakLoop)
                    {
                        break;
                    }

                    currentStepName = row.Field<string>("stepname");

                    stepCount += 1;
                }

                // Catch on match
                if (mainStepCount == mainStep && stepCount == step)
                {
                    // If dataRow Is Nothing Then
                    dataRow = row;
                    // End If
                    if (!String.IsNullOrWhiteSpace(row.Field<string>("substepname")))
                    {
                        subSteps.Add(row);
                    }

                    breakLoop = true;
                }
            }
            return dataRow == null ? "No result! (unknown name, main step or step)" : await RenderStepAsync(name, dataRow, mainStep, step, dependentValue, configurator, subSteps);
        }

        /// <summary>
        /// render sub step
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mainStep"></param>
        /// <param name="step"></param>
        /// <param name="subStep"></param>
        /// <param name="dependentValue"></param>
        /// <param name="configurator"></param>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public async Task<string> RenderSubStep(string name, int mainStep, int step, int subStep, string dependentValue = null, ConfigurationsModel configurator = null, DataTable dataTable = null)
        {
            DataRow dataRow = null;

            dataTable ??= await configuratorsService.GetConfiguratorDataAsync(name);

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return "No result! (unknown name, main step or step)";
            }

            var preRenderStepsQuery = dataTable.Rows[0].Field<string>("pre_render_steps_query");
            WriteToTrace($"preRenderStepsQuery 4: {preRenderStepsQuery}");
            if (!String.IsNullOrWhiteSpace(preRenderStepsQuery))
            {
                await DatabaseConnection.ExecuteAsync(await this.configuratorsService.ReplaceConfiguratorItemsAsync(preRenderStepsQuery, configurator, true));
            }

            var mainStepCounter = 0;
            var stepCounter = 0;
            var subStepCounter = 0;
            var currentMainStepName = "";
            var currentStepName = "";

            foreach (DataRow row in dataTable.Rows)
            {
                if (row.Field<string>("mainstepname") != currentMainStepName)
                {
                    mainStepCounter += 1;
                    stepCounter = 1;
                    subStepCounter = 0;
                    currentMainStepName = row.Field<string>("mainstepname");
                    currentStepName = row.Field<string>("stepname");
                }

                if (row.Field<string>("stepname") != currentStepName)
                {
                    stepCounter += 1;
                    subStepCounter = 0;
                    currentStepName = row.Field<string>("stepname");
                }

                subStepCounter += 1;

                if (mainStepCounter == mainStep && stepCounter == step && subStepCounter == subStep)
                {
                    dataRow = row;
                    break;
                }
            }

            return dataRow == null ? "No result! (unknown name, main step or step)" : await DoRenderingOfSubStepAsync(name, dataRow, mainStep, step, subStep, dependentValue, configurator);
        }

        /// <summary>
        /// get custom parameters of configurator
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        private async Task<CustomParameters> GetCustomParameters(ConfigurationsModel configuration, DataTable dataTable = null)
        {
            var customParameter = new CustomParameters();

            dataTable ??= await configuratorsService.GetConfiguratorDataAsync(configuration.Configurator);

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return null;
            }

            customParameter.Name = dataTable.Rows[0].Field<string>("custom_param_name");
            customParameter.Query = dataTable.Rows[0].Field<string>("custom_param_query");
            customParameter.Dependencies = dataTable.Rows[0].Field<string>("custom_param_dependencies")?.Split(",").ToList();

            // If one of the dependencies is missing, then return, custom parameter cannot be calculated
            if (customParameter.Dependencies != null)
            {
                foreach (var dependency in customParameter.Dependencies)
                {
                    if (!configuration.QueryStringItems.ContainsKey(dependency))
                    {
                        return null;
                    }
                }
            }

            // Check if the parameters are set, if not just return null
            if (String.IsNullOrWhiteSpace(customParameter.Name) || String.IsNullOrWhiteSpace(customParameter.Query))
            {
                return null;
            }

            // Prepare Query
            customParameter.Query = await this.configuratorsService.ReplaceConfiguratorItemsAsync(customParameter.Query, configuration, true);

            // Run Query
            dataTable = await DatabaseConnection.GetAsync(customParameter.Query);

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return null;
            }

            customParameter.Value = dataTable.Rows[0][0].ToString();

            return customParameter;
        }
        #endregion
    }
}
