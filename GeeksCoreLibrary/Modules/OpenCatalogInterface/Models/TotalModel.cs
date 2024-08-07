using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Total")]
public class TotalModel
{
    [XmlElement(ElementName="Money")]
    public MoneyModel Money { get; set; }
}