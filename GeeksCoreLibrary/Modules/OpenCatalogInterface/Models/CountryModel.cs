using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Country")]
public class CountryModel
{
    [XmlAttribute(AttributeName = "isoCountryCode")]
    public string IsoCountryCode { get; set; }

    [XmlText]
    public string Value { get; set; }
}