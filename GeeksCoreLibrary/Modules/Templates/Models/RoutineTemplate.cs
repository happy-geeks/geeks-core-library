using GeeksCoreLibrary.Modules.Templates.Enums;

namespace GeeksCoreLibrary.Modules.Templates.Models;

/// <summary>
/// Model for routine (database stored procedure or function) templates.
/// </summary>
public class RoutineTemplate : Template
{
    /// <summary>
    /// Gets or sets the type of routine (stored procedure or function).
    /// </summary>
    public RoutineTypes RoutineType { get; set; }

    /// <summary>
    /// Gets or sets the parameters of the routine.
    /// </summary>
    public string RoutineParameters { get; set; }

    /// <summary>
    /// Gets or sets the return type of the routine.
    /// </summary>
    public string RoutineReturnType { get; set; }
}