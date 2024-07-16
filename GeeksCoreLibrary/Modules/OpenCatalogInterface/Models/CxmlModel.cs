using System;
using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "cXML")]
public class CxmlModel
{
    [XmlElement(ElementName = "Header")]
    public HeaderModel Header { get; set; }

    [XmlElement(ElementName = "Request")]
    public RequestModel Request { get; set; }

    [XmlAttribute(AttributeName = "version")]
    public DateTime Version { get; set; }

    [XmlAttribute(AttributeName = "payloadID")]
    public string PayloadId { get; set; }

    [XmlAttribute(AttributeName = "timestamp")]
    public string Timestamp { get; set; }

    [XmlAttribute(AttributeName = "lang")]
    public string Lang { get; set; }

    [XmlText]
    public string Text { get; set; }
}