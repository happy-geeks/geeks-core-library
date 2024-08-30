namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

public class AuthenticationResponse
{
    /// <summary>
    /// JWT token used to authenticate on NE Distri API endpoints
    /// </summary>
    public string Token { get; set; }
    
    /// <summary>
    /// Unix Timestamp indicating when the JWT token expires
    /// </summary>
    public ulong Expires { get; set; }
}