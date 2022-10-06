using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Core.Interfaces;

/// <summary>
/// A service for everything that has something to do with user roles and permissions.
/// </summary>
public interface IRolesService
{
    /// <summary>
    /// Gets all available roles for users.
    /// </summary>
    /// <param name="includePermissions">Optional: Whether to include all permissions that each role has. Default is <see langword="false"/>.</param>
    /// <returns>A list of <see cref="RoleModel"/> with all available roles that users can have.</returns>
    Task<List<RoleModel>> GetRolesAsync(bool includePermissions = false);
}