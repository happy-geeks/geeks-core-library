using System;
using System.Text;

namespace GeeksCoreLibrary.Core.Helpers;

public static class DatabaseHelpers
{
    /// <summary>
    /// Uses a regular expression to remove all non-word characters, resulting in a valid parameter name.
    /// </summary>
    /// <param name="input">The string that you want to use as a parameter name for a database command.</param>
    /// <returns>The value that you can actually use as a parameter name.</returns>
    public static string CreateValidParameterName(string input)
    {
        if (String.IsNullOrWhiteSpace(input))
        {
            return "";
        }
        
        // Remove all non-word characters (anything that is not a letter, digit, or underscore)
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (Char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}