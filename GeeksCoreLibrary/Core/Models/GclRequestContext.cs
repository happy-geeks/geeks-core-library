using System.Threading;

// ReSharper disable InconsistentNaming
namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// A static class that holds the current request context for encryption keys and salts used in the application.
/// This uses <see cref="AsyncLocal{T}"/> to ensure that the values are specific to the current asynchronous context,
/// which is particularly useful in web applications where requests are handled asynchronously
/// and when these values sometimes have to changed per user or tenant, without changing it globally in <see cref="GclSettings.Current"/>.
/// </summary>
public static class GclRequestContext
{
    /// <summary>
    /// The encryption key used for AES encryption.
    /// </summary>
    private static readonly AsyncLocal<string> currentDefaultEncryptionKey = new();

    /// <summary>
    /// Gets or sets the encryption key used for AES encryption.
    /// </summary>
    public static string CurrentDefaultEncryptionKey
    {
        get => currentDefaultEncryptionKey.Value;
        set => currentDefaultEncryptionKey.Value = value;
    }

    /// <summary>
    /// The salt string that will be used in the AES encryption. This value should represent at least 8 bytes when converted into bytes using UTF-8.
    /// Note that the functions EncryptWithAesWithSalt and DecryptWithAesWithSalt do NOT use this salt! Those functions use a random salt. Only the functions
    /// EncryptWithAes and DecryptWithAes use this salt.
    /// </summary>
    private static readonly AsyncLocal<string> currentDefaultEncryptionSalt = new();

    /// <summary>
    /// Gets or sets the salt string that will be used in the AES encryption.
    /// </summary>
    public static string CurrentDefaultEncryptionSalt
    {
        get => currentDefaultEncryptionSalt.Value;
        set => currentDefaultEncryptionSalt.Value = value;
    }

    /// <summary>
    /// The encryption key used for triple DES encryption.
    /// </summary>
    private static readonly AsyncLocal<string> currentDefaultEncryptionKeyTripleDes = new();

    /// <summary>
    /// Gets or sets the encryption key used for triple DES encryption.
    /// </summary>
    public static string CurrentDefaultEncryptionKeyTripleDes
    {
        get => currentDefaultEncryptionKeyTripleDes.Value;
        set => currentDefaultEncryptionKeyTripleDes.Value = value;
    }

    /// <summary>
    /// The encryption key the ShoppingBasketsService uses for AES encryption.
    /// </summary>
    private static readonly AsyncLocal<string> currentShoppingBasketEncryptionKey = new();

    /// <summary>
    /// Gets or sets the encryption key the ShoppingBasketsService uses for AES encryption.
    /// </summary>
    public static string CurrentShoppingBasketEncryptionKey
    {
        get => currentShoppingBasketEncryptionKey.Value;
        set => currentShoppingBasketEncryptionKey.Value = value;
    }

    /// <summary>
    /// The encryption key used to encrypt and decrypt the cookie value of the Account component.
    /// </summary>
    private static readonly AsyncLocal<string> currentAccountCookieValueEncryptionKey = new();

    /// <summary>
    /// Gets or sets the encryption key used to encrypt and decrypt the cookie value of the Account component.
    /// </summary>
    public static string CurrentAccountCookieValueEncryptionKey
    {
        get => currentAccountCookieValueEncryptionKey.Value;
        set => currentAccountCookieValueEncryptionKey.Value = value;
    }

    /// <summary>
    /// The encryption key used to encrypt and decrypt the user ID of the Account component.
    /// </summary>
    private static readonly AsyncLocal<string> currentAccountUserIdEncryptionKey = new();

    /// <summary>
    /// Gets or sets the encryption key used to encrypt and decrypt the user ID of the Account component.
    /// </summary>
    public static string CurrentAccountUserIdEncryptionKey
    {
        get => currentAccountUserIdEncryptionKey.Value;
        set => currentAccountUserIdEncryptionKey.Value = value;
    }

    /// <summary>
    /// The encryption key that will be used for encrypting values with an expiry date.
    /// </summary>
    private static readonly AsyncLocal<string> currentExpiringEncryptionKey = new();

    /// <summary>
    /// Gets or sets the encryption key that will be used for encrypting values with an expiry date.
    /// </summary>
    public static string CurrentExpiringEncryptionKey
    {
        get => currentExpiringEncryptionKey.Value;
        set => currentExpiringEncryptionKey.Value = value;
    }

    /// <summary>
    /// The amount of hours an encrypted value is valid when it was encrypted with a date and time.
    /// </summary>
    private static readonly AsyncLocal<int?> currentTemporaryEncryptionHoursValid = new();

    /// <summary>
    /// Gets or sets the amount of hours an encrypted value is valid when it was encrypted with a date and time.
    /// </summary>
    public static int? CurrentTemporaryEncryptionHoursValid
    {
        get => currentTemporaryEncryptionHoursValid.Value;
        set => currentTemporaryEncryptionHoursValid.Value = value;
    }
}