using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="PCard")]
public class PCardModel
{
    [XmlAttribute(AttributeName="expiration")]
    public object Expiration { get; set; }

    [XmlAttribute(AttributeName="number")]
    public object Number { get; set; }
}