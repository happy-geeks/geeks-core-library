using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

public class SegmentModel
{
    [XmlAttribute(AttributeName = "description")]
    public string Description { get; set; }

    [XmlAttribute(AttributeName = "id")]
    public string Id { get; set; }

    [XmlAttribute(AttributeName = "type")]
    public string Type { get; set; }
}