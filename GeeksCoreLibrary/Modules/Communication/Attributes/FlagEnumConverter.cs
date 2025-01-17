using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeeksCoreLibrary.Modules.Communication.Attributes;

public class FlagEnumConverter : StringEnumConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var enumValue = (Enum) value;

        writer.WriteValue(Convert.ToInt32(enumValue));
    }
}