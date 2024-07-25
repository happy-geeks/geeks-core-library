using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "PCard")]
public class PCardModel
{
    [XmlAttribute(AttributeName = "expiration")]
    public string Expiration { get; set; }

    [XmlAttribute(AttributeName = "number")]
    public string Number { get; set; }
}