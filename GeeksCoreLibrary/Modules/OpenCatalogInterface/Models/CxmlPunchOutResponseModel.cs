using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Response")]
public class CxmlPunchOutResponseModel
{
    [XmlElement(ElementName = "Status")]
    public CxmlPunchOutStatusModel Status { get; set; } = new();

    [XmlElement(ElementName = "PunchOutSetupResponse")]
    public PunchOutSetupResponseModel PunchOutSetupResponse { get; set; } = new();
}