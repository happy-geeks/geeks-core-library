using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Distribution")]
public class DistributionModel
{
    [XmlElement(ElementName="Accounting")]
    public AccountingModel Accounting { get; set; }

    [XmlElement(ElementName="Charge")]
    public ChargeModel Charge { get; set; }
}