namespace GeeksCoreLibrary.Modules.DataSelector.Models;

/// <summary>
/// Represents an iteration of connections.
/// </summary>
public class ConnectionIterationModel
{
    /// <summary>
    /// Gets or sets the iteration count of this current level.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the name of the previous entity (in other words, the name of the entity of one level back).
    /// </summary>
    public string PreviousEntityName { get; set; }
}