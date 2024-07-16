using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "StartPage")]
public class CxmlPunchOutStartPageModel
{
    [XmlElement(ElementName = "URL")]
    public string Url { get; set; }
}