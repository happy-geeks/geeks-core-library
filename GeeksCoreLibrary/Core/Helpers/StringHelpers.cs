using System;
using System.Linq;
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
        if (ean.Length != 12 && ean.Any(c => !Char.IsDigit(c)))
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
        var inputBytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = hashSettings.Algorithm switch
        {
            HashAlgorithms.MD5 => MD5.HashData(inputBytes),
            HashAlgorithms.SHA256 => SHA256.HashData(inputBytes),
            HashAlgorithms.SHA384 => SHA384.HashData(inputBytes),
            HashAlgorithms.SHA512 => SHA512.HashData(inputBytes),
            _ => throw new ArgumentOutOfRangeException(nameof(hashSettings.Algorithm), hashSettings.Algorithm, null)
        };

        return hashSettings.Representation switch
        {
            HashRepresentations.Base64 => Convert.ToBase64String(hashBytes),
            HashRepresentations.Hex => Convert.ToHexString(hashBytes),
            _ => throw new ArgumentOutOfRangeException(nameof(hashSettings.Representation), hashSettings.Representation, null)
        };
    }

    /// <summary>
    /// Remove all digits and hyphens from a string.
    /// </summary>
    /// <param name="input">The string to remove the characters from.</param>
    /// <returns>The same string with all digits and hyphens removed.</returns>
    public static string RemoveDigitsAndHyphens(string input)
    {
        var stringBuilder = new StringBuilder(input.Length);
        foreach (var c in input.Where(c => !Char.IsDigit(c) && c != '-'))
        {
            stringBuilder.Append(c);
        }
        return stringBuilder.ToString();
    }
}