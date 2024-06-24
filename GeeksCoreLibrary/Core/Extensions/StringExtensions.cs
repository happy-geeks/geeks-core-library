using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// URL encode a string to make it safe to use in an URL.
        /// </summary>
        /// <param name="input">The string to URL encode.</param>
        /// <returns>The URL encoded string.</returns>
        public static string UrlEncode(this string input)
        {
            return System.Net.WebUtility.UrlEncode(input);
        }

        /// <summary>
        /// URL decode a string.
        /// </summary>
        /// <param name="input">The string to URL decode.</param>
        /// <returns>The URL decoded string.</returns>
        public static string UrlDecode(this string input)
        {
            return System.Net.WebUtility.UrlDecode(input);
        }

        /// <summary>
        /// HTML decode a string.
        /// </summary>
        /// <param name="input">The string to HTML decode.</param>
        /// <returns>The HTML decoded string.</returns>
        public static string HtmlDecode(this string input)
        {
            return System.Net.WebUtility.HtmlDecode(input);
        }

        /// <summary>
        /// HTML encode a string to make it safe to put on a page.
        /// </summary>
        /// <param name="input">The string to HTML encode.</param>
        /// <returns>The HTML encoded string.</returns>
        public static string HtmlEncode(this string input)
        {
            return System.Net.WebUtility.HtmlEncode(input);
        }

        /// <summary>
        /// Replace values in a string with other values while ignoring casing differences.
        /// </summary>
        /// <param name="input">The string to replaces the values in.</param>
        /// <param name="oldValue">The value to replace.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>The string with all requested values replaced.</returns>
        public static string ReplaceCaseInsensitive(this string input, string oldValue, string newValue)
        {
            return input.Replace(oldValue, newValue, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Converts a string to a SEO-friendly variant, for using in URLs.
        /// This will make the string lower case, replace spaces with dashes (-) and remove all kinds of special characters.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The string that can be used in an URL.</returns>
        public static string ConvertToSeo(this string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            var normalizedString = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);

            var prevDash = false;
            var stringBuilder = new StringBuilder();

            // Some characters need remapping because they don't have a "normal" equivalent.
            var specialCharacterMappings = new SortedList<char, string>
            {
                { 'ß', "ss" },
                { 'æ', "ae" },
                { 'ð', "d" },
                { 'ø', "o" },
                { 'þ', "th" },
                { 'đ', "d" },
                { 'ł', "l" },
                { 'œ', "oe" }
            };

            // Array of characters that should be left intact.
            var preserveCharacters = new[] {'_'};

            foreach (var c in normalizedString)
            {
                if (Regex.IsMatch(c.ToString(), "\\p{IsCyrillic}"))
                {
                    continue;
                }

                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                // Some characters should be left intact.
                if (preserveCharacters.Contains(c))
                {
                    stringBuilder.Append(c);
                    continue;
                }

                switch (unicodeCategory)
                {
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                        if (specialCharacterMappings.ContainsKey(c))
                        {
                            stringBuilder.Append(specialCharacterMappings[c]);
                        }
                        else
                        {
                            stringBuilder.Append(c);
                        }

                        prevDash = false;
                        break;
                    case UnicodeCategory.SpaceSeparator:
                    case UnicodeCategory.ConnectorPunctuation:
                    case UnicodeCategory.DashPunctuation:
                    case UnicodeCategory.OtherPunctuation:
                    case UnicodeCategory.MathSymbol:
                        if (!prevDash)
                        {
                            stringBuilder.Append('-');
                            prevDash = true;
                        }
                        break;
                }
            }

            var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC).Trim('-');

            return result;
        }

        /// <summary>
        /// Converts a string to a string that can be safely used in a MySQL query to avoid SQL injections.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="encloseInQuotes">Whether the value should be enclosed in quotes. You should never set this to <see langword="false"/>, unless you add quotes manually in your query! Otherwise SQL injection will still be possible!</param>
        /// <returns>The converted string that can be safely used in a query, as long as quotes are added around it.</returns>
        public static string ToMySqlSafeValue(this string input, bool encloseInQuotes)
        {
            if (input == null)
            {
                return null;
            }

            var result = MySqlConnector.MySqlHelper.EscapeString(input);
            return encloseInQuotes ? $"'{result}'" : result;
        }

        /// <summary>
        /// Encrypts a value with AES and returns the encrypted value as a Base64 string.
        /// </summary>
        /// <param name="input">The string to encrypt.</param>
        /// <param name="key">Optional: The encryption key to use. Default value is the value of "ExpiringEncryptionKey" (if withDateTime = true) or "DefaultEncryptionKey" (if withDateTime = false) from the app settings.</param>
        /// <param name="withDateTime">Optional: Whether to add a timestamp to the encrypted value, so that the the value can have an expire date. The decrypt method decides how long the value is valid.</param>
        /// <param name="useSlowerButMoreSecureMethod">Optional: Whether to use a more secure encryption, but that method is a lot slower. This method will use the code from this article: https://docs.microsoft.com/en-us/dotnet/standard/security/vulnerabilities-cbc-mode</param>
        /// <returns>The encrypted string.</returns>
        public static string EncryptWithAes(this string input, string key = "", bool withDateTime = false, bool useSlowerButMoreSecureMethod = false)
        {
            string encryptionKey;
            var stringToEncrypt = new StringBuilder(input);

            if (!String.IsNullOrWhiteSpace(key))
            {
                // Custom secret key passed in the parameters.
                encryptionKey = key;
            }
            else if (withDateTime && !String.IsNullOrWhiteSpace(GclSettings.Current.ExpiringEncryptionKey))
            {
                // Secret key for values that expire after a set time.
                encryptionKey = GclSettings.Current.ExpiringEncryptionKey;
            }
            else if (!String.IsNullOrWhiteSpace(GclSettings.Current.DefaultEncryptionKey))
            {
                // Default secret key from app settings.
                encryptionKey = GclSettings.Current.DefaultEncryptionKey;
            }
            else
            {
                throw new Exception("EncryptWithAes: No AES secret key set.");
            }

            if (withDateTime)
            {
                stringToEncrypt.Append('~').Append(DateTime.Now.ToString("yyyyMMddHHmmss"));
            }

            if (useSlowerButMoreSecureMethod)
            {
                // Salt of at least 8 bytes is required to derive key.
                // If no salt is set in the appsettings, a basic 0-salt will be used.
                var saltString = GclSettings.Current.DefaultEncryptionSalt;
                var salt = !String.IsNullOrWhiteSpace(saltString) ? Encoding.UTF8.GetBytes(saltString) : Array.Empty<byte>();

                var keyBytes = KeyDerivation.Pbkdf2(encryptionKey, salt, KeyDerivationPrf.HMACSHA512, 100000, 256 / 8);
                var inputBytes = Encoding.UTF8.GetBytes(stringToEncrypt.ToString());
                var encryptedBytes = CryptographyHelpers.Encrypt(keyBytes, inputBytes);

                return Convert.ToBase64String(encryptedBytes);
            }
            else
            {
                // Salt of at least 8 bytes is required to derive key.
                // If no salt is set in the appsettings, a basic 0-salt will be used.
                var saltString = GclSettings.Current.DefaultEncryptionSalt;
                var salt = !String.IsNullOrWhiteSpace(saltString) ? Encoding.UTF8.GetBytes(saltString) : new byte[8];

                // Salt must be at least 8 bytes.
                if (salt.Length < 8)
                {
                    var tempSalt = new byte[8];
                    Buffer.BlockCopy(salt, 0, tempSalt, 0, salt.Length);
                    salt = tempSalt;
                }

                using var deriveBytes = new Rfc2898DeriveBytes(encryptionKey, salt, 2);
                const int KeySize = 256;
                const int BlockSize = 128;
                using var aesManaged = Aes.Create();
                aesManaged.KeySize = KeySize;
                aesManaged.BlockSize = BlockSize;
                aesManaged.Key = deriveBytes.GetBytes(Convert.ToInt32(KeySize / 8));
                aesManaged.IV = deriveBytes.GetBytes(Convert.ToInt32(BlockSize / 8));
                using var encryptor = aesManaged.CreateEncryptor(aesManaged.Key, aesManaged.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using var sw = new StreamWriter(cs);
                    sw.Write(stringToEncrypt.ToString());
                }

                var encryptedBytes = ms.ToArray();

                return Convert.ToBase64String(encryptedBytes);
            }
        }

        /// <summary>
        /// Decrypts a value with AES.
        /// </summary>
        /// <param name="input">The string to decrypt.</param>
        /// <param name="key">Optional: The encryption key to use. Default value is the value of "ExpiringEncryptionKey" (if withDateTime = true) or "DefaultEncryptionKey" (if withDateTime = false) from the app settings.</param>
        /// <param name="withDateTime">Optional: Set the <see langword="true"/> if the value contains a validation date and time. Default is <see langword="false"/>.</param>
        /// <param name="minutesValidOverride">Optional: If you want the encryption to be valid for a different amount of time than what it set in the appsettings, you can change that here.</param>
        /// <param name="useSlowerButMoreSecureMethod">Optional: Whether to use a more secure encryption, but that method is a lot slower. This method will use the code from this article: https://docs.microsoft.com/en-us/dotnet/standard/security/vulnerabilities-cbc-mode</param>
        /// <returns>The decrypted string.</returns>
        public static string DecryptWithAes(this string input, string key = "", bool withDateTime = false, int minutesValidOverride = 0, bool useSlowerButMoreSecureMethod = false)
        {
            string encryptionKey;
            if (!String.IsNullOrWhiteSpace(key))
            {
                // Custom secret key passed in the parameters.
                encryptionKey = key;
            }
            else if (withDateTime && !String.IsNullOrWhiteSpace(GclSettings.Current.ExpiringEncryptionKey))
            {
                // Secret key for values that expire after a set time.
                encryptionKey = GclSettings.Current.ExpiringEncryptionKey;
            }
            else if (!String.IsNullOrWhiteSpace(GclSettings.Current.DefaultEncryptionKey))
            {
                // Default secret key from app settings.
                encryptionKey = GclSettings.Current.DefaultEncryptionKey;
            }
            else
            {
                throw new Exception("DecryptWithAes: No AES secret key set.");
            }

            string output;

            if (useSlowerButMoreSecureMethod)
            {
                // Salt of at least 8 bytes is required to derive key.
                // If no salt is set in the appsettings, a basic 0-salt will be used.
                var saltString = GclSettings.Current.DefaultEncryptionSalt;
                var salt = !String.IsNullOrWhiteSpace(saltString) ? Encoding.UTF8.GetBytes(saltString) : Array.Empty<byte>();
                var inputBytes = Convert.FromBase64String(input);
                var keyBytes = KeyDerivation.Pbkdf2(encryptionKey, salt, KeyDerivationPrf.HMACSHA512, 100000, 256 / 8);
                var decryptedBytes = CryptographyHelpers.Decrypt(keyBytes, inputBytes);
                output = Encoding.UTF8.GetString(decryptedBytes);
            }
            else
            {
                // Salt of at least 8 bytes is required to derive key.
                // If no salt is set in the appsettings, a basic 0-salt will be used.
                var saltString = GclSettings.Current.DefaultEncryptionSalt;
                var salt = !String.IsNullOrWhiteSpace(saltString) ? Encoding.UTF8.GetBytes(saltString) : new byte[8];

                // Salt must be at least 8 bytes.
                if (salt.Length < 8)
                {
                    var tempSalt = new byte[8];
                    Buffer.BlockCopy(salt, 0, tempSalt, 0, salt.Length);
                    salt = tempSalt;
                }

                var inputBytes = Convert.FromBase64String(input);

                using var deriveBytes = new Rfc2898DeriveBytes(encryptionKey, salt, 2);
                const int KeySize = 256;
                const int BlockSize = 128;

                using var aesManaged = Aes.Create();
                aesManaged.KeySize = KeySize;
                aesManaged.BlockSize = BlockSize;
                aesManaged.Key = deriveBytes.GetBytes(Convert.ToInt32(KeySize / 8));
                aesManaged.IV = deriveBytes.GetBytes(Convert.ToInt32(BlockSize / 8));
                using var decryptor = aesManaged.CreateDecryptor(aesManaged.Key, aesManaged.IV);
                using var ms = new MemoryStream(inputBytes);
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using var sr = new StreamReader(cs);
                    output = sr.ReadToEnd();
                }
            }

            if (!withDateTime)
            {
                return output;
            }

            // Contains a date time restriction.
            if (!output.Contains("~"))
            {
                throw new Exception("GCL DecryptWithAes: Expected encrypted value with datetime.");
            }

            var separatorIndex = output.LastIndexOf("~", StringComparison.Ordinal);
            var date = DateTime.ParseExact(output[(separatorIndex + 1)..], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var hoursValid = GclSettings.Current.TemporaryEncryptionHoursValid;
            var minutesValid = minutesValidOverride;
            if (minutesValid <= 0 && hoursValid > 0)
            {
                minutesValid = hoursValid * 60;
            }

            if (minutesValid <= 0)
            {
                minutesValid = 24 * 60;
            }

            if (date.AddMinutes(minutesValid) < DateTime.Now)
            {
                throw new Exception("GCL DecryptWithAes: The encrypted value has expired.");
            }

            return output.Remove(output.Length - 15);
        }

        /// <summary>
        /// Encrypts a value with AES. This method uses a salt, so it's random every time.
        /// </summary>
        /// <param name="input">The value to encrypt.</param>
        /// <param name="key">Optional: The encryption key to use. Default value is the value of "ExpiringEncryptionKey" (if withDateTime = true) or "DefaultEncryptionKey" (if withDateTime = false) from the app settings.</param>
        /// <param name="withDateTime">Optional: Whether to add a timestamp to the encrypted value, so that the the value can have an expire date. The decrypt method decides how long the value is valid.</param>
        /// <param name="useSlowerButMoreSecureMethod">Optional: Whether to use a more secure encryption, but that method is a lot slower. This method will use the code from this article: https://docs.microsoft.com/en-us/dotnet/standard/security/vulnerabilities-cbc-mode</param>
        /// <returns>The encrypted value with the salt prepended to it.</returns>
        public static string EncryptWithAesWithSalt(this string input, string key = "", bool withDateTime = false, bool useSlowerButMoreSecureMethod = false)
        {
            string encryptionKey;
            var stringToEncrypt = new StringBuilder(input);

            if (!String.IsNullOrWhiteSpace(key))
            {
                // Custom secret key passed in the parameters.
                encryptionKey = key;
            }
            else if (withDateTime && !String.IsNullOrWhiteSpace(GclSettings.Current.ExpiringEncryptionKey))
            {
                // Wiser 2.0 secret key for customer.
                encryptionKey = GclSettings.Current.ExpiringEncryptionKey;
            }
            else if (!String.IsNullOrWhiteSpace(GclSettings.Current.DefaultEncryptionKey))
            {
                // Custom secret key set in the app settings.
                encryptionKey = GclSettings.Current.DefaultEncryptionKey;
            }
            else
            {
                throw new Exception("EncryptWithAesWithSalt: No AES secret key set.");
            }

            if (withDateTime)
            {
                stringToEncrypt.Append('~').Append(DateTime.Now.ToString("yyyyMMddHHmmss"));
            }

            var inputBytes = Encoding.UTF8.GetBytes(stringToEncrypt.ToString());

            if (useSlowerButMoreSecureMethod)
            {
                // Generate a 128-bit salt using a cryptographically strong random sequence of nonzero values.
                var saltBytes = new byte[128 / 8];
                using (var rngCsp = RandomNumberGenerator.Create())
                {
                    rngCsp.GetNonZeroBytes(saltBytes);
                }

                var keyBytes = KeyDerivation.Pbkdf2(encryptionKey, saltBytes, KeyDerivationPrf.HMACSHA512, 100000, 256 / 8);
                var encryptedBytes = CryptographyHelpers.Encrypt(keyBytes, inputBytes);
                var outputBytes = new byte[encryptedBytes.Length + saltBytes.Length];
                Buffer.BlockCopy(encryptedBytes, 0, outputBytes, 0, encryptedBytes.Length);
                Buffer.BlockCopy(saltBytes, 0, outputBytes, encryptedBytes.Length, saltBytes.Length);
                return Convert.ToBase64String(outputBytes);
            }
            else
            {
                // Create salt.
                var random = new Random();
                var saltSize = random.Next(8, 12);
                var saltBytes = new byte[saltSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBytes);
                }

                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;

                // Derive the key and IV from the password and the salt.
                var rfc2898DeriveBytes = new Rfc2898DeriveBytes(encryptionKey, saltBytes, 2);
                var keyBytes = rfc2898DeriveBytes.GetBytes(aes.KeySize / 8);
                var ivBytes = rfc2898DeriveBytes.GetBytes(aes.BlockSize / 8);

                aes.Key = keyBytes;
                var resultBytes = aes.EncryptCbc(inputBytes, ivBytes);

                var outputBytes = new byte[resultBytes.Length + saltBytes.Length];
                Buffer.BlockCopy(resultBytes, 0, outputBytes, 0, resultBytes.Length);
                Buffer.BlockCopy(saltBytes, 0, outputBytes, resultBytes.Length, saltBytes.Length);

                var output = new StringBuilder(Convert.ToBase64String(outputBytes));
                output.Replace("/", "-");

                return output.ToString();
            }
        }

        /// <summary>
        /// Decrypts a value with AES. This method uses a salt, so it can decrypt values encrypted with <see cref="EncryptWithAesWithSalt"/>.
        /// </summary>
        /// <param name="input">The string to decrypt.</param>
        /// <param name="key">Optional: The encryption key to use. Default value is the value of "ExpiringEncryptionKey" (if withDateTime = true) or "DefaultEncryptionKey" (if withDateTime = false) from the app settings.</param>
        /// <param name="withDateTime">Optional: Set the <see langword="true"/> if the value contains a validation date and time. Default is <see langword="false"/>.</param>
        /// <param name="minutesValidOverride">Optional: If you want the encryption to be valid for a different amount of time than what it set in the appsettings, you can change that here.</param>
        /// <param name="useSlowerButMoreSecureMethod">Optional: Whether to use a more secure encryption, but that method is a lot slower. This method will use the code from this article: https://docs.microsoft.com/en-us/dotnet/standard/security/vulnerabilities-cbc-mode</param>
        /// <returns>The decrypted string.</returns>
        public static string DecryptWithAesWithSalt(this string input, string key = "", bool withDateTime = false, int minutesValidOverride = 0, bool useSlowerButMoreSecureMethod = false)
        {
            string encryptionKey;

            if (!String.IsNullOrWhiteSpace(key))
            {
                // Custom secret key passed in the parameters.
                encryptionKey = key;
            }
            else if (withDateTime && !String.IsNullOrWhiteSpace(GclSettings.Current.ExpiringEncryptionKey))
            {
                // Wiser 2.0 secret key for customer.
                encryptionKey = GclSettings.Current.ExpiringEncryptionKey;
            }
            else if (!String.IsNullOrWhiteSpace(GclSettings.Current.DefaultEncryptionKey))
            {
                // Custom secret key set in the app settings.
                encryptionKey = GclSettings.Current.DefaultEncryptionKey;
            }
            else
            {
                throw new Exception("DecryptWithAesWithSalt: No AES secret key set.");
            }

            string output;
            if (useSlowerButMoreSecureMethod)
            {
                var inputWithSaltBytes = Convert.FromBase64String(Uri.UnescapeDataString(input));

                var saltByteLength = 128 / 8;

                var inputBytes = new byte[inputWithSaltBytes.Length - saltByteLength];
                var saltBytes = new byte[saltByteLength];

                Buffer.BlockCopy(inputWithSaltBytes, 0, inputBytes, 0, inputBytes.Length);
                Buffer.BlockCopy(inputWithSaltBytes, inputBytes.Length, saltBytes, 0, saltBytes.Length);
                var keyBytes = KeyDerivation.Pbkdf2(encryptionKey, saltBytes, KeyDerivationPrf.HMACSHA512, 100000, 256 / 8);
                var outputBytes = CryptographyHelpers.Decrypt(keyBytes, inputBytes);

                // Turn the decrypted bytes into a string. It is assumed here that the string was encrypted with UTF-8.
                output = Encoding.UTF8.GetString(outputBytes);
            }
            else
            {
                var stringToDecrypt = new StringBuilder(input);
                stringToDecrypt.Replace("-", "/");

                var inputWithSaltBytes = Convert.FromBase64String(Uri.UnescapeDataString(stringToDecrypt.ToString()));

                var saltByteLength = inputWithSaltBytes.Length % 16;

                var inputBytes = new byte[16 * (inputWithSaltBytes.Length / 16)];
                var saltBytes = new byte[saltByteLength];

                Buffer.BlockCopy(inputWithSaltBytes, 0, inputBytes, 0, inputBytes.Length);
                Buffer.BlockCopy(inputWithSaltBytes, inputBytes.Length, saltBytes, 0, saltBytes.Length);

                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;

                // Derive the key and IV from the password and the salt.
                var rfc2898DeriveBytes = new Rfc2898DeriveBytes(encryptionKey, saltBytes, 2);
                var keyBytes = rfc2898DeriveBytes.GetBytes(aes.KeySize / 8);
                var ivBytes = rfc2898DeriveBytes.GetBytes(aes.BlockSize / 8);
                aes.Key = keyBytes;
                // Perform the decryption.
                var decryptedBytes = aes.DecryptCbc(inputBytes, ivBytes);

                // Turn the decrypted bytes into a string. It is assumed here that the string was encrypted with UTF-8.
                output = Encoding.UTF8.GetString(decryptedBytes);
            }

            if (!withDateTime)
            {
                return output;
            }

            // Contains a date time restriction.
            if (!output.Contains("~"))
            {
                throw new Exception("GCL DecryptWithAes: Expected encrypted value with datetime.");
            }

            var separatorIndex = output.LastIndexOf("~", StringComparison.Ordinal);
            var date = DateTime.ParseExact(output[(separatorIndex + 1)..], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var hoursValid = GclSettings.Current.TemporaryEncryptionHoursValid;
            var minutesValid = minutesValidOverride;
            if (minutesValid <= 0 && hoursValid > 0)
            {
                minutesValid = hoursValid * 60;
            }

            if (minutesValid <= 0)
            {
                minutesValid = 24 * 60;
            }

            if (date.AddMinutes(minutesValid) < DateTime.Now)
            {
                throw new Exception("GCL DecryptWithAes: The encrypted value has expired.");
            }

            return output.Remove(output.Length - 15);
        }

        /// <summary>
        /// Hashes a string, adds a salt, and returns the salt + hashed bytes as a Base64 string.
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <param name="saltBytes">Optional: A byte array of the salt to use. If this contains no value, then a random salt will be generated and used.</param>
        /// <returns>The Base64 string containing the SHA512 hash of the input + salt and the salt appended to it.</returns>
        public static string ToSha512ForPasswords(this string input, byte[] saltBytes = null)
        {
            using var sha512Hasher = SHA512.Create();

            var inputBytes = Encoding.UTF8.GetBytes(input);

            if (saltBytes == null)
            {
                var random = new Random();
                var saltSize = random.Next(8, 12);
                saltBytes = new byte[saltSize];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(saltBytes);
            }

            var inputWithSaltBytes = new byte[inputBytes.Length + saltBytes.Length];
            Buffer.BlockCopy(inputBytes, 0, inputWithSaltBytes, 0, inputBytes.Length);
            Buffer.BlockCopy(saltBytes, 0, inputWithSaltBytes, inputBytes.Length, saltBytes.Length);

            var hashedBytes = sha512Hasher.ComputeHash(inputWithSaltBytes);

            // Add the salt to the hashed bytes.
            var outputBytes = new byte[hashedBytes.Length + saltBytes.Length];
            Buffer.BlockCopy(hashedBytes, 0, outputBytes, 0, hashedBytes.Length);
            Buffer.BlockCopy(saltBytes, 0, outputBytes, hashedBytes.Length, saltBytes.Length);

            return Convert.ToBase64String(outputBytes);
        }

        /// <summary>
        /// Hashes a string, and returns the hashed bytes as a Base64 string.
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>The Base64 string containing the SHA512 hash of the input.</returns>
        public static string ToSha512Simple(this string input)
        {
            using var sha512Hasher = SHA512.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashedBytes = sha512Hasher.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Function Verifies an SHA512 hash (including salt) with the given input and returns whether it matches.
        /// The salt is removed from the hash and the input is hashed with the same salt is then compared this.
        /// </summary>
        /// <param name="input">The plain-text string to validate.</param>
        /// <param name="hash">A Base64 string containing the SHA512 hash of a password + salt, appended with the same salt.</param>
        /// <returns>Whether or not the input contains the same password as the one in the hash.</returns>
        public static bool VerifySha512(this string input, string hash)
        {
            // Convert base64-encoded hash value into a byte array.
            var hashWithSaltBytes = Convert.FromBase64String(hash);

            // We must know size of hash (without salt).
            var hashSizeInBytes = 64;

            // Make sure that the specified hash value is long enough.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
            {
                return false;
            }

            // Allocate array to hold original salt bytes retrieved from hash.
            var saltBytes = new byte[hashWithSaltBytes.Length - hashSizeInBytes];

            // Copy salt from the end of the hash to the new array.
            for (var i = 0; i < saltBytes.Length; i++)
            {
                saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];
            }

            // Compute a new hash string.
            var expectedHashString = input.ToSha512ForPasswords(saltBytes);

            return expectedHashString == hash;
        }

        /// <summary>
        /// Determines if a string is contained in a specified sequence by using a specified equality comparer.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <param name="comparer">The <see cref="StringComparer"/> that will be used when comparing values.</param>
        /// <param name="values">The array of values to check against.</param>
        /// <returns>Whether or not the one of the values is the same as the input.</returns>
        public static bool InList(this string input, StringComparer comparer, params string[] values)
        {
            return values.Contains(input, comparer);
        }

        /// <summary>
        /// Determines if a string is contained in a specified sequence by using the <see cref="StringComparer.Ordinal"/> equality comparer.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <param name="values">The array of values to check against.</param>
        /// <returns>Whether or not the one of the values is the same as the input.</returns>
        public static bool InList(this string input, params string[] values)
        {
            return input.InList(StringComparer.Ordinal, values);
        }

        /// <summary>
        /// Convert a string to a Dictionary, splitting rows with [RowSplitter] and cells with [ColumnSplitter].
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="rowSplitter">The value that is used to divide the rows.</param>
        /// <param name="columnSplitter">The value that is used to divide the columns.</param>
        /// <param name="addSortInKey">Optional: Set to true to prepend all keys/columns with an index/counter and an underscore (_). Default value is false.</param>
        /// <param name="useFirstEntryIfExist">Optional: If the input contains duplicate keys/columns, then the first one will be used if this is set to true, or the last one if it's set to false. Default value is true.</param>
        /// <returns>The Dictionary made from the input.</returns>
        public static Dictionary<string, string> ToDictionary(this string input, string rowSplitter, string columnSplitter, bool addSortInKey = false, bool useFirstEntryIfExist = true)
        {
            var informationHolder = new List<string>();
            var parameterList = new Dictionary<string, string>();
            var counter = 0;

            // Breek de string af en zet deze in een sortedList
            informationHolder.AddRange(input.Split(rowSplitter));
            foreach (var item in informationHolder)
            {
                if (!item.Contains(columnSplitter))
                {
                    continue;
                }

                var key = item.Split(columnSplitter)[0];
                if (addSortInKey)
                {
                    key = counter.ToString("0000") + "_" + key;
                    counter += 1;
                }
                if (parameterList.ContainsKey(key))
                {
                    if (!useFirstEntryIfExist)
                    {
                        parameterList[key] = item.Split(columnSplitter)[1];
                    }
                }
                else
                {
                    parameterList.Add(key, item.Split(columnSplitter)[1]);
                }
            }

            return parameterList;
        }

        /// <summary>
        /// Displays the entered string with the first letter in uppercase.
        /// </summary>
        /// <param name="input">The string to capitalize the first letter of.</param>
        /// <returns>The input with the first letter capitalized.</returns>
        public static string CapitalizeFirst(this string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return String.Empty;
            }

            return input[0].ToString().ToUpper() + input[1..];
        }

        /// <summary>
        /// Removes illegal characters from a file name.
        /// </summary>
        /// <param name="input">The file name.</param>
        /// <returns>A string that can be safely used as a file name on the machine that this application runs on.</returns>
        public static string StripIllegalFilenameCharacters(this string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            var regexSearch = Path.GetInvalidFileNameChars().ToString();
            var regex = new Regex($"[{Regex.Escape(regexSearch!)}]", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
            var output = regex.Replace(input, "");
            return output.Replace("&", "_").Replace("+", "_");
        }

        /// <summary>
        /// Removes illegal characters from a directory path.
        /// </summary>
        /// <param name="input">The directory name.</param>
        /// <returns>A string that can be safely used as a directory name on the machine that this application runs on.</returns>
        public static string StripIllegalPathCharacters(this string input)
        {
            var regexSearch = Path.GetInvalidPathChars().ToString();
            var regex = new Regex($"[{Regex.Escape(regexSearch!)}]", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
            var output = regex.Replace(input, "");
            return output.Replace("&", "_").Replace("+", "_");
        }
    }
}