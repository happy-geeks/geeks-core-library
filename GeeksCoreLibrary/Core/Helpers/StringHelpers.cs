using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Core.Helpers;

public class StringHelpers
{
    /// <summary>
    /// Try to decrypt a value with AES.
    /// </summary>
    /// <param name="input">The string to decrypt.</param>
    /// <param name="output">The decrypted string if it was successful or <see langword="null"/> if not.</param>
    /// <param name="key">Optional: The encryption key to use. Default value is the value of "QueryTemplatesDecryptionKey" from the app settings.</param>
    /// <param name="withDateTime">Optional: Set the <see langword="true"/> if the value contains a validation date and time. Default is <see langword="false"/>.</param>
    /// <returns>A <see langword="bool"/> indicating whether the decryption was successful or not.</returns>
    public static bool TryDecryptWithAesWithSalt(string input, out string output, string key = "", bool withDateTime = false)
    {
        output = null;
        try
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            output = input.DecryptWithAesWithSalt(key, withDateTime);
            return true;
        }
        catch
        {
            // Ignored, don't call this method if you need to see or log the exception.
            return false;
        }
    }

    /// <summary>
    /// Calculate the final digit of a GTIN-13 EAN number.
    /// </summary>
    /// <param name="ean">EAN code to calculate validation digit. EAN consist only of 12 numeric characters</param>
    /// <returns>The validation digit</returns>
    public static int CalculateGtn13ValidationDigit(string ean)
    {
        var regex = new Regex("^[0-9]{12}$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));

        if (!regex.IsMatch(ean))
        {
            throw new ArgumentException($"Value '{ean}' is an invalid EAN. The EAN must consist of 12 numerical values", ean);
        }

        var calculationSum = 0;

        for (var i = 0; i < ean.Length; i++)
        {
            var number = Int32.Parse(ean[i].ToString());
            if (i % 2 == 0)
            {
                calculationSum += number;
            }
            else
            {
                calculationSum += number * 3;
            }
        }

        return (10 - calculationSum % 10) % 10;
    }

    /// <summary>
    /// Hash a value given a specific algorithm and return it in the given representation.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <param name="hashSettings">The settings to use for hashing.</param>
    /// <returns>Returns the value hashed with the algorithm converted to the given representation.</returns>
    public static string HashValue(string value, HashSettingsModel hashSettings)
    {
        HashAlgorithm hashAlgorithm;

        switch (hashSettings.Algorithm)
        {
            case HashAlgorithms.MD5:
                hashAlgorithm = MD5.Create();
                break;
            case HashAlgorithms.SHA256:
                hashAlgorithm = SHA256.Create();
                break;
            case HashAlgorithms.SHA384:
                hashAlgorithm = SHA384.Create();
                break;
            case HashAlgorithms.SHA512:
                hashAlgorithm = SHA512.Create();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hashSettings.Algorithm), hashSettings.Algorithm, null);
        }

        var bytes = Encoding.ASCII.GetBytes(value);
        var hashBytes = hashAlgorithm.ComputeHash(bytes);

        hashAlgorithm.Dispose();

        switch (hashSettings.Representation)
        {
            case HashRepresentations.Base64:
                return Convert.ToBase64String(hashBytes);
            case HashRepresentations.Hex:
                return Convert.ToHexString(hashBytes);
            default:
                throw new ArgumentOutOfRangeException(nameof(hashSettings.Representation), hashSettings.Representation, null);
        }
    }
}