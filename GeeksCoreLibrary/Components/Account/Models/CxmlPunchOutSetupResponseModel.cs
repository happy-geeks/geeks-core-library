using System.Xml.Serialization;

namespace GeeksCoreLibrary.Components.Account.Models;

[XmlRoot(ElementName = "cXML")]
public class CxmlPunchOutSetupResponseModel
{
    [XmlElement(ElementName = "Response")]
    public CxmlPunchOutResponseModel Response { get; set; } = new CxmlPunchOutResponseModel();
    [XmlAttribute(AttributeName = "version")]
    public string Version { get; set; }
    [XmlAttribute(AttributeName = "lang", Namespace = "http://www.w3.org/XML/1998/namespace")]
    public string Lang { get; set; }
    [XmlAttribute(AttributeName = "payloadID")]
    public string PayloadID { get; set; }
    [XmlAttribute(AttributeName = "timestamp")]
    public string Timestamp { get; set; }
}
