using System;
using System.Text.RegularExpressions;

namespace GeeksCoreLibrary.Core.Helpers;

/// <summary>
/// A helper class for regular expressions.
/// </summary>
public static class RegexHelpers
{
    /// <summary>
    /// Escapes a minimal set of metacharacters (\, *, +, ?, |, {, }, [, ], (, ), ^, $, ., #, and whitespace),
    /// by replacing them with their \ codes. This converts a string so that
    /// it can be used as a constant within a regular expression safely. (Note that the
    /// reason # and whitespace must be escaped is so the string can be used safely
    /// within an expression parsed with x mode. If future Regex features add
    /// additional metacharacters, developers should depend on Escape to escape those
    /// characters as well.)
    /// </summary>
    public static string Escape(string input)
    {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (String.IsNullOrEmpty(input))
        {
            return input;
        }

        // For some reason, Regex.Escape does not escape "}" and "]" characters, so we need to do that manually.
        return Regex.Escape(input).Replace("}", "\\}").Replace("]", "\\]");
    }
}