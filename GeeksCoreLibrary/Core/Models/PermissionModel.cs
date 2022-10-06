using System;
using GeeksCoreLibrary.Core.Enums;

namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// A model for indicating permissions for a role on an object.
/// </summary>
public class PermissionModel
{
    /// <summary>
    /// Gets or sets the item ID, if these permissions are for a single item.
    /// </summary>
    public ulong ItemId { get; set; }

    /// <summary>
    /// Gets or sets the entity property ID, if these permissions are for a single field/property in Wiser.
    /// </summary>
    public int EntityPropertyId { get; set; }

    /// <summary>
    /// Gets or sets the module ID, if these permissions are for an entire module in Wiser.
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the actual permissions. This is an enum with the <see cref="FlagsAttribute"/>, which means you can set multiple permissions in one property.
    /// </summary>
    public AccessRights Permissions { get; set; }
}