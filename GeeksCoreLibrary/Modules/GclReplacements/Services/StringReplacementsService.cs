using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.GclReplacements.Services
{
    /// <inheritdoc cref="IStringReplacementsService" />
    public class StringReplacementsService : IStringReplacementsService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly IObjectsService objectsService;
        private readonly ILanguagesService languagesService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAccountsService accountsService;
        private readonly IReplacementsMediator replacementsMediator;

        public StringReplacementsService(IOptions<GclSettings> gclSettings,
                                         IObjectsService objectsService,
                                         ILanguagesService languagesService,
                                         IAccountsService accountsService,
                                         IReplacementsMediator replacementsMediator,
                                         IHttpContextAccessor httpContextAccessor = null)
        {
            this.gclSettings = gclSettings.Value;
            this.objectsService = objectsService;
            this.languagesService = languagesService;
            this.httpContextAccessor = httpContextAccessor;
            this.accountsService = accountsService;
            this.replacementsMediator = replacementsMediator;
        }

        /// <inheritdoc />
        public async Task<string> DoAllReplacementsAsync(string input, DataRow dataRow = null, bool handleRequest = true, bool evaluateLogicSnippets = true, bool removeUnknownVariables = true, bool forQuery = false, string defaultFormatter = "HtmlEncode", bool handleVariableDefaults = true)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            // Reusable variables.
            Regex regex;
            var dataDictionary = new Dictionary<string, object>();

            // Defaults.
            var curDateTime = DateTime.Now;

            // Make sure the language code has a value.
            if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
            {
                // This function fills the property "CurrentLanguageCode".
                await languagesService.GetLanguageCodeAsync();
            }

            dataDictionary.Clear();
            dataDictionary.Add("NowDateTime", curDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            dataDictionary.Add("NowYear", curDateTime.Year.ToString());
            dataDictionary.Add("NowMonth", curDateTime.Month.ToString());
            dataDictionary.Add("NowDay", curDateTime.Day.ToString());
            dataDictionary.Add("LanguageCode", languagesService.CurrentLanguageCode);
            dataDictionary.Add("language_code", languagesService.CurrentLanguageCode);
            dataDictionary.Add("MlJclLanguageCode", languagesService.CurrentLanguageCode); // Legacy key for old library support.
            dataDictionary.Add("Hostname", HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext));
            dataDictionary.Add("Environment", (int)gclSettings.Environment);
            dataDictionary.Add("IpAddress", HttpContextHelpers.GetUserIpAddress(httpContextAccessor?.HttpContext));
            dataDictionary.Add("UserAgent", HttpContextHelpers.GetHeaderValueAs<string>(httpContextAccessor?.HttpContext, Microsoft.Net.Http.Headers.HeaderNames.UserAgent) ?? String.Empty);
            dataDictionary.Add("RelativeUrl", HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext).PathAndQuery);
            if (input.Contains("{Guid", StringComparison.OrdinalIgnoreCase) || input.Contains("{Uuid", StringComparison.OrdinalIgnoreCase))
            {
                var guid = Guid.NewGuid().ToString();
                dataDictionary.Add("Guid", guid);
                dataDictionary.Add("Uuid", guid);
            }
            input = replacementsMediator.DoReplacements(input, dataDictionary, forQuery: forQuery, defaultFormatter: defaultFormatter);

            // System object replaces.
            if (input.Contains("[SO{"))
            {
                dataDictionary.Clear();

                regex = new Regex(@"\[SO{([^\}]+)}]", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                foreach (Match m in regex.Matches(input))
                {
                    var value = m.Groups[1].Value;
                    
                    // Check if there are any valid formatter functions used in the variable and if so, use the variable name without the formatter as a system variable.
                    var replacementVariables = replacementsMediator.GetReplacementVariables($"{{{value}}}", defaultFormatter: defaultFormatter);
                    foreach (var variable in replacementVariables)
                    {
                        if (variable.Formatters.All(f => replacementsMediator.GetFormatterMethod(f) != null))
                        {
                            value = variable.VariableName;
                        }
                    }

                    if (dataDictionary.ContainsKey(value))
                    {
                        continue;
                    }

                    dataDictionary.Add(value, await objectsService.FindSystemObjectByDomainNameAsync(value.Replace("\\:", ":")));
                }

                input = replacementsMediator.DoReplacements(input, dataDictionary, "[SO{", "}]", forQuery: forQuery, defaultFormatter: defaultFormatter);
            }

            input = await accountsService.DoAccountReplacementsAsync(input, forQuery);

            // Do replacements multiple times to handle double replacements such as "{price:Currency(true,{culture})}".
            var counter = 0;
            while (input.Contains("{") && counter < 3)
            {
                counter++;

                // DataRow replacements.
                if (dataRow != null)
                {
                    input = replacementsMediator.DoReplacements(input, dataRow, forQuery, defaultFormatter: defaultFormatter);
                }

                // Request replacements.
                if (handleRequest && httpContextAccessor?.HttpContext != null)
                {
                    input = DoHttpRequestReplacements(input, forQuery, defaultFormatter);
                    input = DoSessionReplacements(input, forQuery, defaultFormatter);
                }

                // Translations.
                if (input.Contains("[T{"))
                {
                    dataDictionary.Clear();

                    regex = new Regex(@"\[T{([^\}]+)}]", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                    foreach (Match m in regex.Matches(input))
                    {
                        var value = m.Groups[1].Value;

                        // Check if there are any valid formatter functions used in the variable and if so, use the variable name without the formatter as translation.
                        var replacementVariables = replacementsMediator.GetReplacementVariables($"{{{value}}}", defaultFormatter: defaultFormatter);
                        foreach (var variable in replacementVariables)
                        {
                            if (variable.Formatters.All(f => replacementsMediator.GetFormatterMethod(f) != null))
                            {
                                value = variable.VariableName;
                            }
                        }

                        if (dataDictionary.ContainsKey(value))
                        {
                            continue;
                        }

                        dataDictionary.Add(value, await languagesService.GetTranslationAsync(value));
                    }

                    input = replacementsMediator.DoReplacements(input, dataDictionary, "[T{", "}]", forQuery: forQuery, defaultFormatter: defaultFormatter);
                }

                // CMS objects.
                if (input.Contains("[O{") && httpContextAccessor?.HttpContext != null)
                {
                    dataDictionary.Clear();

                    // Try to get the type number by host name.
                    if (!Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync(HttpContextHelpers.GetHostName(httpContextAccessor.HttpContext, includePort: false)), out var objectsTypeNumber) || objectsTypeNumber == 0)
                    {
                        // Revert to -100 if the parsing failed or if it returned 0. This is a special value that will look through all objects, ignoring the type number completely.
                        objectsTypeNumber = -100;
                    }

                    regex = new Regex(@"\[O{([^\}]+)}]", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                    foreach (Match m in regex.Matches(input))
                    {
                        var value = m.Groups[1].Value;
                        if (dataDictionary.ContainsKey(value))
                        {
                            continue;
                        }

                        // Check if there are any valid formatter functions used in the variable and if so, use the variable name without the formatter as object name.
                        var replacementVariables = replacementsMediator.GetReplacementVariables($"{{{value}}}", defaultFormatter: defaultFormatter);
                        foreach (var variable in replacementVariables)
                        {
                            if (variable.Formatters.All(f => replacementsMediator.GetFormatterMethod(f) != null))
                            {
                                value = variable.VariableName;
                            }
                        }

                        dataDictionary.Add(value, await objectsService.GetObjectValueAsync(value, objectsTypeNumber));
                    }

                    input = replacementsMediator.DoReplacements(input, dataDictionary, "[O{", "}]", forQuery: forQuery, defaultFormatter: defaultFormatter);
                }
            }

            // Handle variables with default values that haven't been replaced yet.
            if (handleVariableDefaults)
            {
                input = HandleVariablesDefaultValues(input);
            }

            // Whether template variables that were not replaced should be removed.
            if (removeUnknownVariables)
            {
                input = RemoveTemplateVariables(input);
            }

            // Evaluate the [if][endif] logic snippets.
            if (evaluateLogicSnippets)
            {
                input = EvaluateTemplate(input);
            }

            return input;
        }

        /// <inheritdoc />
        public string DoHttpRequestReplacements(string input, bool forQuery = false, string defaultFormatter = "HtmlEncode")
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return input;
            }

            // GET variables.
            if (httpContextAccessor.HttpContext.Items.ContainsKey(Constants.WiserUriOverrideForReplacements) && httpContextAccessor.HttpContext.Items[Constants.WiserUriOverrideForReplacements] is Uri wiserUriOverride)
            {
                input = replacementsMediator.DoReplacements(input, QueryHelpers.ParseQuery(wiserUriOverride.Query), forQuery, defaultFormatter: defaultFormatter);
            }
            else
            {
                input = replacementsMediator.DoReplacements(input, httpContextAccessor.HttpContext.Request.Query, forQuery, defaultFormatter: defaultFormatter);
            }

            // POST variables.
            if (httpContextAccessor.HttpContext.Request.HasFormContentType)
            {
                input = replacementsMediator.DoReplacements(input, httpContextAccessor.HttpContext.Request.Form, forQuery, defaultFormatter: defaultFormatter);
            }

            // Cookies.
            input = replacementsMediator.DoReplacements(input, httpContextAccessor.HttpContext.Request.Cookies, forQuery, defaultFormatter: defaultFormatter);

            // Request cache.
            input = replacementsMediator.DoReplacements(input, httpContextAccessor.HttpContext.Items.Select(x => new KeyValuePair<string, string>(x.Key?.ToString(), x.Value?.ToString())), forQuery, defaultFormatter: defaultFormatter);

            return input;
        }

        /// <inheritdoc />
        public string DoSessionReplacements(string input, bool forQuery = false, string defaultFormatter = "HtmlEncode")
        {
            if (httpContextAccessor?.HttpContext?.Features.Get<ISessionFeature>()?.Session == null || !httpContextAccessor.HttpContext.Session.IsAvailable)
            {
                return input;
            }

            return replacementsMediator.DoReplacements(input, httpContextAccessor.HttpContext.Session, forQuery, defaultFormatter: defaultFormatter);
        }

        /// <inheritdoc />
        public IEnumerable<IEnumerable<string>> DoReplacements(string input, DataSet replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
        {
            return replacementsMediator.DoReplacements(input, replaceData, forQuery, caseSensitive, prefix, suffix, defaultFormatter);
        }

        /// <inheritdoc />
        public IEnumerable<string> DoReplacements(string input, DataTable replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
        {
            return replacementsMediator.DoReplacements(input, replaceData, forQuery, caseSensitive, prefix, suffix, defaultFormatter);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, DataRow replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
        {
            return replacementsMediator.DoReplacements(input, replaceData, forQuery, caseSensitive, prefix, suffix, defaultFormatter);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, IEnumerable<KeyValuePair<string, string>> replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
        {
            return replacementsMediator.DoReplacements(input, replaceData, forQuery, caseSensitive, prefix, suffix, defaultFormatter);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, IEnumerable<KeyValuePair<string, StringValues>> replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
        {
            return replacementsMediator.DoReplacements(input, replaceData, forQuery, caseSensitive, prefix, suffix, defaultFormatter);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, ISession replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
        {
            return replacementsMediator.DoReplacements(input, replaceData, forQuery, caseSensitive, prefix, suffix, defaultFormatter);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, JToken replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
        {
            return replacementsMediator.DoReplacements(input, replaceData, forQuery, caseSensitive, prefix, suffix, defaultFormatter);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, IDictionary<string, object> replaceData, string prefix = "{", string suffix = "}", bool forQuery = false, string defaultFormatter = "HtmlEncode")
        {
            return replacementsMediator.DoReplacements(input, replaceData, prefix, suffix, forQuery, defaultFormatter);
        }

        /// <inheritdoc />
        public string EvaluateTemplate(string input)
        {
            return replacementsMediator.EvaluateTemplate(input);
        }

        /// <inheritdoc />
        public string HandleVariablesDefaultValues(string input, string prefix = "{", string suffix = "}")
        {
            return replacementsMediator.HandleVariablesDefaultValues(input, prefix, suffix);
        }

        /// <inheritdoc />
        public string RemoveTemplateVariables(string input, string prefix = "{", string suffix = "}")
        {
            return replacementsMediator.RemoveTemplateVariables(input, prefix, suffix);
        }

        /// <inheritdoc />
        public string FillStringByClassList(JToken input, string inputString, bool evaluateTemplate = false, string repeatVariableName = "repeat")
        {
            if (input == null || String.IsNullOrWhiteSpace(inputString))
            {
                return inputString;
            }

            var output = new StringBuilder();

            if (input.Type == JTokenType.Array)
            {
                var array = (JArray)input;

                var reg = new Regex($"(.*){{{repeatVariableName}}}(.*){{/{repeatVariableName}}}(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(30));
                var m = reg.Match(inputString);

                if (m.Success)
                {
                    // Repeat the subtemplate for each JObject in the JArray
                    var index = 0;

                    foreach (var item in array)
                    {
                        // For each item in the list call the FillStringByClass function with the subtemplate
                        var subtemplate = m.Groups[2].Value;
                        subtemplate = subtemplate.Replace("{index}", index.ToString());
                        subtemplate = subtemplate.Replace("{volgnr}", (index + 1).ToString());
                        subtemplate = subtemplate.Replace("{count}", "{~count~}"); // Temporary replace count variable, otherwise this variable is replaced by the FillStringByClass function
                        output.Append(FillStringByClass(item, subtemplate, evaluateTemplate).Replace("{~count~}", "{count}")); // Set back the count variable
                        index += 1;
                    }

                    output.Insert(0, m.Groups[1].Value);
                    output.Append(m.Groups[3].Value);
                    output.Replace("{count}", index.ToString());
                }
                else
                {
                    // Use only the first item in the JSON
                    output.Append(FillStringByClass(input.First, inputString, evaluateTemplate));
                }
            }
            else
            {
                output.Append(FillStringByClass(input, inputString, evaluateTemplate));
            }

            return output.ToString();
        }

        /// <inheritdoc />
        public string FillStringByClass(JToken input, string inputString, bool evaluateTemplate = false)
        {
            if (input == null || String.IsNullOrWhiteSpace(inputString))
            {
                return inputString;
            }

            var regexRepeats = new Regex(@"{repeat:([^\.]+?)}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));

            // Handle the repeaters, duplicate (parts of) the template.
            // First get all repeaters in string.
            foreach (Match repeater in regexRepeats.Matches(inputString))
            {
                var repeaterName = repeater.Groups[1].Value;

                // Find the repeats and duplicate the sub-templates as much as needed, the variables will be automatically numbered, example {orderlines(0).description}.
                foreach (Match m in Regex.Matches(inputString, $"{{repeat:{repeaterName}}}(.*?){{/repeat:{repeaterName}}}", RegexOptions.Singleline, TimeSpan.FromSeconds(30)))
                {
                    // Then loop through each repeater.
                    var subTemplate = m.Groups[1].Value;
                    var templates = new StringBuilder();
                    var index = 0;

                    var propertyValue = GetPropertyValue(input, repeaterName);
                    if (propertyValue == null)
                    {
                        continue;
                    }

                    if (propertyValue.Type != JTokenType.String)
                    {
                        foreach (var subObject in propertyValue)
                        {
                            // Prevention of replacing {repeaterName.count} by {repeaterName(0).count}: First replace by {~repeaterName.count~}, on the end replace back
                            var subTemplateItem = subTemplate;
                            subTemplateItem = subTemplateItem.Replace($"{{{repeaterName}.count}}", "{~" + repeaterName + ".count~}");
                            subTemplateItem = subTemplateItem.Replace($"{{{repeaterName}.index}}", index.ToString());
                            subTemplateItem = subTemplateItem.Replace($"{{{repeaterName}.volgnr}}", (index + 1).ToString());
                            subTemplateItem = subTemplateItem.Replace($"{{{repeaterName}.", "{");
                            subTemplateItem = subTemplateItem.Replace($"{{repeat:{repeaterName}", $"{{repeat:{repeaterName}({index})");
                            subTemplateItem = subTemplateItem.Replace($"{{/repeat:{repeaterName}", $"{{/repeat:{repeaterName}({index})");
                            subTemplateItem = subTemplateItem.Replace($"{{~{repeaterName}.count~}}", $"{{{repeaterName}.count}}");

                            subTemplateItem = FillStringByClassList(subObject, subTemplateItem, evaluateTemplate);

                            templates.Append(subTemplateItem);

                            index += 1;
                        }
                    }

                    inputString = inputString.Replace(m.Value, templates.ToString());

                    // Replace the count variable with the actual count
                    inputString = inputString.Replace($"{{{repeaterName}.count}}", index.ToString());
                }

                // Process the inner repeaters multiple levels deep (recursive) when input is JSON
                if (input.Type != JTokenType.Object)
                {
                    continue;
                }

                foreach (Match innerRepeater in regexRepeats.Matches(inputString.Replace($"{{repeat:{repeaterName}(0).", "{repeat:")))
                {
                    var innerRepeaterName = innerRepeater.Groups[1].Value;

                    // Then loop through each inner repeater
                    foreach (Match m in Regex.Matches(inputString, $"{{repeat:{repeaterName}\\(([0-9]+?)\\)\\.{innerRepeaterName}}}(.*?){{/repeat:{repeaterName}\\([0-9]+?\\)\\.{innerRepeaterName}}}", RegexOptions.Singleline, TimeSpan.FromSeconds(30)))
                    {
                        var index = Convert.ToInt32(m.Groups[1].Value);
                        var template = m.Value.Replace($"{repeaterName}({index}).", "");

                        var result = FillStringByClass(((JArray)((JObject)input)[repeaterName])[index], template, evaluateTemplate);

                        inputString = inputString.Replace(m.Value, result);
                    }
                }
            }

            // Replace all variables
            // Get matches like: {customer.address.streetline1}
            var regex = new Regex("{([^};]*[^};\\s])}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
            foreach (Match m in regex.Matches(inputString))
            {
                var value = GetPropertyValue(input, MakeColumnValueFromVariable(m.Value));
                var stringValue = "";

                if (value != null)
                {
                    if (value is JValue jValue)
                    {
                        if (jValue.Value != null)
                        {
                            stringValue = Convert.ToString(jValue.Value);
                        }
                    }
                    else
                    {
                        stringValue = value.ToString();
                    }

                    var variableName = MakeColumnValueFromVariable(m.Groups[1].Value);
                    var replacementData = new Dictionary<string, string>
                    {
                        {
                            variableName, stringValue
                        }
                    };

                    inputString = replacementsMediator.DoReplacements(inputString, replacementData, caseSensitive: false);
                }
            }

            // Evaluate template, working with if...else...then statements
            if (evaluateTemplate)
            {
                inputString = EvaluateTemplate(inputString);
            }

            return inputString;
        }

        /// <summary>
        /// Turns a variable including any optional formatters into a normal column name.
        /// E.g.: '{name:seo|encrypt}' becomes 'name'.
        /// </summary>
        /// <param name="variable">The variable to turn into a column name.</param>
        /// <param name="prefix">The prefix of the variable. Defaults to '{'.</param>
        /// <param name="suffix">The suffix of the variable. Defaults to '}'.</param>
        /// <returns></returns>
        private static string MakeColumnValueFromVariable(string variable, string prefix = "{", string suffix = "}")
        {
            var output = variable.Trim();

            if (output.StartsWith(prefix))
            {
                output = output[prefix.Length..];
            }
            if (output.EndsWith(suffix))
            {
                output = output[..^suffix.Length];
            }

            var colonIndex = output.LastIndexOf(':');
            if (colonIndex >= 0 && colonIndex != output.Length - 1)
            {
                output = output[..colonIndex];
            }

            return output;
        }

        /// <summary>
        /// Attempts to retrieve a value from the supplied <see cref="JToken"/> instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private static JToken GetPropertyValue(JToken input, string propertyName)
        {
            if (input == null)
            {
                return null;
            }

            if (propertyName.Contains(".") && !propertyName.Split('.')[0].Contains("("))
            {
                var propertyInfo = input.GetType().GetProperty(propertyName.Split('.')[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (propertyInfo == null)
                {
                    return null;
                }

                var newPropertyName = propertyName.Replace(propertyName.Split('.')[0] + ".", "");
                var value = GetPropertyValue((JToken)propertyInfo.GetValue(input, null), newPropertyName);
                return value != null ? value.ToString() : $"{{{propertyName}}}";
            }

            if (propertyName.Contains("("))
            {
                var regex = new Regex(@"(.*)\((\d.*)\)(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                var m = regex.Match(propertyName);
                if (!m.Success)
                {
                    return $"{{{propertyName}}}";
                }

                JToken innerValue;

                if (!String.IsNullOrWhiteSpace(m.Groups[3].Value))
                {
                    innerValue = GetPropertyValue(input, m.Groups[1].Value);
                    if (innerValue is not {Type: JTokenType.Array})
                    {
                        return null;
                    }

                    var innerArrayValue = ((JArray)innerValue)[Convert.ToInt32(m.Groups[2].Value)];
                    return GetPropertyValue(innerArrayValue, m.Groups[3].Value.TrimStart('.'));
                }

                innerValue = GetPropertyValue(input, m.Groups[1].Value);
                return innerValue.Type == JTokenType.Array
                    ? ((JArray)innerValue)[Convert.ToInt32(m.Groups[2].Value)].ToString()
                    : $"{{{propertyName}}}";
            }

            if (input.Type == JTokenType.Object)
            {
                var innerObject = (JObject)input;
                return innerObject[propertyName];
            }

            if (input.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null)
            {
                return input.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(input, null) as JToken;
            }

            return $"{{{propertyName}}}";
        }
    }
}