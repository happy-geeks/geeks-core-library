using System;
using System.IO;
using System.Xml.Serialization;

namespace GeeksCoreLibrary.Core.Helpers;

public static class XmlHelpers
{
    /// <summary>
    /// Deserialize an XML string to an object.
    /// </summary>
    /// <param name="xml">The XML string to deserialize.</param>
    /// <typeparam name="T">The class to deserialize to.</typeparam>
    /// <returns>The deserialized value of <see cref="T"/>.</returns>
    public static T DeserializeXml<T>(string xml)
    {
        if (String.IsNullOrWhiteSpace(xml))
        {
            return default;
        }

        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xml);
        return (T) serializer.Deserialize(reader);
    }

    /// <summary>
    /// Serialize an object to an XML string.
    /// </summary>
    /// <param name="input">The object to serialize.</param>
    /// <typeparam name="T">The object's type.</typeparam>
    /// <returns>The serialized XML string.</returns>
    public static string SerializeXml<T>(T input)
    {
        if (input == null)
        {
            return null;
        }

        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter();
        serializer.Serialize(writer, input);
        return writer.ToString();
    }
}