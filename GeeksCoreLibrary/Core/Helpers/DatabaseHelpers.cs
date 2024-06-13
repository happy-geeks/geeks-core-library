using System;
using System.Text.RegularExpressions;

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

        var regex = new Regex(@"[^\w]", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
        return regex.Replace(input, "");
    }
}