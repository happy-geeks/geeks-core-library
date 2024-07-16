using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Name")]
public class NameModel
{
    [XmlAttribute(AttributeName="lang")]
    public string Lang { get; set; }

    [XmlText]
    public string Value { get; set; }
}