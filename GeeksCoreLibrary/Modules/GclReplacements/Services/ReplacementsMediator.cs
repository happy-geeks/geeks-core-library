using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Extensions;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.GclReplacements.Services;

/// <inheritdoc cref="IReplacementsMediator" />
public class ReplacementsMediator : IReplacementsMediator, IScopedService
{
    private readonly IDatabaseConnection databaseConnection;
    private readonly Regex logicSnippetRegex;
    private readonly Regex formatterRegex;
    private readonly MethodInfo[] formatters;

    private const string RawFormatterName = "Raw";

    /// <summary>
    /// Creates a new instance of <see cref="ReplacementsMediator"/>.
    /// </summary>
    public ReplacementsMediator(IDatabaseConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;

        formatters = typeof(StringReplacementsExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public);

        // Create some regular expressions so they can be re-used instead of creating them each time the function is called.
        formatterRegex = new Regex(@"(?<methodname>[^\(\)]+)(?:\((?<parameters>[^\)]+)\))?", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
        logicSnippetRegex = new Regex(@"\[if\((?<left>((?!\[if\().)*?)(?<op>=|!|<|>|&lt;|&gt;|%)(?<right>((?!\[if\().)*?)\)\](?<text>((?!\[if\().)*?)\[endif\]", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
    }

    /// <inheritdoc />
    public IEnumerable<IEnumerable<string>> DoReplacements(string input, DataSet replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}")
    {
        if (replaceData == null || replaceData.Tables.Count == 0)
        {
            yield break;
        }

        foreach (DataTable dataTable in replaceData.Tables)
        {
            yield return DoReplacements(input, dataTable, forQuery, caseSensitive, prefix, suffix);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> DoReplacements(string input, DataTable replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}")
    {
        if (replaceData == null || replaceData.Rows.Count == 0)
        {
            yield break;
        }

        foreach (DataRow dataRow in replaceData.Rows)
        {
            yield return DoReplacements(input, dataRow, forQuery, caseSensitive, prefix, suffix);
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
    public string DoReplacements(string input, IEnumerable<KeyValuePair<string, string>> replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}")
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

        return DoReplacements(input, dataDictionary, prefix, suffix, forQuery);
    }

    /// <inheritdoc />
    public string DoReplacements(string input, IEnumerable<KeyValuePair<string, StringValues>> replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}")
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

        return DoReplacements(input, dataDictionary, prefix, suffix, forQuery);
    }

    /// <inheritdoc />
    public string DoReplacements(string input, ISession replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}")
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

        return DoReplacements(input, dataDictionary, prefix, suffix, forQuery);
    }

    /// <inheritdoc />
    public string DoReplacements(string input, JToken replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}")
    {
        if (replaceData == null)
        {
            return input;
        }

        var output = input;
        var dataDictionary = new Dictionary<string, object>(caseSensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        // Get property values from the current level to use for replacements.
        foreach (var jToken in replaceData)
        {
            if (jToken is not JProperty item || item.Value.Type == JTokenType.Array || item.Value.Type == JTokenType.Object)
            {
                continue;
            }

            dataDictionary.Add(item.Name, item.Value);
        }

        // Do the replacements for the current level.
        output = DoReplacements(output, dataDictionary, prefix, suffix, forQuery);

        // Repeat the process for each object in the current level until the bottom is reached.
        foreach (var jToken in replaceData)
        {
            if (jToken is not JProperty item)
            {
                continue;
            }

            switch (item.Value.Type)
            {
                case JTokenType.Object:
                    output = DoReplacements(output, item.Value, forQuery, caseSensitive, prefix, suffix);
                    break;
                case JTokenType.Array:
                {
                    foreach (var subToken in item.Value)
                    {
                        if (subToken is not JObject subItem)
                        {
                            continue;
                        }

                        output = DoReplacements(output, subItem, forQuery, caseSensitive, prefix, suffix);
                    }

                    break;
                }
            }
        }

        return output;
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
                if (!String.IsNullOrEmpty(variable.DefaultValue))
                {
                    value = variable.DefaultValue;
                }

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

                var parameters = new List<object>(1 + (formatter.Parameters?.Length ?? 0)) {firstValue};
                if (formatter.Parameters?.Length > 0)
                {
                    parameters.AddRange(formatter.Parameters);
                }

                value = (string) formatter.Method.Invoke(null, parameters.ToArray());
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
    public string HandleVariablesDefaultValues(string input, string prefix = "{", string suffix = "}")
    {
        if (String.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var regex = new Regex($@"{prefix}([^\]{suffix}\s]*)\~([^\]{suffix}\s]*){suffix}", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
        foreach (Match match in regex.Matches(input))
        {
            input = input.Replace(match.Value, match.Groups[2].Value);
        }

        return input;
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

        var regex = new Regex($@"{prefix}[^\]{suffix}\s]*{suffix}", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));

        return regex.Replace(input, "");
    }

    /// <inheritdoc />
    public StringReplacementVariable[] GetReplacementVariables(string input, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode")
    {
        if (String.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<StringReplacementVariable>();
        }

        prefix = Regex.Escape(prefix);
        suffix = Regex.Escape(suffix);

        var regex = new Regex($@"{prefix}(?<field>[^\{{\}}]*?){suffix}", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));

        var result = new List<StringReplacementVariable>();
        foreach (Match match in regex.Matches(input))
        {
            var fieldName = match.Groups["field"].Value;
            var originalFieldName = fieldName;
            var variableFormatters = "";
            var defaultValue = "";

            // Checks for default values.
            var defaultValueSeparatorLocation = fieldName.LastIndexOf("~", StringComparison.Ordinal);
            if (defaultValueSeparatorLocation > 0) // This 0 is on purpose, it wouldn't make sense if the default value separator is the first character of the variable.
            {
                var colonIndexOf = fieldName.LastIndexOf(":", StringComparison.Ordinal);
                if (defaultValueSeparatorLocation + 1 > colonIndexOf)
                {
                    var defaultValueWithSeparator = colonIndexOf == -1 ? fieldName.Substring(defaultValueSeparatorLocation) : fieldName.Substring(defaultValueSeparatorLocation, colonIndexOf);
                    defaultValue = defaultValueWithSeparator.Remove(0, 1);
                    fieldName = fieldName.Remove(defaultValueSeparatorLocation, defaultValueWithSeparator.Length);
                }
            }

            // Colons that are escaped with a backslash are temporarily replaced with "~~COLON~~".
            fieldName = fieldName.Replace("\\:", "~~COLON~~");

            // Check if formatters are used. If the field ends with a colon, it's assumed to be part of the field name and not the formatter separator.
            // No check is performed to see if the formatters are valid, as that would slow things down too much.
            if (fieldName.Contains(':') && !fieldName.Trim().EndsWith(':'))
            {
                var lastColonIndex = fieldName.LastIndexOf(":", StringComparison.Ordinal);
                variableFormatters = fieldName[lastColonIndex..].TrimStart(':');
                fieldName = fieldName[..lastColonIndex];
            }

            var variable = new StringReplacementVariable
            {
                MatchString = match.Value,
                VariableName = fieldName,
                OriginalVariableName = originalFieldName,
                DefaultValue = defaultValue
            };

            // Now replace "~~COLON~~" with an actual colon again.
            variableFormatters = variableFormatters.Replace("~~COLON~~", ":");

            // Add the formatters to the list.
            variable.Formatters.AddRange(variableFormatters.Split('|', StringSplitOptions.RemoveEmptyEntries));

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

    /// <inheritdoc />
    public StringReplacementMethod GetFormatterMethod(string formatterString)
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
            return new StringReplacementMethod {Method = formatterMethod};
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
            return new StringReplacementMethod {Method = formatterMethod, Parameters = outputParameters.ToArray()};
        }

        // Not enough parameters probably means the optional parameters have not been added yet. Add default values for them.
        for (var i = outputParameters.Count; i < totalParameterCount; i++)
        {
            var methodParameter = methodParameters[i + 1];
            outputParameters.Add(methodParameter.DefaultValue);
        }

        return new StringReplacementMethod {Method = formatterMethod, Parameters = outputParameters.ToArray()};
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