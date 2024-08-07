using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Contact")]
public class ContactModel
{
    [XmlElement(ElementName = "Name")]
    public NameModel Name { get; set; }

    [XmlElement(ElementName = "Email")]
    public string Email { get; set; }

    [XmlElement(ElementName = "Phone")]
    public PhoneModel Phone { get; set; }

    [XmlAttribute(AttributeName = "role")]
    public string Role { get; set; }

    [XmlText]
    public string Text { get; set; }
}