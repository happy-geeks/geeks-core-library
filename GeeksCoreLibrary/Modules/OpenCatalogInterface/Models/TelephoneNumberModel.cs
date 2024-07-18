using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="TelephoneNumber")]
public class TelephoneNumberModel
{
    [XmlElement(ElementName="CountryCode")]
    public CountryCodeModel CountryCode { get; set; }

    [XmlElement(ElementName="AreaOrCityCode")]
    public string AreaOrCityCode { get; set; }

    [XmlElement(ElementName="Number")]
    public string Number { get; set; }
}