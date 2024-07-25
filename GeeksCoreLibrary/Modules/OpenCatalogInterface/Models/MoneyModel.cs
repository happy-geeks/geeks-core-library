using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Money")]
public class MoneyModel
{
    [XmlAttribute(AttributeName = "currency")]
    public string Currency { get; set; }

    [XmlText]
    public decimal Amount { get; set; }
}