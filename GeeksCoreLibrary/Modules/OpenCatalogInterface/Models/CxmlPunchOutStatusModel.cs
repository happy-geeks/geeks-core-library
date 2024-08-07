using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Status")]
public class CxmlPunchOutStatusModel
{
    [XmlAttribute(AttributeName = "code")]
    public string Code { get; set; }

    [XmlAttribute(AttributeName = "text")]
    public string Text { get; set; }

    [XmlText]
    public string InnerText { get; set; }
}