using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GeeksCoreLibrary.Core.Helpers;

public static class CssHelpers
{
    /// <summary>
    /// Moved any @import() statements in a CSS string to the top of that string.
    /// </summary>
    /// <param name="input">A string that contains CSS.</param>
    /// <returns>The new CSS with all import statements moved to the top.</returns>
    public static string MoveImportStatementsToTop(string input)
    {
        if (String.IsNullOrWhiteSpace(input) || !input.Contains("@import", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        var regex = new Regex(@"\@import url\((.+?)\);", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(30));
        var importList = new List<string>();

        foreach (Match match in regex.Matches(input))
        {
            importList.Add(match.Value);
            input = input.Replace(match.Value, "");
        }

        return String.Join("", importList) + Environment.NewLine + input;
    }
}