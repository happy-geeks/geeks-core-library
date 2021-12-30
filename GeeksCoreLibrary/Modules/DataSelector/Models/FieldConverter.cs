#nullable enable
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.DataSelector.Models
{
    public class FieldConverter : JsonConverter
    {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return new Field
                {
                    FieldName = reader.ReadAsString()
                };
            }

            // The resulting field.
            var field = existingValue ?? new Field();

            // The object that was loaded.
            var jsonObject = JObject.Load(reader);

            // Populate the object's values into the Field object.
            using var subReader = jsonObject.CreateReader();
            serializer.Populate(subReader, field);

            return field;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return typeof(Field).IsAssignableFrom(objectType) || objectType == typeof(string);
        }
    }
}
