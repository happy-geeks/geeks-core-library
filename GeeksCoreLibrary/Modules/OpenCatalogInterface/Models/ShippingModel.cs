using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Shipping")]
public class ShippingModel
{
    [XmlElement(ElementName="Money")]
    public MoneyModel Money { get; set; }

    [XmlElement(ElementName="Description")]
    public DescriptionModel Description { get; set; }
}