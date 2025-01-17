namespace GeeksCoreLibrary.Components.OrderProcess.Models;

/// <summary>
/// A model for deciding where and how to save a field from the order process.
/// </summary>
public class OrderProcessFieldSaveToSettingsModel
{
    /// <summary>
    /// Gets or sets the entity type to save this field to.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the property name to save the value of the field in.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the link type of the link of this entity to the account, basket or order.
    /// </summary>
    public int LinkType { get; set; }
}