using GeeksCoreLibrary.Modules.Templates.Enums;

namespace GeeksCoreLibrary.Modules.Templates.Models;

/// <summary>
/// Model for trigger templates.
/// </summary>
public class TriggerTemplate : Template
{
    /// <summary>
    /// Gets or sets the timing of the trigger ("after" or "before").
    /// </summary>
    public TriggerTimings TriggerTiming { get; set; }

    /// <summary>
    /// Gets or sets the event of the trigger ("insert", "update", or "delete").
    /// </summary>
    public TriggerEvents TriggerEvent { get; set; }

    /// <summary>
    /// Gets or sets the name of the table the trigger is meant for.
    /// </summary>
    public string TriggerTableName { get; set; }
}