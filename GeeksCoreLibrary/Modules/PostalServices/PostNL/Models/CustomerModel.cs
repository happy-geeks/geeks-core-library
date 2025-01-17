namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;

public class CustomerModel
{
    public AddressModel Address { get; set; }
    public string CollectionLocation { get; set; }
    public string CustomerCode { get; set; }
    public string CustomerNumber { get; set; }
    public string Email { get; set; }
}