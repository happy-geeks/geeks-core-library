using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "cXML")]
public class CxmlPunchOutSetupResponseModel
{
    [XmlElement(ElementName = "Response")]
    public CxmlPunchOutResponseModel Response { get; set; } = new();

    [XmlAttribute(AttributeName = "version")]
    public string Version { get; set; }

    [XmlAttribute(AttributeName = "lang", Namespace = "http://www.w3.org/XML/1998/namespace")]
    public string Lang { get; set; }

    [XmlAttribute(AttributeName = "payloadID")]
    public string PayloadId { get; set; }

    [XmlAttribute(AttributeName = "timestamp")]
    public string Timestamp { get; set; }
}