using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="To")]
public class ToModel
{
    [XmlElement(ElementName="Credential")]
    public CredentialModel Credential { get; set; }
}