using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Charge")]
public class ChargeModel
{
    [XmlElement(ElementName="Money")]
    public MoneyModel Money { get; set; }
}