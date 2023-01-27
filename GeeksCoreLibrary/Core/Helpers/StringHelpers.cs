using System;
using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Extensions;

namespace GeeksCoreLibrary.Core.Helpers
{
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
            var regex = new Regex("^[0-9]{12}$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));

            if (!regex.IsMatch(ean))
            {
                throw new ArgumentException($"Value '{ean}' is an invalid EAN. The EAN must consist of 12 numerical values",ean);
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
    }
}
