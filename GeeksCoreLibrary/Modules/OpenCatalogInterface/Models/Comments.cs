using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Comments")]
public class Comments
{
    [XmlAttribute(AttributeName = "lang")]
    public string Lang { get; set; }
}