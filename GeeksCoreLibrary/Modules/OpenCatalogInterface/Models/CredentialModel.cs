using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Credential")]
public class CredentialModel
{
	[XmlElement(ElementName = "Identity")]
	public string Identity { get; set; }

	[XmlAttribute(AttributeName = "domain")]
	public string Domain { get; set; }

	[XmlElement(ElementName = "SharedSecret")]
	public string SharedSecret { get; set; }
}