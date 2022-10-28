using System;
using GeeksCoreLibrary.Modules.Communication.Attributes;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.Communication.Enums;

[Flags]
[JsonConverter(typeof(FlagEnumConverter))]
public enum TriggerWeekDays
{
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64
}