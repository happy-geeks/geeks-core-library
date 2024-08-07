using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="UnitPrice")]
public class UnitPriceModel
{
    [XmlElement(ElementName="Money")]
    public MoneyModel Money { get; set; }
}