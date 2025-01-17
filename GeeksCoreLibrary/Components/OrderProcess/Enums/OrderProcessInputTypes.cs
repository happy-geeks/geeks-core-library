using System.Runtime.Serialization;

namespace GeeksCoreLibrary.Components.OrderProcess.Enums;

public enum OrderProcessInputTypes
{
    Text,
    Number,
    Email,
    Tel,
    Password,
    Date,
    Time,

    [EnumMember(Value = "datetime-local")]
    DateTime,
    Month,
    Week,
    Url,
    Range,
    Color,
    Hidden
}