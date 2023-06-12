using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.GclReplacements.Interfaces;

/// <summary>
/// This is an intermediary service for some replacement functions that need to be called via multiple services.
/// This is made to prevent circular dependencies.
/// </summary>
public interface IReplacementMediator
{
    /// <summary>
    /// Performs all replacements on a string using all data from a <see cref="DataSet"/>.
    /// This will return an IEnumerable of IEnumerable of strings. So you will have a string for each row in each table, where the replacements have been done.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <returns>An IEnumerable of IEnumerable of strings. So you will have a string for each row in each table, where the replacements have been performed.</returns>
    IEnumerable<IEnumerable<string>> DoReplacements(string input, DataSet replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Performs all replacements on a string using all data from a <see cref="DataTable"/>.
    /// This will return an IEnumerable of strings. So you will have a string for each row in the table, where the replacements have been performed.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <returns>An IEnumerable of strings. So you will have a string for each row in the table, where the replacements have been performed.</returns>
    IEnumerable<string> DoReplacements(string input, DataTable replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Performs all replacements on a string using the data from a DataRow.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, DataRow replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Performs all replacements on a string using data that implements the IEnumerable&lt;KeyValuePair&lt;string, string&gt;&gt; interface.
    /// This function will typically be used to replace Query and Form replacements from the http context.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, IEnumerable<KeyValuePair<string, string>> replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Performs all replacements on a string using data that implements the IEnumerable&lt;KeyValuePair&lt;string, StringValues&gt;&gt; interface.
    /// This function will typically be used to replace Query and Form replacements from the http context.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, IEnumerable<KeyValuePair<string, StringValues>> replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Performs all replacements on a string using data that implements the ISession interface.
    /// This function will typically be used to replace Session replacements from the http context.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, ISession replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Performs replacements on a string using a JToken. This function is the most generic function, and all other replacement functions also use this function.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <param name="caseSensitive">Optional: Whether the variable names in the replacement data dictionary should be case sensitive. Default is true.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, JToken replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}");

    /// <summary>
    /// Performs replacements on a string using a dictionary of some type. This function is the most generic function, and all other replacement functions also use this function.
    /// </summary>
    /// <param name="input">The string to do replacements on.</param>
    /// <param name="replaceData">The data that needs to be used for the replacements.</param>
    /// <param name="prefix">Optional: The string that is used as the prefix for every variable that needs to be replaced. Default value is "{".</param>
    /// <param name="suffix">Optional: The string that is used as the suffix for every variable that needs to be replaced. Default value is "}".</param>
    /// <param name="forQuery">Optional: Set to true to make all replaced values safe against SQL injection. You should only set this to true for SQL queries. Default is false.</param>
    /// <returns>The original string with all replacements done.</returns>
    string DoReplacements(string input, IDictionary<string, object> replaceData, string prefix = "{", string suffix = "}", bool forQuery = false);

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
}