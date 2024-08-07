using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="ShipTo")]
public class ShipToModel
{
    [XmlElement(ElementName="Address")]
    public AddressModel Address { get; set; }
}