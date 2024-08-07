using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "PunchOutSetupResponse")]
public class PunchOutSetupResponseModel
{
    [XmlElement(ElementName = "StartPage")]
    public CxmlPunchOutStartPageModel StartPage { get; set; } = new();
}