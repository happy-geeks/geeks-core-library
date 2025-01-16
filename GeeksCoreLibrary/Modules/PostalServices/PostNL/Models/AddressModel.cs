using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;

public class AddressModel
{
    public string CompanyName { get; set; }
    public string AddressType { get; set; }
    public string City { get; set; }
    public string Countrycode { get; set; }
    public string FirstName { get; set; }
    [JsonProperty("HouseNr")]
    public string HouseNumber { get; set; }
    [JsonProperty("HouseNrExt")]
    public string HouseNumberAddition { get; set; }
    public string Name { get; set; }
    public string Street { get; set; }
    public string Zipcode { get; set; }
}