using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="ItemID")]
public class ItemIdModel
{
    [XmlElement(ElementName="SupplierPartID")]
    public int SupplierPartId { get; set; }

    [XmlElement(ElementName="SupplierPartAuxiliaryID")]
    public string SupplierPartAuxiliaryId { get; set; }
}