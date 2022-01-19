using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class ProductsModel
    {
        /// <summary>
        /// Gets or sets the EAN number of the product.
        /// Only for configured products.
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// Gets or sets the product code.
        /// Only for regular products.
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// Gets or sets the action of what to do with the product (such as "ADD" to add it to the basket).
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the materials / details of the product. This will be shown in the basket to the user. 
        /// The basket itself nor the API does anything with this information, besides showing it to the user.
        /// </summary>
        public List<MaterialsModel> Materials { get; set; } = new List<MaterialsModel>();

        /// <summary>
        /// Gets or sets the URL of the product image.
        /// </summary>
        [JsonProperty("imageURL")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the title. This will be shown as is in the shopping basket of Gamma.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the quantity of products.
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the delivery type.
        /// </summary>
        public string DeliveryType { get; set; }

        /// <summary>
        /// Gets or sets the delivery time (in days).
        /// </summary>
        public int? DeliveryTime { get; set; }

        /// <summary>
        /// Gets or sets the price for the customer.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the purchase price.
        /// </summary>
        public string PurchasePrice { get; set; }
    }
}
