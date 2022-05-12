using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models
{
    public class ItemModel
    {
        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        [JsonProperty("item_name")]
        public string ItemName { get; set; }
        
        public string Affiliation { get; set; }
        
        public string Coupon { get; set; }
        
        public string Currency { get; set; }
        
        public double Discount { get; set; }
        
        public long Index { get; set; }

        [JsonProperty("item_brand")]
        public string ItemBrand { get; set; }

        [JsonProperty("item_category")]
        public string ItemCategory { get; set; }

        [JsonProperty("item_category2")]
        public string ItemCategoryTwo { get; set; }

        [JsonProperty("item_category3")]
        public string ItemCategoryThree { get; set; }

        [JsonProperty("item_category4")]
        public string ItemCategoryFour { get; set; }

        [JsonProperty("item_category5")]
        public string ItemCategoryFive { get; set; }

        [JsonProperty("item_list_id")]
        public string ItemListId { get; set; }

        [JsonProperty("item_list_name")]
        public string ItemListName { get; set; }

        [JsonProperty("item_variant")]
        public string ItemVariant { get; set; }

        [JsonProperty("location_id")]
        public string LocationId { get; set; }
        
        public double Price { get; set; }
        
        public long Quantity { get; set; }
    }
}
