using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Phone")]
public class PhoneModel
{
    [XmlElement(ElementName="TelephoneNumber")]
    public TelephoneNumberModel TelephoneNumber { get; set; }
}