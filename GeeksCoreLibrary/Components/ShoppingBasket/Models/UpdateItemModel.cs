using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    public class UpdateItemModel
    {
        public ulong LineId { get; set; }

        public string UniqueId { get; set; }

        public IDictionary<string, string> LineDetails { get; set; }
    }
}
