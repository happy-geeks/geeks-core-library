using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="From")]
public class FromModel
{
    [XmlElement(ElementName="Credential")]
    public CredentialModel Credential { get; set; }
}