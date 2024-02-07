using System.Collections.Generic;
using OrderProcessConstants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    public class AddToShoppingBasketModel
    {
        public string UniqueId { get; set; }

        public ulong ItemId { get; set; }

        public int Quantity { get; set; } = 1;

        public string Type { get; set; } = OrderProcessConstants.OrderLineProductType;

        public IDictionary<string, string> LineDetails { get; set; }
    }
}