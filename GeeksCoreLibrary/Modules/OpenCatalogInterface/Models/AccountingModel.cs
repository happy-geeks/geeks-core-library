using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "Accounting")]
public class AccountingModel
{
    [XmlElement(ElementName = "Segment")]
    public SegmentModel Segment { get; set; }

    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
}