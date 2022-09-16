using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Extensions;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.GclReplacements.Services
{
    /// <inheritdoc cref="IStringReplacementsService" />
    public class StringReplacementsService : IStringReplacementsService, IScopedService
    {
        private readonly IObjectsService objectsService;
        private readonly ILanguagesService languagesService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAccountsService accountsService;
        private readonly IDatabaseConnection databaseConnection;

        private readonly MethodInfo[] formatters;

        private readonly Regex formatterRegex;
        private readonly Regex logicSnippetRegex;

        private const string RawFormatterName = "Raw";

        public StringReplacementsService(IObjectsService objectsService, ILanguagesService languagesService, IHttpContextAccessor httpContextAccessor, IAccountsService accountsService, IDatabaseConnection databaseConnection)
        {
            this.objectsService = objectsService;
            this.languagesService = languagesService;
            this.httpContextAccessor = httpContextAccessor;
            this.accountsService = accountsService;
            this.databaseConnection = databaseConnection;

            formatters = typeof(StringReplacementsExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public);

            // Create some regular expressions so they can be re-used instead of creating them each time the function is called.
            formatterRegex = new Regex(@"(?<methodname>[^\(\)]+)(?:\((?<parameters>[^\)]+)\))?");
            logicSnippetRegex = new Regex(@"\[if\((?<left>((?!\[if\().)*?)(?<op>=|!|<|>|&lt;|&gt;|%)(?<right>((?!\[if\().)*?)\)\](?<text>((?!\[if\().)*?)\[endif\]", RegexOptions.Singleline);
        }

        /// <inheritdoc />
        public async Task<string> DoAllReplacementsAsync(string input, DataRow dataRow = null, bool handleRequest = true, bool evaluateLogicSnippets = true, bool removeUnknownVariables = true, bool forQuery = false)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            // Reusable variables.
            Regex r;
            var dataDictionary = new Dictionary<string, object>();

            // Defaults.
            var curDateTime = DateTime.Now;

            dataDictionary.Clear();
            dataDictionary.Add("NowDateTime", curDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            dataDictionary.Add("NowYear", curDateTime.Year.ToString());
            dataDictionary.Add("NowMonth", curDateTime.Month.ToString());
            dataDictionary.Add("NowDay", curDateTime.Day.ToString());
            dataDictionary.Add("LanguageCode", languagesService.CurrentLanguageCode);
            dataDictionary.Add("language_code", languagesService.CurrentLanguageCode);
            dataDictionary.Add("Hostname", HttpContextHelpers.GetHostName(httpContextAccessor.HttpContext));
            input = DoReplacements(input, dataDictionary, forQuery: forQuery);

            // System object replaces.
            if (input.Contains("[SO{"))
            {
                dataDictionary.Clear();

                r = new Regex(@"\[SO{([^\}]+)}]");
                foreach (Match m in r.Matches(input))
                {
                    var value = m.Groups[1].Value;
                    if (dataDictionary.ContainsKey(value))
                    {
                        continue;
                    }

                    dataDictionary.Add(value, await objectsService.FindSystemObjectByDomainNameAsync(value.Replace("\\:", ":")));
                }

                input = DoReplacements(input, dataDictionary, "[SO{", "}]", forQuery: forQuery);
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
                    input = DoReplacements(input, dataRow, forQuery);
                }

                // Request replacements.
                if (handleRequest && httpContextAccessor.HttpContext != null)
                {
                    input = DoHttpRequestReplacements(input, forQuery);
                    input = DoSessionReplacements(input, forQuery);
                }

                // Translations.
                if (input.Contains("[T{"))
                {
                    dataDictionary.Clear();

                    r = new Regex(@"\[T{([^\}]+)}]");
                    foreach (Match m in r.Matches(input))
                    {
                        var value = m.Groups[1].Value;
                        if (dataDictionary.ContainsKey(value))
                        {
                            continue;
                        }

                        dataDictionary.Add(value, await languagesService.GetTranslationAsync(value));
                    }

                    input = DoReplacements(input, dataDictionary, "[T{", "}]", forQuery: forQuery);
                }

                // CMS objects.
                if (input.Contains("[O{") && httpContextAccessor.HttpContext != null)
                {
                    dataDictionary.Clear();

                    // Try to get the type number by host name.
                    if (!Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync(HttpContextHelpers.GetHostName(httpContextAccessor.HttpContext)), out var objectsTypeNumber) || objectsTypeNumber == 0)
                    {
                        // Revert to -100 if the parsing failed or if it returned 0. This is a special value that will look through all objects, ignoring the type number completely.
                        objectsTypeNumber = -100;
                    }

                    r = new Regex(@"\[O{([^\}]+)}]");
                    foreach (Match m in r.Matches(input))
                    {
                        var value = m.Groups[1].Value;
                        if (dataDictionary.ContainsKey(value))
                        {
                            continue;
                        }

                        dataDictionary.Add(value, await objectsService.GetObjectValueAsync(value, objectsTypeNumber));
                    }

                    input = DoReplacements(input, dataDictionary, "[O{", "}]", forQuery: forQuery);
                }
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
        public string DoHttpRequestReplacements(string input, bool forQuery = false)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return input;
            }

            // GET variables.
            if (httpContextAccessor.HttpContext.Items.ContainsKey(Constants.WiserUriOverrideForReplacements) && httpContextAccessor.HttpContext.Items[Constants.WiserUriOverrideForReplacements] is Uri wiserUriOverride)
            {
                input = DoReplacements(input, QueryHelpers.ParseQuery(wiserUriOverride.Query), forQuery);
            }
            else
            {
                input = DoReplacements(input, httpContextAccessor.HttpContext.Request.Query, forQuery);
            }

            // POST variables.
            if (httpContextAccessor.HttpContext.Request.HasFormContentType)
            {
                input = DoReplacements(input, httpContextAccessor.HttpContext.Request.Form, forQuery);
            }

            // Cookies.
            input = DoReplacements(input, httpContextAccessor.HttpContext.Request.Cookies, forQuery);

            // Request cache.
            input = DoReplacements(input, httpContextAccessor.HttpContext.Items.Select(x => new KeyValuePair<string, string>(x.Key?.ToString(), x.Value?.ToString())), forQuery);

            return input;
        }

        /// <inheritdoc />
        public string DoSessionReplacements(string input, bool forQuery = false)
        {
            if (httpContextAccessor.HttpContext?.Features.Get<ISessionFeature>() == null || !httpContextAccessor.HttpContext.Session.IsAvailable)
            {
                return input;
            }

            return DoReplacements(input, httpContextAccessor.HttpContext.Session, forQuery);
        }

        /// <inheritdoc />
        public IEnumerable<IEnumerable<string>> DoReplacements(string input, DataSet replaceData, bool forQuery = false, bool caseSensitive = false)
        {
            if (replaceData == null || replaceData.Tables.Count == 0)
            {
                yield break;
            }

            foreach (DataTable dataTable in replaceData.Tables)
            {
                yield return DoReplacements(input, dataTable, forQuery, caseSensitive);
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> DoReplacements(string input, DataTable replaceData, bool forQuery = false, bool caseSensitive = false)
        {
            if (replaceData == null || replaceData.Rows.Count == 0)
            {
                yield break;
            }

            foreach (DataRow dataRow in replaceData.Rows)
            {
                yield return DoReplacements(input, dataRow, forQuery, caseSensitive);
            }
        }

        /// <inheritdoc />
        public string DoReplacements(string input, DataRow replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}")
        {
            if (replaceData == null)
            {
                return input;
            }

            var dataDictionary = new Dictionary<string, object>(caseSensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (var column in replaceData.Table.Columns.Cast<DataColumn>())
            {
                dataDictionary.Add(column.ColumnName, replaceData[column]);
            }

            return DoReplacements(input, dataDictionary, prefix, suffix, forQuery);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, IEnumerable<KeyValuePair<string, string>> replaceData, bool forQuery = false, bool caseSensitive = false)
        {
            if (replaceData == null)
            {
                return input;
            }

            var dataDictionary = new Dictionary<string, object>(caseSensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (var (key, value) in replaceData)
            {
                dataDictionary.Add(key, value);
            }
            return DoReplacements(input, dataDictionary, forQuery: forQuery);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, IEnumerable<KeyValuePair<string, StringValues>> replaceData, bool forQuery = false, bool caseSensitive = false)
        {
            if (replaceData == null)
            {
                return input;
            }

            var dataDictionary = new Dictionary<string, object>(caseSensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (var (key, value) in replaceData)
            {
                dataDictionary.Add(key, value.ToString());
            }
            return DoReplacements(input, dataDictionary, forQuery: forQuery);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, ISession replaceData, bool forQuery = false, bool caseSensitive = false)
        {
            var dataDictionary = new Dictionary<string, object>(caseSensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (var item in replaceData.Keys)
            {
                if (!replaceData.TryGetValue(item, out var rawValue))
                {
                    continue;
                }

                dataDictionary.Add(item, Encoding.UTF8.GetString(rawValue));
            }
            return DoReplacements(input, dataDictionary, forQuery: forQuery);
        }

        /// <inheritdoc />
        public string DoReplacements(string input, IDictionary<string, object> replaceData, string prefix = "{", string suffix = "}", bool forQuery = false)
        {
            if (replaceData == null || replaceData.Count == 0)
            {
                return input;
            }

            // Find all replacement variables in the input string. Every replacement variable can also have multiple formatters.
            var variables = forQuery ? GetReplacementVariables(input, prefix, suffix, null) : GetReplacementVariables(input, prefix, suffix);
            if (variables.Length == 0)
            {
                return input;
            }

            var output = new StringBuilder(input);
            foreach (var variable in variables)
            {
                // A variable is skipped if it exists in the input string, but not in the provided data.
                if (!replaceData.ContainsKey(variable.VariableName) && !replaceData.ContainsKey(variable.OriginalVariableName))
                {
                    continue;
                }

                string variableName;
                bool skipFormatters;
                if (replaceData.ContainsKey(variable.VariableName))
                {
                    // Variable matches on match up until last colon (e.g.: "foo:bar" in "foo:bar:seo").
                    // Formatters will be used in this scenario.
                    variableName = variable.VariableName;
                    skipFormatters = false;
                }
                else
                {
                    // Variable matches on full match (e.g. "foo:bar"); Use original match and skip formatters.
                    variableName = variable.OriginalVariableName;
                    skipFormatters = true;
                }

                var value = Convert.ToString(replaceData[variableName], new CultureInfo("en-US"));
                if (String.IsNullOrWhiteSpace(value))
                {
                    // Replace the variable if it's an empty string or if it only contains whitespace and continue with the next variable.
                    output.Replace(variable.MatchString, value);
                    continue;
                }

                if (skipFormatters || !variable.Formatters.Any())
                {
                    // Simply replace the variable if there are no formatters found.
                    if (forQuery)
                    {
                        var parameterName = $"sql_{DatabaseHelpers.CreateValidParameterName(variable.MatchString)}";
                        databaseConnection.AddParameter(parameterName, value);
                        value = $"?{parameterName}";

                        // Make sure there won't be quotes around the variable in the query, otherwise it will be seen as a literal string by MySql.
                        output.Replace($"'{variable.MatchString}'", value).Replace($"\"{variable.MatchString}\"", value);
                    }

                    output.Replace(variable.MatchString, value);
                    continue;
                }

                var hasRawFormatter = false;
                foreach (var formatterString in variable.Formatters)
                {
                    var formatter = GetFormatterMethod(formatterString);
                    if (String.Equals(formatterString, RawFormatterName, StringComparison.OrdinalIgnoreCase))
                    {
                        hasRawFormatter = true;
                    }

                    if (formatter == null)
                    {
                        // Simply ignore the formatter if no method can be found with the formatter name.
                        continue;
                    }

                    // Get the type of the first parameter.
                    var firstValue = ConvertValue(value, formatter.Method.GetParameters()[0].ParameterType);

                    var parameters = new List<object>(1 + (formatter.Parameters?.Length ?? 0)) { firstValue };
                    if (formatter.Parameters?.Length > 0)
                    {
                        parameters.AddRange(formatter.Parameters);
                    }

                    value = (string)formatter.Method.Invoke(null, parameters.ToArray());
                }

                if (forQuery)
                {
                    if (hasRawFormatter)
                    {
                        // By default, we use SQL parameters for all replacements in a query. Developers can use the "Raw" formatter to prevent that.
                        // This is required in certain situations, such as getting information from a dynamic column (for example: "SELECT `name_{LanguageCode}` FROM x WHERE y").
                        // IMPORTANT NOTE: This is less secure than SQL parameters. To prevent SQL injection attacks, developers need to make sure that they add quotes/backticks around the entire value/replacement/column name, just like in the example above.
                        value = value.ToMySqlSafeValue(false);
                    }
                    else
                    {
                        var parameterName = $"sql_{DatabaseHelpers.CreateValidParameterName(variable.MatchString)}";
                        databaseConnection.AddParameter(parameterName, value);
                        value = $"?{parameterName}";

                        // Make sure there won't be quotes around the variable in the query, otherwise it will be seen as a literal string by MySql.
                        output.Replace($"'{variable.MatchString}'", value).Replace($"\"{variable.MatchString}\"", value);
                    }
                }

                output.Replace(variable.MatchString, value);
            }

            return output.ToString();
        }

        /// <inheritdoc />
        public string EvaluateTemplate(string input)
        {
            if (String.IsNullOrWhiteSpace(input) || !input.Contains("[if(", StringComparison.Ordinal))
            {
                return input;
            }

            var result = input;

            var leftAsDecimal = 0M;
            var rightAsDecimal = 0M;

            var matches = logicSnippetRegex.Matches(result);

            var infiniteLoopProtection = 0;
            while (matches.Count > 0)
            {
                if (infiniteLoopProtection++ > 250)
                {
                    break;
                }

                foreach (Match regexMatch in matches)
                {
                    var leftPart = regexMatch.Groups["left"].Value;
                    var rightPart = regexMatch.Groups["right"].Value;
                    var op = regexMatch.Groups["op"].Value;
                    var text = regexMatch.Groups["text"].Value;

                    var parseSuccessful = true;
                    if (op.InList("<", ">", "&lt;", "&gt;"))
                    {
                        parseSuccessful = Decimal.TryParse(leftPart.Trim(), out leftAsDecimal);
                        parseSuccessful = parseSuccessful && Decimal.TryParse(rightPart.Trim(), out rightAsDecimal);
                    }

                    var conditionPasses = false;
                    switch (op)
                    {
                        case "=":
                            conditionPasses = leftPart.Equals(rightPart);
                            break;
                        case "!":
                            conditionPasses = !leftPart.Equals(rightPart);
                            break;
                        case "<":
                        case "&lt;":
                            conditionPasses = parseSuccessful && leftAsDecimal < rightAsDecimal;
                            break;
                        case ">":
                        case "&gt;":
                            conditionPasses = parseSuccessful && leftAsDecimal > rightAsDecimal;
                            break;
                        case "%":
                            conditionPasses = leftPart.Contains(rightPart, StringComparison.Ordinal);
                            break;
                    }

                    if (text.Contains("[else]", StringComparison.Ordinal))
                    {
                        var index = text.IndexOf("[else]", StringComparison.Ordinal);
                        var truePart = text.Substring(0, index);
                        var falsePart = text[(index + 6)..];

                        result = result.Replace(regexMatch.Value, conditionPasses ? truePart : falsePart);
                    }
                    else
                    {
                        result = result.Replace(regexMatch.Value, conditionPasses ? text : "");
                    }
                }

                matches = logicSnippetRegex.Matches(result);
            }

            return result;
        }

        /// <inheritdoc />
        public string RemoveTemplateVariables(string input, string prefix = "{", string suffix = "}")
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            prefix = Regex.Escape(prefix);
            suffix = Regex.Escape(suffix);

            var regex = new Regex($@"{prefix}[^\]{suffix}\s]*{suffix}");

            return regex.Replace(input, "");
        }

        /// <inheritdoc />
        public string FillStringByClassList(JToken input, string inputString, bool evaluateTemplate = false, string repeatVariableName = "repeat")
        {
            var output = "";

            if (input.Type == JTokenType.Array)
            {
                var array = (JArray)input;

                var reg = new Regex($"(.*){{{repeatVariableName}}}(.*){{/{repeatVariableName}}}(.*)", RegexOptions.Singleline);
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
                        output += FillStringByClass(item, subtemplate).Replace("{~count~}", "{count}"); // Set back the count variable
                        index += 1;
                    }

                    output = m.Groups[1].Value + output + m.Groups[3].Value;
                    output = output.Replace("{count}", index.ToString());
                }
                else
                {
                    // Use only the first item in the JSON
                    output = FillStringByClass(input.First, inputString);
                }
            }
            else
            {
                output = FillStringByClass(input, inputString, evaluateTemplate);
            }

            return output;
        }

        /// <inheritdoc />
        public string FillStringByClass(JToken input, string inputString, bool evaluateTemplate = false)
        {
            var regexRepeats = new Regex(@"{repeat:([^\.]+?)}");

            // Handle the repeaters, duplicate (parts of) the template.
            // First get all repeaters in string.
            foreach (Match repeater in regexRepeats.Matches(inputString))
            {
                var repeaterName = repeater.Groups[1].Value;

                // Find the repeats and duplicate the sub-templates as much as needed, the variables will be automatically numbered, example {orderlines(0).description}.
                foreach (Match m in Regex.Matches(inputString, $"{{repeat:{repeaterName}}}(.*?){{/repeat:{repeaterName}}}", RegexOptions.Singleline))
                {
                    // Then loop through each repeater.
                    var subTemplate = m.Groups[1].Value;
                    var templates = new StringBuilder();
                    var index = 0;

                    if (GetPropertyValue(input, repeaterName).Type != JTokenType.String)
                    {
                        var propertyValue = GetPropertyValue(input, repeaterName);
                        if (propertyValue != null)
                        {
                            foreach (var unused in propertyValue)
                            {
                                // Prevention of replacing {repeaterName.count} by {repeaterName(0).count}: First replace by {~repeaterName.count~}, on the end replace back
                                var subTemplateItem = subTemplate;
                                subTemplateItem = subTemplateItem.Replace($"{{{repeaterName}.count}}", "{~" + repeaterName + ".count~}");
                                subTemplateItem = subTemplateItem.Replace($"{{{repeaterName}.index}}", index.ToString());
                                subTemplateItem = subTemplateItem.Replace($"{{{repeaterName}.volgnr}}", (index + 1).ToString());
                                subTemplateItem = subTemplateItem.Replace($"{{{repeaterName}", $"{{{repeaterName}({index})");
                                subTemplateItem = subTemplateItem.Replace($"{{repeat:{repeaterName}", $"{{repeat:{repeaterName}({index})");
                                subTemplateItem = subTemplateItem.Replace($"{{/repeat:{repeaterName}", $"{{/repeat:{repeaterName}({index})");
                                subTemplateItem = subTemplateItem.Replace($"{{~{repeaterName}.count~}}", $"{{{repeaterName}.count}}");

                                templates.Append(subTemplateItem);

                                index += 1;
                            }
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
                    foreach (Match m in Regex.Matches(inputString, $"{{repeat:{repeaterName}\\(([0-9]+?)\\)\\.{innerRepeaterName}}}(.*?){{/repeat:{repeaterName}\\([0-9]+?\\)\\.{innerRepeaterName}}}", RegexOptions.Singleline))
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
            var regex = new Regex("{(.[^}]*)}");
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
                }

                var variableName = MakeColumnValueFromVariable(m.Groups[1].Value);
                var replacementData = new Dictionary<string, string>
                {
                    {
                        variableName, stringValue
                    }
                };

                inputString = DoReplacements(inputString, replacementData, caseSensitive: false);
            }

            // Evaluate template, working with if...else...then statements
            if (evaluateTemplate)
            {
                EvaluateTemplate(inputString);
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
        /// Checks for any replacement variables in a string and returns an array of <see cref="StringReplacementVariable"/>.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <param name="prefix">The prefix of replacement variables. The default is '{'.</param>
        /// <param name="suffix">The suffix of replacement variables. The default is '}'.</param>
        /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
        /// <returns></returns>
        private static StringReplacementVariable[] GetReplacementVariables(string input, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<StringReplacementVariable>();
            }

            prefix = Regex.Escape(prefix);
            suffix = Regex.Escape(suffix);

            var regex = new Regex($@"{prefix}(?<field>[^\{{\}}]*?){suffix}");

            var result = new List<StringReplacementVariable>();
            foreach (Match match in regex.Matches(input))
            {
                var fieldName = match.Groups["field"].Value;
                var originalFieldName = fieldName;
                var formatters = "";

                // Colons that are escaped with a backslash are temporarily replaced with "~~COLON~~".
                fieldName = fieldName.Replace("\\:", "~~COLON~~");

                // Check if formatters are used. If the field ends with a colon, it's assumed to be part of the field name and not the formatter separator.
                // No check is performed to see if the formatters are valid, as that would slow things down too much.
                if (fieldName.Contains(":") && !fieldName.Trim().EndsWith(":"))
                {
                    var lastColonIndex = fieldName.LastIndexOf(":", StringComparison.Ordinal);
                    formatters = fieldName[lastColonIndex..].TrimStart(':');
                    fieldName = fieldName[..lastColonIndex];
                }

                var variable = new StringReplacementVariable
                {
                    MatchString = match.Value,
                    VariableName = fieldName,
                    OriginalVariableName = originalFieldName
                };

                // Now replace "~~COLON~~" with an actual colon again.
                formatters = formatters.Replace("~~COLON~~", ":");

                // Add the formatters to the list.
                variable.Formatters.AddRange(formatters.Split('|', StringSplitOptions.RemoveEmptyEntries));

                // Add the default formatter, unless the raw formatter has been used.
                if (!String.IsNullOrWhiteSpace(defaultFormatter)
                    && !variable.Formatters.Any(f => String.Equals(f, defaultFormatter, StringComparison.OrdinalIgnoreCase))
                    && !variable.Formatters.Any(f => String.Equals(f, RawFormatterName, StringComparison.OrdinalIgnoreCase))
                    && !variable.Formatters.Any(f => f != null && f.StartsWith("CurrencySup", StringComparison.OrdinalIgnoreCase)))
                {
                    variable.Formatters.Add(defaultFormatter);
                }

                result.Add(variable);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Attempts to retrieve a value from the supplied <see cref="JToken"/> instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public JToken GetPropertyValue(JToken input, string propertyName)
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
                var regex = new Regex(@"(.*)\((\d.*)\)(.*)");
                var m = regex.Match(propertyName);
                if (!m.Success)
                {
                    return $"{{{propertyName}}}";
                }

                JToken innerValue;

                if (!String.IsNullOrWhiteSpace(m.Groups[3].Value))
                {
                    innerValue = GetPropertyValue(input, m.Groups[1].Value);
                    if (innerValue.Type != JTokenType.Array)
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


        /// <summary>
        /// Attempts to find a formatter method (a method in <see cref="StringReplacementsExtensions"/>) to be used in a string replacement snippet.
        /// </summary>
        /// <param name="formatterString"></param>
        /// <returns></returns>
        private StringReplacementMethod GetFormatterMethod(string formatterString)
        {
            var match = formatterRegex.Match(formatterString);

            if (!match.Success)
            {
                return null;
            }

            var methodName = match.Groups["methodname"].Value;
            var formatterMethod = formatters.FirstOrDefault(f => f.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            if (formatterMethod == null)
            {
                return null;
            }

            var methodParameters = formatterMethod.GetParameters();

            // Method has no additional parameter (aside from the first one, which is the object value itself).
            // Return object with only the method.
            if (methodParameters.Length == 1)
            {
                return new StringReplacementMethod { Method = formatterMethod };
            }

            string parametersString = null;
            if (formatterString.Contains("(", StringComparison.Ordinal))
            {
                parametersString = match.Groups["parameters"].Value;
                if (parametersString.Length == 0)
                {
                    parametersString = null;
                }
            }

            var optionalParameterCount = methodParameters.Count(p => p.IsOptional);

            // The reason for the -1 is because the first parameter of an extension method is always the value of the type it's extending.
            // Therefore, that parameter should not be added in the count.
            var totalParameterCount = methodParameters.Length - 1;
            var minimumParameterCount = totalParameterCount - optionalParameterCount;
            var outputParameters = new List<object>(methodParameters.Length);

            if (parametersString == null && minimumParameterCount > 0)
            {
                var message = optionalParameterCount > 0
                    ? $"Parameter count mismatch for '{methodName}'. Expected at least {minimumParameterCount}, but got 0."
                    : $"Parameter count mismatch for '{methodName}'. Expected {totalParameterCount}, but got 0.";

                throw new TargetParameterCountException(message);
            }

            if (parametersString != null)
            {
                // To ensure commas can be used as strings, they can be escaped with a backslash.
                // To allow this, any instances of "\," are first replaced with "~~COMMA~~" so they're not used when splitting on ",".
                parametersString = parametersString.Replace("\\,", "~~COMMA~~");

                var parameters = parametersString.Split(',');

                if (parameters.Length < minimumParameterCount || parameters.Length > totalParameterCount)
                {
                    var message = optionalParameterCount > 0
                        ? $"Parameter count mismatch for '{methodName}'. Expected at least {minimumParameterCount}, but got {parameters.Length}."
                        : $"Parameter count mismatch for '{methodName}'. Expected {totalParameterCount}, but got {parameters.Length}.";

                    throw new TargetParameterCountException(message);
                }

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterString = parameters[i].Trim().Replace("~~COMMA~~", ",");
                    var methodParameter = methodParameters[i + 1];
                    outputParameters.Add(ConvertValue(parameterString, methodParameter.ParameterType));
                }
            }

            if (outputParameters.Count >= totalParameterCount)
            {
                return new StringReplacementMethod { Method = formatterMethod, Parameters = outputParameters.ToArray() };
            }

            // Not enough parameters probably means the optional parameters have not been added yet. Add default values for them.
            for (var i = outputParameters.Count; i < totalParameterCount; i++)
            {
                var methodParameter = methodParameters[i + 1];
                outputParameters.Add(methodParameter.DefaultValue);
            }
            return new StringReplacementMethod { Method = formatterMethod, Parameters = outputParameters.ToArray() };
        }

        /// <summary>
        /// Converts the type of a string value to the given <see cref="Type"/>.
        /// </summary>
        /// <param name="input">The string that needs conversion.</param>
        /// <param name="type">The <see cref="Type"/> the string needs be converted to.</param>
        /// <returns></returns>
        private static object ConvertValue(string input, Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(decimal))
            {
                return Convert.ToDecimal(input, new CultureInfo("en-US"));
            }
            if (type == typeof(double))
            {
                return Convert.ToDouble(input, new CultureInfo("en-US"));
            }
            if (type == typeof(DateTime))
            {
                return Convert.ToDateTime(input, new CultureInfo("en-US"));
            }

            return Convert.ChangeType(input, type);
        }
    }
}
