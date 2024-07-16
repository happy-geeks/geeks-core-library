using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="Header")]
public class HeaderModel
{
    [XmlElement(ElementName="From")]
    public FromModel From { get; set; }

    [XmlElement(ElementName="To")]
    public ToModel To { get; set; }

    [XmlElement(ElementName="Sender")]
    public SenderModel Sender { get; set; }
}