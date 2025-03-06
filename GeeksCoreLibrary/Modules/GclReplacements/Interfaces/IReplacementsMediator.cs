using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using GeeksCoreLibrary.Modules.GclReplacements.Enums;
using GeeksCoreLibrary.Modules.GclReplacements.Extensions;
using GeeksCoreLibrary.Modules.GclReplacements.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.GclReplacements.Interfaces;

/// <summary>
/// This is an intermediary service for some replacement functions that need to be called via multiple services.
/// This is made to prevent circular dependencies.
/// </summary>
public interface IReplacementsMediator
{
    /// <summary>
    /// Performs all replacements on a string using all data from a <see cref="DataSet"/>.
    /// This will return an IEnumerable of IEnumerable of strings. So you will have a string for each row in each table where the replacements have been done.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case-sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>An IEnumerable of IEnumerable of strings. So you will have a string for each row in each table, where the replacements have been performed.</returns>
    IEnumerable<IEnumerable<string>> DoReplacements(string input, DataSet replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Performs all replacements on a string using all data from a <see cref="DataTable"/>.
    /// This will return an IEnumerable of strings. So you will have a string for each row in the table, where the replacements have been performed.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case-sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>An IEnumerable of strings. So you will have a string for each row in the table, where the replacements have been performed.</returns>
    IEnumerable<string> DoReplacements(string input, DataTable replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Performs all replacements on a string using the data from a DataRow.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case-sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, DataRow replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Performs all replacements on a string using data that implements the <see cref="NameValueCollection"/> class (which comes from Request.QueryString and HttpUtility.ParseQueryString(), for example).
    /// This function will typically be used to replace Query and Form replacements from the http context.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case-sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, NameValueCollection replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Performs all replacements on a string using data that implements the IEnumerable&lt;KeyValuePair&lt;string, string&gt;&gt; interface.
    /// This function will typically be used to replace Query and Form replacements from the http context.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case-sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, IEnumerable<KeyValuePair<string, string>> replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Performs all replacements on a string using data that implements the IEnumerable&lt;KeyValuePair&lt;string, StringValues&gt;&gt; interface.
    /// This function will typically be used to replace Query and Form replacements from the http context.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case-sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, IEnumerable<KeyValuePair<string, StringValues>> replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Performs all replacements on a string using data that implements the ISession interface.
    /// This function will typically be used to replace Session replacements from the http context.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case-sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, ISession replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Performs replacements on a string using a name/value collection. You need this after parsing a query string parameter.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="isFromUnsafeSource">Optional: The <see cref="replaceData"/> is from an untrusted source, e.g. user input.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, NameValueCollection replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", bool isFromUnsafeSource = false);

    /// <summary>
    /// Performs replacements on a string using a JToken. This function is the most generic function, and all other replacement functions also use this function.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case-sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, JToken replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Performs replacements on a string using a dictionary of some type. This function is the most generic function, and all other replacement functions also use this function.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <param name="unsafeSource">Optional: The source type if the <paramref name="replaceData"/> is from an untrusted source, e.g. user input. Set to null if the source is not unsafe/untrusted.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, IDictionary<string, object> replaceData, string prefix = "{", string suffix = "}", bool forQuery = false, string defaultFormatter = "HtmlEncode", UnsafeSources? unsafeSource = null);

    /// <summary>
    /// Evaluates logic snippets in a string. These are simple if/else statements that can be used to conditionally include or exclude parts of a template.
    /// The syntax looks like this: [if({variable}=x)]...[else]...[endif].
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <returns>The original string with all replacements done.</returns>
    string EvaluateTemplate(string input);

    /// <summary>
    /// Searches input string for variables with default values and replaces them with those default values.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <returns>The original string with all replacements done.</returns>
    string HandleVariablesDefaultValues(string input, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Removes any template variables that are present in the input string.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be removed. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be removed. Default value is "}".</param>
    /// <returns>The original string without variables.</returns>
    string RemoveTemplateVariables(string input, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Checks for any replacement variables in a string and returns an array of <see cref="StringReplacementVariable"/>.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <param name="prefix">The prefix of replacement variables. The default is '{'.</param>
    /// <param name="suffix">The suffix of replacement variables. The default is '}'.</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <returns>An array of <see cref="StringReplacementVariable"/>.</returns>
    StringReplacementVariable[] GetReplacementVariables(string input, string prefix = "{", string suffix = "}", string defaultFormatter = "HtmlEncode");

    /// <summary>
    /// Attempts to find a formatter method (a method in <see cref="StringReplacementsExtensions"/>) to be used in a string replacement snippet.
    /// </summary>
    /// <param name="formatterString">The name of the formatter.</param>
    /// <returns>A <see cref="StringReplacementMethod"/> object containing information about the method and variables.</returns>
    StringReplacementMethod GetFormatterMethod(string formatterString);

    /// <summary>
    /// Performs replacements based on data available in the HTTP request, such as query and form values.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <returns></returns>
    string DoHttpRequestReplacements(string input, bool forQuery = false, string defaultFormatter = "HtmlEncode");

    /// <summary>
    /// Performs replacements based on data available in the session.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
    /// <param name="defaultFormatter">Optional: The default formatter to use. This should be HtmlEncode for anything that gets output to the browser. Default value is "HtmlEncode".</param>
    /// <returns></returns>
    string DoSessionReplacements(string input, bool forQuery = false, string defaultFormatter = "HtmlEncode");
}