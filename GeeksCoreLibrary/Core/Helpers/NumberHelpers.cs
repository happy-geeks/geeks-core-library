using System;
using System.Collections.Generic;

namespace GeeksCoreLibrary.Core.Helpers;

public class NumberHelpers
{
    /// <summary>
    /// Format a byte size into a human-readable string.
    /// </summary>
    /// <param name="size">The size in bytes.</param>
    /// <returns>A human-readable string with a KB/MB/GB/etc suffix.</returns>
    public static string FormatByteSize(double size)
    {
        var fileSizeSuffixes = new List<string> {"B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        const string formatTemplate = "{0}{1:0.##} {2}";

        if (size == 0)
        {
            return String.Format(formatTemplate, null, 0, fileSizeSuffixes[0]);
        }

        var absoluteSize = Math.Abs(size);
        var logarithm = (int)Math.Log(absoluteSize, 1024);
        logarithm = logarithm >= fileSizeSuffixes.Count ? fileSizeSuffixes.Count - 1 : logarithm;
        var normalizedSize = absoluteSize / Math.Pow(1000, logarithm);

        return String.Format(formatTemplate, size < 0 ? "-" : null, normalizedSize, fileSizeSuffixes[logarithm]);
    }
}