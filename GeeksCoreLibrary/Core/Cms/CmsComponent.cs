using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Base.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Core.Cms
{
    public abstract class CmsComponent<T, T2> : ViewComponent
        where T : CmsSettings
        where T2 : Enum
    {
        protected ILogger Logger;
        protected IStringReplacementsService StringReplacementsService;
        protected ITemplatesService TemplatesService;
        protected IDatabaseConnection DatabaseConnection;
        protected IAccountsService AccountsService;
        protected IComponentsService ComponentsService;

        /// <summary>
        /// Whether the component should be ran in legacy mode. This reads and writes the old Wiser1 JSON settings.
        /// </summary>
        public T2 LegacyMode { get; set; }

        /// <summary>
        /// Settings For the component that are used in the CMS.
        /// </summary>
        public T Settings { get; set; }

        /// <summary>
        /// The ID of the component in the CMS.
        /// </summary>
        public int ComponentId { get; protected set; }

        /// <summary>
        /// Any extra data to be used in all replacements in the component.
        /// </summary>
        protected Dictionary<string, string> ExtraDataForReplacements { get; set; }

        /// <summary>
        /// Save the settings to the CMS, or return the setting string.
        /// </summary>
        /// <returns></returns>
        public string SaveSettingsToCms(bool onlyReturnString = false)
        {
            return null;
        }

        /// <summary>
        /// Generate the output Html.
        /// </summary>
        /// <returns></returns>
        public abstract Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData);

        /// <summary>
        /// Parses the JSON to the correct CmsSettings.
        /// </summary>
        public abstract void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null);

        /// <summary>
        /// Get the correct settings JSON. If legacy mode than legacy object else the normal object is serialized.
        /// </summary>
        public abstract string GetSettingsJson();

        /// <summary>
        /// Invokes a specific method on a ViewComponent and returns the results of that method.
        /// This is meant to be used in /gclcomponent.gcl.
        /// </summary>
        /// <param name="callMethod">The name of the method to call.</param>
        /// <returns>The result of the method.</returns>
        protected async Task<object> InvokeMethodAsync(string callMethod)
        {
            var componentInstanceType = GetType();
            var method = componentInstanceType.GetMethod(callMethod, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                method = componentInstanceType.GetMethod($"{callMethod}Async", BindingFlags.Public | BindingFlags.Instance);
            }
            if (method == null)
            {
                Logger.LogTrace($"Called GclComponent.gcl with componentId '{ComponentId}' and methodName '{callMethod}', but this method does not exist.");
                return null;
            }

            var parameterValues = new List<object>();
            var methodParameters = method.GetParameters();
            if (!string.IsNullOrWhiteSpace(HttpContext.Request.ContentType) && !HttpContext.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var parameter in methodParameters)
                {
                    if (parameter.IsOptional && !HttpContextHelpers.RequestContainsKey(HttpContext, parameter.Name))
                    {
                        parameterValues.Add(parameter.DefaultValue);
                        continue;
                    }

                    parameterValues.Add(Convert.ChangeType(HttpContextHelpers.GetRequestValue(HttpContext, parameter.Name), parameter.ParameterType));
                }
            }
            else
            {
                if (HttpContext.Request.Body.CanSeek)
                {
                    HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                }

                using var stream = new StreamReader(HttpContext.Request.Body);
                var body = await stream.ReadToEndAsync();
                var contentObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

                foreach (var parameter in methodParameters)
                {
                    var (key, value) = contentObject.FirstOrDefault(p => p.Key.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
                    if (parameter.IsOptional && !contentObject.ContainsKey(key))
                    {
                        parameterValues.Add(parameter.DefaultValue);
                        continue;
                    }

                    if (String.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    switch (value)
                    {
                        // If the value is a JObject, it means that it's not a simple type (such as string or int), so convert that to it's actual type, otherwise just cast it.
                        case JObject valueAsJObject:
                            parameterValues.Add(valueAsJObject.ToObject(parameter.ParameterType));
                            break;
                        case JArray valueAsJArray:
                            parameterValues.Add(valueAsJArray.ToObject(parameter.ParameterType));
                            break;
                        default:
                            parameterValues.Add(Convert.ChangeType(value, parameter.ParameterType));
                            break;
                    }
                }
            }

            object result;

            if (method.ReturnType.BaseType == typeof(Task))
            {
                result = await (dynamic)method.Invoke(this, BindingFlags.Public | BindingFlags.Instance, null, parameterValues.ToArray(), Thread.CurrentThread.CurrentCulture);
            }
            else
            {
                result = method.Invoke(this, BindingFlags.Public | BindingFlags.Instance, null, parameterValues.ToArray(), Thread.CurrentThread.CurrentCulture);
            }

            if (result is Task<object> resultTask)
            {
                result = await resultTask.ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Gets the current component mode and finds the default settings for that mode.
        /// For each setting in <see cref="Settings"/> that has no value, a default value will be set.
        /// </summary>
        protected void HandleDefaultSettingsFromComponentMode()
        {
            var settingsType = Settings.GetType();
            var componentModeProperty = settingsType.GetProperty("ComponentMode");
            if (componentModeProperty == null)
            {
                return;
            }

            var assembly = Assembly.GetAssembly(GetType());
            var fullTypeName = $"{GetType().Namespace}.Models.{GetType().Name}{componentModeProperty.GetValue(Settings)}SettingsModel";
            var type = assembly?.GetType(fullTypeName);
            if (type == null)
            {
                return;
            }

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var propertyWithDefaultValue in properties)
            {
                var resultProperty = settingsType.GetProperty(propertyWithDefaultValue.Name);
                if (resultProperty == null)
                {
                    continue;
                }

                var currentValue = resultProperty.GetValue(Settings);
                if (currentValue != null && !Equals(currentValue, resultProperty.PropertyType.GetDefaultValue()) && (currentValue is not string stringValue || !String.IsNullOrEmpty(stringValue)))
                {
                    continue;
                }

                var defaultValueAttribute = propertyWithDefaultValue.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultValueAttribute?.Value == null)
                {
                    continue;
                }

                var propertyType = Nullable.GetUnderlyingType(resultProperty.PropertyType) ?? resultProperty.PropertyType;
                resultProperty.SetValue(Settings, Convert.ChangeType(defaultValueAttribute.Value, propertyType));
            }
        }

        /// <summary>
        /// Renders the query from the <see param="queryToUse"/> parameter, by replacing all variables with their corresponding values,
        /// then executes that rendered query and lastly returns the <see cref="DataTable" /> with the result(s).
        /// </summary>
        /// <param name="queryToUse">The query to render and execute.</param>
        /// <param name="dataRowForReplacements">Optional: A <see cref="DataRow"/> to use for replacements from the result of a query.</param>
        /// <param name="doVariablesCheck">Optional: If this is set to true and the query still contains unhandled replacements after doing all of the replacements, then the query will not be executed. Default value is <see langword="false" />.</param>
        /// <param name="skipCache">Optional: Set to true to ensure the caching is never used for the query. Default value is <see langword="false" />.</param>
        /// <returns>A <see cref="DataTable" /> with the result(s), or NULL if the query was empty.</returns>
        protected async Task<DataTable> RenderAndExecuteQueryAsync(string queryToUse, DataRow dataRowForReplacements = null, bool doVariablesCheck = false, bool skipCache = false)
        {
            if (String.IsNullOrWhiteSpace(queryToUse))
            {
                WriteToTrace("Query for component is empty!");
                return new DataTable();
            }
        
            if (ExtraDataForReplacements != null && ExtraDataForReplacements.Any())
            {
                
                queryToUse = StringReplacementsService.DoReplacements(queryToUse, ExtraDataForReplacements, true);
            }
            
            queryToUse = await TemplatesService.DoReplacesAsync(queryToUse, handleDynamicContent: false, dataRow: dataRowForReplacements, forQuery: true);
            if (doVariablesCheck)
            {
                var expression = new Regex("{.*?}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                if (expression.IsMatch(queryToUse))
                {
                    // Don't proceed, query from data selector contains variables, this gives syntax errors.
                    return new DataTable();
                }
            }

            return await DatabaseConnection.GetAsync(queryToUse, skipCache);
        }

        /// <summary>
        /// This is just a wrapper around The default logger.
        /// It adds the current class name and the ComponentId before every trace, so we can see which traces come from this component.
        /// </summary>
        /// <param name="message">The message to write to trace.</param>
        /// <param name="showAsWarning">Optional: Indicate whether to show the message as a warning/error. Default is <see langword="false"/>.</param>
        protected void WriteToTrace(string message, bool showAsWarning = false)
        {
            message = $"{GetType().Name} ({ComponentId}) - {message}";
            if (showAsWarning)
            {
                Logger.LogWarning(message);
            }
            else
            {
                Logger.LogTrace(message);
            }
        }

        /// <summary>
        /// Use this function to check whether or not your component should render it's HTML.
        /// </summary>
        /// <returns></returns>
        protected async Task<(bool RenderHtml, string DebugInformation)> ShouldRenderHtmlAsync()
        {
            var renderHtml = !Settings.UserNeedsToBeLoggedIn || (AccountsService != null && (await AccountsService.GetUserDataFromCookieAsync()).UserId > 0);
            var debugInformation = renderHtml ? "" : $"<!-- Component {GetType().Name} ({ComponentId}) not rendered because the user is not logged in. -->";
            return (renderHtml, debugInformation);
        }

        protected void AddExternalJavaScriptLibrary(string url, bool async = false, bool defer = false)
        {
            var javaScriptLibraries = HttpContext.Items[CmsSettings.ExternalJavaScriptLibrariesFromComponentKey] as List<JavaScriptResourceModel> ?? new List<JavaScriptResourceModel>();

            // Turn the URL into an absolute URL if it isn't already.
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
            {
                url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}{url}";
            }

            // Check if the URL isn't already in the list.
            var item = javaScriptLibraries.FirstOrDefault(l => l.Uri.AbsoluteUri.Equals(url, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.Async = async;
                item.Defer = defer;
            }
            else
            {
                javaScriptLibraries.Add(new JavaScriptResourceModel
                {
                    Uri = new Uri(url, UriKind.RelativeOrAbsolute),
                    Async = async,
                    Defer = defer
                });
            }

            HttpContext.Items[CmsSettings.ExternalJavaScriptLibrariesFromComponentKey] = javaScriptLibraries;
        }

        /// <summary>
        /// Add hidden input with content id and anti forgery token in every form.
        /// </summary>
        /// <param name="html"></param>
        /// <param name="componentIdFieldName"></param>
        /// <returns></returns>
        protected string AddComponentIdToForms(string html, string componentIdFieldName)
        {
            if (ComponentId <= 0)
            {
                return html;
            }

            var componentIdInput = $"<input type='hidden' name='{componentIdFieldName}' value='{ComponentId}' />";
            var formTagLength = 7;
            var closeFormIndex = html.IndexOf("</form>", StringComparison.Ordinal);
            if (closeFormIndex <= -1)
            {
                formTagLength = 8;
                closeFormIndex = html.IndexOf("</jform>", StringComparison.Ordinal);
            }

            while (closeFormIndex > -1)
            {
                var startIndex = closeFormIndex + formTagLength;
                if (ComponentId > 0)
                {
                    startIndex += componentIdInput.Length;
                    Logger.LogDebug($"Place hidden input with id {componentIdFieldName}={ComponentId}");
                    html = html.Insert(closeFormIndex, componentIdInput);
                }

                formTagLength = 7;
                closeFormIndex = html.IndexOf("</form>", startIndex, StringComparison.Ordinal);
                if (closeFormIndex > -1)
                {
                    continue;
                }

                formTagLength = 8;
                closeFormIndex = html.IndexOf("</jform>", startIndex, StringComparison.Ordinal);
            }

            return html;
        }
    }
}