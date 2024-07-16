using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="BillTo")]
public class BillToModel
{
    [XmlElement(ElementName="Address")]
    public AddressModel Address { get; set; }
}