using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "ItemOut")]
public class ItemOutModel
{
    [XmlElement(ElementName = "ItemID")]
    public ItemIdModel ItemId { get; set; }

    [XmlElement(ElementName = "ItemDetail")]
    public ItemDetailModel ItemDetail { get; set; }

    [XmlElement(ElementName = "ShipTo")]
    public ShipToModel ShipTo { get; set; }

    [XmlElement(ElementName = "Shipping")]
    public ShippingModel Shipping { get; set; }

    [XmlElement(ElementName = "Tax")]
    public TaxModel Tax { get; set; }

    [XmlElement(ElementName = "Distribution")]
    public DistributionModel Distribution { get; set; }

    [XmlElement(ElementName = "Comment")]
    public string Comment { get; set; }

    [XmlAttribute(AttributeName = "lineNumber")]
    public int LineNumber { get; set; }

    [XmlAttribute(AttributeName = "requestedDeliveryDate")]
    public string RequestedDeliveryDate { get; set; }

    [XmlAttribute(AttributeName = "quantity")]
    public decimal Quantity { get; set; }

    [XmlAttribute(AttributeName = "requisitionID")]
    public string RequisitionId { get; set; }
}