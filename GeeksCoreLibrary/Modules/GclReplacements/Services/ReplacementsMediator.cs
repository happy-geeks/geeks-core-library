using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using GeeksCoreLibrary.Modules.GclReplacements.Enums;
using GeeksCoreLibrary.Modules.GclReplacements.Extensions;
using GeeksCoreLibrary.Modules.GclReplacements.Helpers;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Models;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.GclReplacements.Services;

/// <inheritdoc cref="IReplacementsMediator" />
public class ReplacementsMediator(
    IDatabaseConnection databaseConnection,
    ILogger<ReplacementsMediator> logger,
    IHttpContextAccessor httpContextAccessor = null)
    : IReplacementsMediator, IScopedService
{
    private static readonly FrozenDictionary<string, MethodInfo> Formatters = 
        typeof(StringReplacementsExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Select(m => new KeyValuePair<string, MethodInfo>(m.Name, m))
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private const string RawFormatterName = "Raw";

    /// <inheritdoc />
    public IEnumerable<IEnumerable<string>> DoReplacements(string input, DataSet replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
    {
        if (replaceData == null || replaceData.Tables.Count == 0)
        {
            yield break;
        }

        foreach (DataTable dataTable in replaceData.Tables)
        {
            yield return DoReplacements(input, dataTable, forQuery, caseSensitive, prefix, suffix, defaultFormatter, unsafeSource);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> DoReplacements(string input, DataTable replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
    {
        if (replaceData == null || replaceData.Rows.Count == 0)
        {
            yield break;
        }

        foreach (DataRow dataRow in replaceData.Rows)
        {
            yield return DoReplacements(input, dataRow, forQuery, caseSensitive, prefix, suffix, defaultFormatter, unsafeSource);
        }
    }

    /// <inheritdoc />
    public string DoReplacements(string input, DataRow replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
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

        return DoReplacements(input, dataDictionary, prefix, suffix, forQuery, defaultFormatter, unsafeSource);
    }

    /// <inheritdoc />
    public string DoReplacements(string input, NameValueCollection replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
    {
        if (replaceData == null)
        {
            return input;
        }

        var dataDictionary = new Dictionary<string, object>(caseSensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var key in replaceData.AllKeys.Where(k => !String.IsNullOrWhiteSpace(k)))
        {
            dataDictionary.Add(key, replaceData[key]);
        }

        return DoReplacements(input, dataDictionary, prefix, suffix, forQuery, defaultFormatter, unsafeSource);
    }

    /// <inheritdoc />
    public string DoReplacements(string input, IEnumerable<KeyValuePair<string, string>> replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
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

        return DoReplacements(input, dataDictionary, prefix, suffix, forQuery, defaultFormatter, unsafeSource);
    }

    /// <inheritdoc />
    public string DoReplacements(string input, IEnumerable<KeyValuePair<string, StringValues>> replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
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

        return DoReplacements(input, dataDictionary, prefix, suffix, forQuery, defaultFormatter, unsafeSource);
    }

    /// <inheritdoc />
    public string DoReplacements(string input, ISession replaceData, bool forQuery = false, bool caseSensitive = false, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
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

        return DoReplacements(input, dataDictionary, prefix, suffix, forQuery, defaultFormatter, unsafeSource);
    }

    /// <inheritdoc />
    public string DoReplacements(string input, JToken replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
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
        output = DoReplacements(output, dataDictionary, prefix, suffix, forQuery, defaultFormatter, unsafeSource);

        // Repeat the process for each object in the current level until the bottom is reached.
        foreach (var jToken in replaceData)
        {
            switch (jToken)
            {
                case JProperty item:
                    switch (item.Value.Type)
                    {
                        case JTokenType.Object:
                            output = DoReplacements(output, item.Value, forQuery, caseSensitive, prefix, suffix, defaultFormatter, unsafeSource);
                            break;
                        case JTokenType.Array:
                        {
                            foreach (var subToken in item.Value)
                            {
                                if (subToken is not JObject subItem)
                                {
                                    continue;
                                }

                                output = DoReplacements(output, subItem, forQuery, caseSensitive, prefix, suffix, defaultFormatter, unsafeSource);
                            }

                            break;
                        }
                    }

                    break;
                case JObject jObject:
                    output = DoReplacements(output, jObject, forQuery, caseSensitive, prefix, suffix, defaultFormatter, unsafeSource);
                    break;
                case JArray jArray:
                {
                    foreach (var subToken in jArray)
                    {
                        if (subToken is not JObject subItem)
                        {
                            continue;
                        }

                        output = DoReplacements(output, subItem, forQuery, caseSensitive, prefix, suffix, defaultFormatter, unsafeSource);
                    }

                    break;
                }
            }
        }

        return output;
    }

    /// <inheritdoc />
    public string DoReplacements(string input, IDictionary<string, object> replaceData, string prefix = "{", string suffix = "}", bool forQuery = false, string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null)
    {
        if (replaceData == null || replaceData.Count == 0)
        {
            return input;
        }

        // Find all replacement variables in the input string. Every replacement variable can also have multiple formatters.
        var variables = forQuery ? GetReplacementVariables(input, prefix, suffix, null) : GetReplacementVariables(input, prefix, suffix, defaultFormatter);
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

            if (unsafeSource.HasValue)
            {
                // If the replacement data is from an unsafe source, then skip any user related variables, to make sure that users can't overwrite these.
                if (variableName.StartsWith("Account_", StringComparison.OrdinalIgnoreCase) || variableName.StartsWith("AccountWiser2_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // If the value comes from un untrusted source (e.g. user input), we need to strip HTML tags to prevent XSS attacks.
                // If the replacements are for a query the inputs are protected using sql parameters so encoding can be skipped.
                if (!forQuery)
                {
                    value = value.StripHtml();
                    value = unsafeSource switch
                    {
                        UnsafeSources.HttpRequest => Uri.EscapeDataString(value),
                        _ => throw new ArgumentOutOfRangeException(nameof(unsafeSource), unsafeSource, null)
                    };
                }
            }

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

            if (skipFormatters || variable.Formatters.Count == 0)
            {
                // Simply replace the variable if there are no formatters found.
                if (forQuery)
                {
                    var parameterName = $"sql_{DatabaseHelpers.CreateValidParameterName(variable.VariableName)}";
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
                    var list = new List<string> {variableName};
                    list.AddRange(variable.Formatters);
                    var parameterName = $"sql_{DatabaseHelpers.CreateValidParameterName(String.Join("_", list))}";
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
        // Some checks to short circuit the evaluation
        if (String.IsNullOrWhiteSpace(input))
        {
            return input;
        }
        
        // We already check for an if statement to short-circuit templates without if statements
        // so we can pass in the found index into EvaluateTemplateInternal as an optimisation
        var firstIfIndex = input.IndexOf("[if(", StringComparison.Ordinal);
        if (firstIfIndex == -1)
        {
            return input;
        }

        var templateBuilder = new StringBuilder(input.Length);
        return EvaluateTemplateInternal(input, templateBuilder, firstIfIndex).ToString();
    }

    private StringBuilder EvaluateTemplateInternal(ReadOnlySpan<char> input, StringBuilder templateBuilder, int ifIndexOverride = -1)
    {
        while (!input.IsEmpty)
        {
            int ifIndex;
            if (ifIndexOverride != -1)
            {
                ifIndex = ifIndexOverride;
                ifIndexOverride = -1;
            }
            else
            {
                ifIndex = input.IndexOf("[if(", StringComparison.Ordinal);
            }

            if (ifIndex == -1)
            {
                return templateBuilder.Append(input);
            }

            templateBuilder.Append(input[..ifIndex]);
            input = input[ifIndex..];
            
            var parts = FindConditionalParts(input);

            var conditionPasses = false;
            decimal leftAsDecimal;
            decimal rightAsDecimal = 0;
            bool parseSuccessful;
            switch (parts.Operator)
            {
                case "=":
                    conditionPasses = parts.LeftOperand.SequenceEqual(parts.RightOperand);
                    break;
                case "!":
                    conditionPasses = !parts.LeftOperand.SequenceEqual(parts.RightOperand);
                    break;
                case "<":
                case "&lt;":
                    parseSuccessful = Decimal.TryParse(parts.LeftOperand, out leftAsDecimal);
                    parseSuccessful = parseSuccessful && Decimal.TryParse(parts.RightOperand, out rightAsDecimal);

                    conditionPasses = parseSuccessful && leftAsDecimal < rightAsDecimal;
                    break;
                case ">":
                case "&gt;":
                    parseSuccessful = Decimal.TryParse(parts.LeftOperand, out leftAsDecimal);
                    parseSuccessful = parseSuccessful && Decimal.TryParse(parts.RightOperand, out rightAsDecimal);

                    conditionPasses = parseSuccessful && leftAsDecimal > rightAsDecimal;
                    break;
                case "%":
                    conditionPasses = parts.LeftOperand.Contains(parts.RightOperand, StringComparison.Ordinal);
                    break;
                default:
                    logger.LogWarning($"Invalid conditional operator: {parts.Operator}.");
                    templateBuilder.Append(input[..parts.ScannedUntil]);
                    break;
            }

            ReadOnlySpan<char> conditionalPart;
            int nextFoundIndex;
            if (conditionPasses)
            {
                conditionalPart = parts.TrueBranchValue;
                nextFoundIndex = parts.NextFoundTrueBranchIfIndex;
            }
            else
            {
                conditionalPart = parts.FalseBranchValue;
                nextFoundIndex = parts.NextFoundFalseBranchIfIndex;
            }
            
            if (nextFoundIndex != -1)
            {
                templateBuilder = EvaluateTemplateInternal(conditionalPart, templateBuilder, nextFoundIndex);
            }
            else
            {
                templateBuilder.Append(conditionalPart);
            }

            input = input[parts.ScannedUntil..];
        }
        return templateBuilder;
    }

    private IfStatementParts FindConditionalParts(ReadOnlySpan<char> input)
    {
        var parts = new IfStatementParts();

        // 1. Find the opening [if( ... )] tag.
        var ifStart = input.IndexOf("[if(", StringComparison.Ordinal);
        if (ifStart == -1)
        {
            return parts;
        }

        // 2. Find the end of the if condition (look for ")]" after "[if(").
        var conditionEnd = input[ifStart..].IndexOf(")]", StringComparison.Ordinal);
        if (conditionEnd == -1)
        {
            logger.LogWarning("Invalid conditional: missing closing token for if condition.");
            parts.Operator = default;
            parts.ScannedUntil = ifStart + "[if(".Length;
            return parts;
        }
        // Adjust conditionEnd to be relative to the whole input.
        conditionEnd += ifStart;

        // 3. Extract the condition text inside the tag.
        // For example, for "[if(1=1)]" this yields "1=1".
        var conditionContent = input.Slice(
            ifStart + "[if(".Length, 
            conditionEnd - ifStart - "[if(".Length);

        // 4. Parse the condition into left operand, operator, and right operand.
        ParseCondition(conditionContent, out var leftOperand, out var op, out var rightOperand);
        parts.LeftOperand = leftOperand;
        parts.Operator = op;
        parts.RightOperand = rightOperand;

        // 5. Now scan for the true/false content.
        // The content starts immediately after the [if(…)] tag.
        var contentStart = conditionEnd + ")]".Length;
        var depth = 1;
        var elseIndex = -1;
        var endifIndex = -1;

        if (parts.Operator.IsEmpty)
        {
            parts.ScannedUntil = contentStart;
            return parts;
        }

        var contentSpan = input[contentStart..];
        foreach (var ifPartMatch in PrecompiledRegexes.ConditionalParts.EnumerateMatches(contentSpan))
        {
            var foundPart = contentSpan.Slice(ifPartMatch.Index, ifPartMatch.Length);
            
            switch (foundPart)
            {
                case "[if(":
                {
                    // A nested if: increase depth.
                    depth++;
                
                    // We've encountered an if statement so save it for the next scan
                    if (elseIndex == -1 && parts.NextFoundTrueBranchIfIndex == -1)
                    {
                        parts.NextFoundTrueBranchIfIndex = ifPartMatch.Index;
                    } 
                    else if (elseIndex != -1 && parts.NextFoundFalseBranchIfIndex == -1)
                    {
                        parts.NextFoundFalseBranchIfIndex = ifPartMatch.Index - (elseIndex + "[else]".Length);
                    }
                    break;
                }
                case "[else]":
                {
                    // Only treat an [else] at the outermost level.
                    if (depth == 1 && elseIndex == -1)
                    {
                        elseIndex = ifPartMatch.Index;
                    }
                    break;
                }
                case "[endif]":
                {
                    // A closing token: decrease depth.
                    depth--;
                    if (depth == 0)
                    {
                        endifIndex = ifPartMatch.Index;
                    }
                    break;
                }
            }

            // end index found so break out of the foreach
            if (endifIndex != -1)
            {
                break;
            }
        }

        // If no endIfEnd is found
        if (endifIndex == -1)
        {
            logger.LogWarning("Invalid conditional: missing closing [endif].");
            
            parts.Operator = default;
            
            if (parts.NextFoundTrueBranchIfIndex > -1)
            {
                parts.ScannedUntil = parts.NextFoundTrueBranchIfIndex;
            }
            else if (parts.NextFoundFalseBranchIfIndex > -1)
            {
                parts.ScannedUntil = parts.NextFoundFalseBranchIfIndex + elseIndex + "[else]".Length;
            }
            else
            {
                parts.ScannedUntil = contentSpan.Length;
            }
            
            parts.ScannedUntil += contentStart;
            return parts;
        }
    
        // 6. Slice out the true and false parts.
        if (elseIndex != -1)
        {
            // Everything after the [if(…)] tag up to the [else] is the true branch.
            parts.TrueBranchValue = contentSpan[..elseIndex];
            // Everything after the [else] up to the matching [endif] is the false branch.
            var falsePathStart = elseIndex + "[else]".Length;
            parts.FalseBranchValue = contentSpan.Slice(falsePathStart, endifIndex - falsePathStart);
        }
        else
        {
            // No [else] token: the true branch extends all the way to the [endif].
            parts.TrueBranchValue = contentSpan[..endifIndex];
        }
        
        parts.ScannedUntil = contentStart + endifIndex + "[endif]".Length;
    
        return parts;
    }

    /// <summary>
    /// Parses the condition inside the [if(…)] tag into left operand, operator, and right operand.
    /// </summary>
    private void ParseCondition(ReadOnlySpan<char> condition, 
        out ReadOnlySpan<char> left, 
        out ReadOnlySpan<char> @operator, 
        out ReadOnlySpan<char> right)
    {
        // First, check for longer operators (HTML encoded)
        var multiCharOps = new[] { "&lt;", "&gt;" };

        foreach (var op in multiCharOps)
        {
            var index = condition.IndexOf(op, StringComparison.Ordinal);
            if (index != -1)
            {
                left = condition[..index].Trim();
                @operator = condition.Slice(index, op.Length);
                right = condition[(index + op.Length)..].Trim();
                return;
            }
        }

        // Next, check for single-character operators.
        ReadOnlySpan<char> singleOps = ['=', '!', '<', '>', '%'];
        for (var i = 0; i < condition.Length; i++)
        {
            if (singleOps.Contains(condition[i]))
            {
                left = condition[..i].Trim();
                @operator = condition.Slice(i, 1);
                right = condition[(i + 1)..].Trim();
                return;
            }
        }

        logger.LogWarning("Invalid conditional: no operator found in the if condition.");
        left = default;
        @operator = default;
        right = default;
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
            return [];
        }

        prefix = Regex.Escape(prefix);
        suffix = Regex.Escape(suffix);

        var regex = new Regex($@"{prefix}(?<field>[^\{{\}}]+?){suffix}", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));

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
                var defaultValueWithSeparator = colonIndexOf == -1 || defaultValueSeparatorLocation + 1 > colonIndexOf
                    ? fieldName[defaultValueSeparatorLocation..]
                    : fieldName.Substring(defaultValueSeparatorLocation, colonIndexOf - defaultValueSeparatorLocation);

                defaultValue = defaultValueWithSeparator.Remove(0, 1);
                fieldName = fieldName.Remove(defaultValueSeparatorLocation, defaultValueWithSeparator.Length);
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
                && !variable.Formatters.Any(f => f != null && f.StartsWith("CurrencySup", StringComparison.OrdinalIgnoreCase))
                && !variable.Formatters.Any(f => f != null && f.StartsWith("UrlEncode", StringComparison.OrdinalIgnoreCase)))
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
        var match = PrecompiledRegexes.Formatters.Match(formatterString);

        if (!match.Success)
        {
            return null;
        }

        var methodName = match.Groups["methodname"].Value;
        if (!Formatters.TryGetValue(methodName, out var formatterMethod))
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
        if (formatterString.Contains('(', StringComparison.Ordinal))
        {
            parametersString = match.Groups["parameters"].Value;
            if (parametersString.Length == 0)
            {
                parametersString = null;
            }
        }

        var optionalParameterCount = methodParameters.Count(p => p.IsOptional);

        // The reason for the -1 is that the first parameter of an extension method is always the value of the type it's extending.
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
    /// Converts a string value to the given <see cref="Type"/>.
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
            input = DoReplacements(input, QueryHelpers.ParseQuery(wiserUriOverride.Query), forQuery, defaultFormatter: defaultFormatter);
        }
        else
        {
            input = DoReplacements(input, httpContextAccessor.HttpContext.Request.Query, forQuery, defaultFormatter: defaultFormatter);
        }

        // POST variables.
        if (httpContextAccessor.HttpContext.Request.HasFormContentType)
        {
            input = DoReplacements(input, httpContextAccessor.HttpContext.Request.Form, forQuery, defaultFormatter: defaultFormatter);
        }

        // Cookies.
        input = DoReplacements(input, httpContextAccessor.HttpContext.Request.Cookies, forQuery, defaultFormatter: defaultFormatter);

        // Request cache.
        input = DoReplacements(input, httpContextAccessor.HttpContext.Items.Select(x => new KeyValuePair<string, string>(x.Key?.ToString(), x.Value?.ToString())), forQuery, defaultFormatter: defaultFormatter);

        return input;
    }

    /// <inheritdoc />
    public string DoSessionReplacements(string input, bool forQuery = false, string defaultFormatter = "HtmlEncode")
    {
        if (httpContextAccessor?.HttpContext?.Features.Get<ISessionFeature>()?.Session == null || !httpContextAccessor.HttpContext.Session.IsAvailable)
        {
            return input;
        }

        return DoReplacements(input, httpContextAccessor.HttpContext.Session, forQuery, defaultFormatter: defaultFormatter);
    }
}