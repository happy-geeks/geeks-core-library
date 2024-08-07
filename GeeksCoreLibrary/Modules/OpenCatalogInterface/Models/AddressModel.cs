using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Address")]
public class AddressModel
{
    [XmlElement(ElementName = "Name")]
    public NameModel Name { get; set; }

    [XmlElement(ElementName = "PostalAddress")]
    public PostalAddressModel PostalAddress { get; set; }

    [XmlElement(ElementName = "Email")]
    public string Email { get; set; }

    [XmlElement(ElementName = "Phone")]
    public PhoneModel Phone { get; set; }

    [XmlAttribute(AttributeName = "isoCountryCode")]
    public string IsoCountryCode { get; set; }

    [XmlAttribute(AttributeName = "addressID")]
    public string AddressId { get; set; }
}