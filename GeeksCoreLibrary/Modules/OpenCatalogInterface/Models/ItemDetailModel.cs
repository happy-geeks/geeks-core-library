using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="ItemDetail")]
public class ItemDetailModel
{
    [XmlElement(ElementName="UnitPrice")]
    public UnitPriceModel UnitPrice { get; set; }

    [XmlElement(ElementName="Description")]
    public DescriptionModel Description { get; set; }

    [XmlElement(ElementName="UnitOfMeasure")]
    public string UnitOfMeasure { get; set; }

    [XmlElement(ElementName="Classification")]
    public ClassificationModel Classification { get; set; }

    [XmlElement(ElementName="ManufacturerPartID")]
    public string ManufacturerPartId { get; set; }

    [XmlElement(ElementName="ManufacturerName")]
    public string ManufacturerName { get; set; }

    [XmlElement(ElementName="URL")]
    public string Url { get; set; }
}