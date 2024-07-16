using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Request")]
public class RequestModel
{
    [XmlElement(ElementName = "OrderRequest")]
    public OrderRequestModel OrderRequest { get; set; }

    [XmlAttribute(AttributeName = "deploymentMode")]
    public string DeploymentMode { get; set; }
}