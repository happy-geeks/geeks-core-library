namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class AddressModel
    {
        public string Street { get; set; }
        public string StreetNumber { get; set; }
        public string StreetNumberAdditional { get; set; }
        public string PostalCode { get; set; }
        public string PostalPlace { get; set; }
        public string CountryCode { get; set; }
        public string CareOf { get; set; }
    }
}