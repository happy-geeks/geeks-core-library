namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

public class AddressModel
{
    /// <summary>
    /// Gets or sets street and house number
    /// </summary>
    public string Address { get; set; }
    
    /// <summary>
    /// Gets or sets country
    /// This must be a 2-letter ISO 3166-1 alpha-2 code
    /// </summary>
    public string Country { get; set; }
    
    /// <summary>
    /// Email of the recipient. Optional
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// Gets or sets full name of the recipient
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets phone number
    /// </summary>
    public string Phone { get; set; }
    
    /// <summary>
    /// Gets or sets place name of the recipient 
    /// </summary>
    public string Place { get; set; }
    
    /// <summary>
    /// Gets or sets zipcode of the recipient
    /// </summary>
    public string Zipcode { get; set; }
}