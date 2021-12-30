using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    public class AddToShoppingBasketModel
    {
        public string UniqueId { get; set; }

        public ulong ItemId { get; set; }

        public decimal Quantity { get; set; }

        public string Type { get; set; } = "product";

        public IDictionary<string, string> LineDetails { get; set; }
    }
}
