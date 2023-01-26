namespace GeeksCoreLibrary.Components.OrderProcess.Enums;

/// <summary>
/// Methods for how to create a concept order from a basket, when the user starts a payment.
/// </summary>
public enum OrderProcessBasketToConceptOrderMethods
{
    /// <summary>
    /// This will create a copy of the basket and make that the concept order.
    /// This is the preferred option in most cases, because it is more secure and user friendly.
    /// If the user starts a payment and then cancel it, they will still have their basket and can even add more products to it then.
    /// When they start a payment for the second time, another concept order will be created from the updated basket, to make sure that they will pay for the new price.
    /// </summary>
    CreateCopy,
    
    /// <summary>
    /// This will change the entity type of the basket to that of a concept order.
    /// This is meant for websites that can have huge baskets with hundreds or thousands of basket lines, because those would take a long time to make copies of.
    /// But this has the problem that the user will lose their basket when starting a payment and they will have to add all products again if they want to switch the payment method after having started one.
    /// </summary>
    Convert
}