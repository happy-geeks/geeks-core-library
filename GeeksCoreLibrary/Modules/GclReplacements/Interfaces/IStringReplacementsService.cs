using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.GclReplacements.Interfaces
{
    public interface IStringReplacementsService
    {
        /// <summary>
        /// Performs all replacements based on several default functions, such as query, form and cookies.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="dataRow"></param>
        /// <param name="handleRequest"></param>
        /// <param name="evaluateLogicSnippets"></param>
        /// <param name="removeUnknownVariables"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <returns></returns>
        Task<string> DoAllReplacementsAsync(string input, DataRow dataRow = null, bool handleRequest = true, bool evaluateLogicSnippets = true, bool removeUnknownVariables = true, bool forQuery = false);

        /// <summary>
        /// Performs replacements based on data available in the HTTP request, such as query and form values.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <returns></returns>
        string DoHttpRequestReplacements(string input, bool forQuery = false);

        /// <summary>
        /// Performs replacements based on data available in the session.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <returns></returns>
        string DoSessionReplacements(string input, bool forQuery = false);

        /// <summary>
        /// Performs all replacements on a string using the data from the first DataTable of a DataSet using a specific row index.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="replaceData"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <param name="caseSensitive">Whether the variable names in the replacement data dictionary should be case sensitive.</param>
        /// <returns></returns>
        IEnumerable<IEnumerable<string>> DoReplacements(string input, DataSet replaceData, bool forQuery = false, bool caseSensitive = true);

        /// <summary>
        /// Performs all replacements on a string using the data from a DataTable using a specific row index.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="replaceData"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <param name="caseSensitive">Whether the variable names in the replacement data dictionary should be case sensitive.</param>
        /// <returns></returns>
        IEnumerable<string> DoReplacements(string input, DataTable replaceData, bool forQuery = false, bool caseSensitive = true);

        /// <summary>
        /// Performs all replacements on a string using the data from a DataRow.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="replaceData"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <param name="caseSensitive">Whether the variable names in the replacement data dictionary should be case sensitive.</param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        string DoReplacements(string input, DataRow replaceData, bool forQuery = false, bool caseSensitive = true, string prefix = "{", string suffix = "}");

        /// <summary>
        /// Performs all replacements on a string using data that implements the IEnumerable&lt;KeyValuePair&lt;string, string&gt;&gt; interface.
        /// This function will typically be used to replace Query and Form replacements from the http context.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="replaceData"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <param name="caseSensitive">Whether the variable names in the replacement data dictionary should be case sensitive.</param>
        /// <returns></returns>
        string DoReplacements(string input, IEnumerable<KeyValuePair<string, string>> replaceData, bool forQuery = false, bool caseSensitive = true);

        /// <summary>
        /// Performs all replacements on a string using data that implements the IEnumerable&lt;KeyValuePair&lt;string, StringValues&gt;&gt; interface.
        /// This function will typically be used to replace Query and Form replacements from the http context.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="replaceData"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <param name="caseSensitive">Whether the variable names in the replacement data dictionary should be case sensitive.</param>
        /// <returns></returns>
        string DoReplacements(string input, IEnumerable<KeyValuePair<string, StringValues>> replaceData, bool forQuery = false, bool caseSensitive = true);
        
        /// <summary>
        /// Performs all replacements on a string using data that implements the ISession interface.
        /// This function will typically be used to replace Session replacements from the http context.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="replaceData"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <param name="caseSensitive">Whether the variable names in the replacement data dictionary should be case sensitive.</param>
        /// <returns></returns>
        string DoReplacements(string input, ISession replaceData, bool forQuery = false, bool caseSensitive = true);

        /// <summary>
        /// Performs replacements on a string using a dictionary of some type. This function is the most generic function, and all other replacement functions also use this function.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="replaceData"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <returns></returns>
        string DoReplacements(string input, IDictionary<string, object> replaceData, string prefix = "{", string suffix = "}", bool forQuery = false);

        /// <summary>
        /// Evaluates logic snippets in a string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        string EvaluateTemplate(string input);

        /// <summary>
        /// Searches input string for variables with default values and replaces them with those default values.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="prefix">The prefix that variables start with.</param>
        /// <param name="suffix">The suffix that variables end with.</param>
        /// <returns></returns>
        string HandleVariablesDefaultValues(string input, string prefix = "{", string suffix = "}");

        /// <summary>
        /// Removes any template variables that are present in the input string.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        string RemoveTemplateVariables(string input, string prefix = "{", string suffix = "}");

        /// <summary>
        /// Replace variables in a string based on JSON data.
        /// Example: {customer.address.streetline1} or {orderid}.
        /// </summary>
        /// <param name="input">The <see cref="JToken"/> instance to be used for the data.</param>
        /// <param name="inputString">The template.</param>
        /// <param name="evaluateTemplate">Whether logic snippets should be evaluated.</param>
        /// <param name="repeatVariableName">The name of the basic repeater variable. Defaults to 'repeat'.</param>
        /// <returns>The template with all JSON data replaced.</returns>
        string FillStringByClassList(JToken input, string inputString, bool evaluateTemplate = false, string repeatVariableName = "repeat");

        /// <summary>
        /// Replace variables in a string based on JSON data.
        /// Example: {customer.address.streetline1} or {orderid}.
        /// </summary>
        /// <param name="input">The <see cref="JToken"/> instance to be used for the data.</param>
        /// <param name="inputString">The template.</param>
        /// <param name="evaluateTemplate">Whether logic snippets should be evaluated.</param>
        /// <returns>The template with all JSON data replaced.</returns>
        string FillStringByClass(JToken input, string inputString, bool evaluateTemplate = false);
    }
}
