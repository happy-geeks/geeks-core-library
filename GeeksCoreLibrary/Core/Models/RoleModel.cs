using System.Collections.Generic;

namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// A model for a user role. Users can have multiple roles. Each role has it's own permissions.
/// </summary>
public class RoleModel
{
    /// <summary>
    /// Gets or sets the ID of the role.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets all permissions for this role.
    /// </summary>
    public List<PermissionModel> Permissions { get; set; }
    
    /// <summary>
    /// Gets or sets the ip addresses of the role.
    /// </summary>
    public List<string> IpAddresses { get; set; }
}