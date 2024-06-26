using System.Xml.Serialization;

namespace GeeksCoreLibrary.Components.Account.Models;

[XmlRoot(ElementName = "PunchOutSetupResponse")]
public class PunchOutSetupResponseModel
{
    [XmlElement(ElementName = "StartPage")]
    public CxmlPunchOutStartPageModel StartPage { get; set; } = new CxmlPunchOutStartPageModel();
}