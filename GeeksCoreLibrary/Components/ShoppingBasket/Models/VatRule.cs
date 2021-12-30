namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    public class VatRule
    {
        public string Country { get; set; } = "";

        /// <summary>
        /// Gets or sets whether the VAT rule is for B2B clients. -1 means not set, 0 is not B2B, 1 is B2B.
        /// </summary>
        public int B2B { get; set; } = -1;

        public int VatRate { get; set; } = 1;

        public decimal Percentage { get; set; } = 21M;
    }
}
