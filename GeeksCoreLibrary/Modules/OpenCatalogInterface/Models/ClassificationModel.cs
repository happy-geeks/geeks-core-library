using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Classification")]
public class ClassificationModel
{
    [XmlAttribute(AttributeName = "domain")]
    public string Domain { get; set; }
}