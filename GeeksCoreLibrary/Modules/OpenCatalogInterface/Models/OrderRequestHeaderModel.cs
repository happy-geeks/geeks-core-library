using System.Collections.Generic;
using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName = "OrderRequestHeader")]
public class OrderRequestHeaderModel
{
    [XmlElement(ElementName = "Total")]
    public TotalModel Total { get; set; }

    [XmlElement(ElementName = "ShipTo")]
    public ShipToModel ShipTo { get; set; }

    [XmlElement(ElementName = "BillTo")]
    public BillToModel BillTo { get; set; }

    [XmlElement(ElementName = "Shipping")]
    public ShippingModel Shipping { get; set; }

    [XmlElement(ElementName = "Tax")]
    public TaxModel Tax { get; set; }

    [XmlElement(ElementName = "Payment")]
    public PaymentModel Payment { get; set; }

    [XmlElement(ElementName = "Contact")]
    public List<ContactModel> Contact { get; set; }

    [XmlElement(ElementName = "Comments")]
    public Comments Comments { get; set; }

    [XmlElement(ElementName = "Extrinsic")]
    public ExtrinsicModel Extrinsic { get; set; }

    [XmlAttribute(AttributeName = "orderID")]
    public string OrderId { get; set; }

    [XmlAttribute(AttributeName = "orderVersion")]
    public int OrderVersion { get; set; }

    [XmlAttribute(AttributeName = "orderDate")]
    public string OrderDate { get; set; }

    [XmlAttribute(AttributeName = "type")]
    public string Type { get; set; }
}