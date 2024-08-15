using System;
using System.IO;
using System.Text;
using System.Xml;
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
        using var stringReader = new StringReader(xml);

        // Prevents the XmlSerializer from resolving external resources, to mitigate XML External Entity (XXE) vulnerabilities.
        using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings { XmlResolver = null });
        return (T) serializer.Deserialize(xmlReader);
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
        var xmlStringBuilder = new StringBuilder();
        using var xmlWriter = XmlWriter.Create(xmlStringBuilder);
        serializer.Serialize(xmlWriter, input);
        return xmlStringBuilder.ToString();
    }
}