using System.Xml.Serialization;

namespace GeeksCoreLibrary.Components.Account.Models;

[XmlRoot(ElementName = "Response")]
public class CxmlPunchOutResponseModel
{
    [XmlElement(ElementName = "Status")]
    public CxmlPunchOutStatusModel Status { get; set; } = new CxmlPunchOutStatusModel();
    [XmlElement(ElementName = "PunchOutSetupResponse")]
    public PunchOutSetupResponseModel PunchOutSetupResponse { get; set; } = new PunchOutSetupResponseModel();
}