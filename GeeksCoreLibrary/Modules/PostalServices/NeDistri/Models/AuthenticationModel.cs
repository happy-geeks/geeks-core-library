namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

public class AuthenticationModel
{
    /// <summary>
    /// Login name
    /// </summary>
    public string Login { get; set; }
    
    /// <summary>
    /// Random Nonce to make each authentication request unique
    /// </summary>
    public string Nonce { get; set; }
    
    /// <summary>
    /// Lifetime of the requested JWT token
    /// </summary>
    public int Lifetime { get; set; } = 1800;
}