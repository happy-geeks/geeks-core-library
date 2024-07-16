using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Description")]
public class DescriptionModel
{
    [XmlAttribute(AttributeName="lang")]
    public string Lang { get; set; }

    [XmlText]
    public string Value { get; set; }
}