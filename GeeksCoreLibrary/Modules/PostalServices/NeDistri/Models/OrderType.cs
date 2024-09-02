namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

/// <summary>
/// NE DistriService Order types as described in <a href="https://orders.ne.nl/apidoc/orders#types">the documentation</a>
/// </summary>
public enum OrderType
{
    Shipment = 5,
    ReturnShipment = 6
}