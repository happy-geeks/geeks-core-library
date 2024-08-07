using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Sender")]
public class SenderModel
{
    [XmlElement(ElementName="Credential")]
    public CredentialModel Credential { get; set; }

    [XmlElement(ElementName="UserAgent")]
    public string UserAgent { get; set; }
}