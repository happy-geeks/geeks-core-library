using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Payment")]
public class PaymentModel
{
    [XmlElement(ElementName="PCard")]
    public PCardModel PCard { get; set; }
}