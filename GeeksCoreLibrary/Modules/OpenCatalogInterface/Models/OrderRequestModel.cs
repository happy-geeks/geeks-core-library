using System.Collections.Generic;
using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.OpenCatalogInterface.Models;

[XmlRoot(ElementName="OrderRequest")]
public class OrderRequestModel
{
    [XmlElement(ElementName="OrderRequestHeader")]
    public OrderRequestHeaderModel OrderRequestHeader { get; set; }

    [XmlElement(ElementName="ItemOut")]
    public List<ItemOutModel> ItemOut { get; set; }
}