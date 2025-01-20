using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Core.Models.Pro6PP;

public class Pro6PPAddressModel
{
    [JsonProperty("street")]
    public string StreetName { get; set; }

    [JsonProperty("streetnumber")]
    public string HouseNumber { get; set; }

    [JsonProperty("nl_sixpp")]
    public string PostalCode { get; set; }

    public string City { get; set; }

    public string Municipality { get; set; }

    public string Province { get; set; }

    [JsonProperty("lng")]
    public decimal Longitude { get; set; }

    [JsonProperty("lat")]
    public decimal Latitude { get; set; }

    public IEnumerable<string> Functions { get; set; }

    [JsonProperty("areacode")]
    public string AreaCode { get; set; }

    [JsonProperty("construction_year")]
    public int ConstructionYear { get; set; }
}