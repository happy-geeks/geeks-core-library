using System.Collections.Generic;
using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "PostalAddress")]
public class PostalAddressModel
{
    [XmlElement(ElementName = "DeliverTo")]
    public List<string> DeliverTo { get; set; }

    [XmlElement(ElementName = "Street")]
    public List<string> Street { get; set; }

    [XmlElement(ElementName = "City")]
    public string City { get; set; }

    [XmlElement(ElementName = "State")]
    public string State { get; set; }

    [XmlElement(ElementName = "PostalCode")]
    public string PostalCode { get; set; }

    [XmlElement(ElementName = "Country")]
    public CountryModel Country { get; set; }

    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
}