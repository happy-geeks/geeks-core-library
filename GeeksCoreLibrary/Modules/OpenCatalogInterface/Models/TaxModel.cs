using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Tax")]
public class TaxModel
{
    [XmlElement(ElementName="Money")]
    public MoneyModel Money { get; set; }

    [XmlElement(ElementName="Description")]
    public DescriptionModel Description { get; set; }
}