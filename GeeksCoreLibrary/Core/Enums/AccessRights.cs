using System;

namespace GeeksCoreLibrary.Core.Enums;

/// <summary>
/// Enum for Wiser permission flags.
/// </summary>
[Flags]
public enum AccessRights
{
    /// <summary>
    /// The user is not allowed to do anything.
    /// </summary>
    Nothing = 0,

    /// <summary>
    /// The user is allowed to execute read actions.
    /// </summary>
    Read = 1,

    /// <summary>
    /// The user is allowed to execute create actions.
    /// </summary>
    Create = 2,

    /// <summary>
    /// The user is allowed to execute update actions.
    /// </summary>
    Update = 4,

    /// <summary>
    /// The user is allowed to execute delete actions.
    /// </summary>
    Delete = 8
}