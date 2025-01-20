namespace GeeksCoreLibrary.Core.Models;

public class AddressInfoModel
{
    public bool Success { get; set; }

    public string StreetName { get; set; }

    public string PlaceName { get; set; }

    public string Province { get; set; }

    public string Municipality { get; set; }

    public decimal? Longitude { get; set; }

    public decimal? Latitude { get; set; }

    public string Error { get; set; }
}