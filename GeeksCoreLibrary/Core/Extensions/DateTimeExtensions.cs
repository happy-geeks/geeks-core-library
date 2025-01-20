using System;

namespace GeeksCoreLibrary.Core.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Convert Unix time to DateTime
    /// </summary>
    public static DateTime UnixTimeToDateTime(this int input)
    {
        var functionReturnValue = new DateTime(1970, 1, 1);
        functionReturnValue = functionReturnValue.AddSeconds(input);

        if (functionReturnValue.IsDaylightSavingTime())
        {
            functionReturnValue = functionReturnValue.AddHours(1);
        }

        return functionReturnValue;
    }
}