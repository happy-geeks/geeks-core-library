using System.Xml.Serialization;

namespace GeeksCoreLibrary.Components.Account.Models;

[XmlRoot(ElementName = "StartPage")]
public class CxmlPunchOutStartPageModel
{
    [XmlElement(ElementName = "URL")]
    public string URL { get; set; }
}