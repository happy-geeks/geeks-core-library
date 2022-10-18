using System;
using GeeksCoreLibrary.Modules.Communication.Enums;

namespace GeeksCoreLibrary.Modules.Communication.Extensions;

/// <summary>
/// Extensions for the <see cref="TriggerWeekDays"/> enum.
/// </summary>
public static class TriggerWeekDaysExtensions
{
    /// <summary>
    /// Check if the current weekday is marked in the flag.
    /// </summary>
    /// <param name="triggerWeekDays">The variable to perform the check on.</param>
    /// <returns></returns>
    public static bool IsToday(this TriggerWeekDays triggerWeekDays)
    {
        return triggerWeekDays.IsWeekday(DateTime.Now.DayOfWeek);
    }

    /// <summary>
    /// Check if the given weekday is marked in the flag.
    /// </summary>
    /// <param name="triggerWeekDays">The variable to perform the check on.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to check.</param>
    /// <returns></returns>
    public static bool IsWeekday(this TriggerWeekDays triggerWeekDays, DayOfWeek dayOfWeek)
    {
        switch (dayOfWeek)
        {
            case DayOfWeek.Sunday:
                return (triggerWeekDays & TriggerWeekDays.Sunday) == TriggerWeekDays.Sunday;
            case DayOfWeek.Monday:
                return (triggerWeekDays & TriggerWeekDays.Monday) == TriggerWeekDays.Monday;
            case DayOfWeek.Tuesday:
                return (triggerWeekDays & TriggerWeekDays.Tuesday) == TriggerWeekDays.Tuesday;
            case DayOfWeek.Wednesday:
                return (triggerWeekDays & TriggerWeekDays.Wednesday) == TriggerWeekDays.Wednesday;
            case DayOfWeek.Thursday:
                return (triggerWeekDays & TriggerWeekDays.Thursday) == TriggerWeekDays.Thursday;
            case DayOfWeek.Friday:
                return (triggerWeekDays & TriggerWeekDays.Friday) == TriggerWeekDays.Friday;
            case DayOfWeek.Saturday:
                return (triggerWeekDays & TriggerWeekDays.Saturday) == TriggerWeekDays.Saturday;
            default:
                throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null);
        }
    }
}