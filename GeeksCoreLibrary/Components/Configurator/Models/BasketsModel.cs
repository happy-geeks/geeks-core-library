using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class BasketModel
    {
        /// <summary>
        /// Gets or sets the list of products.
        /// </summary>
        public List<ProductsModel> Products { get; set; } = new List<ProductsModel>();

        /// <summary>
        /// Gets or sets whether the result was successful.
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// Gets or sets the result message.
        /// </summary>
        public string ResultMessage { get; set; }

        /// <summary>
        /// Gets or sets the verification signature.
        /// This is used to check if the data has been tampered with.
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Gets or sets the source. This is the name of the base product that was ordered.
        /// </summary>
        public string Source { get; set; }
    }
}
