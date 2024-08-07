using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "CountryCode")]
public class CountryCodeModel
{
    [XmlAttribute(AttributeName = "isoCountryCode")]
    public string IsoCountryCode { get; set; }
}