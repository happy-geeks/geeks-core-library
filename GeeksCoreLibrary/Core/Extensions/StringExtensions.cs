using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// URL encode a string
        /// </summary>
        /// <param name="input"></param>
        public static string UrlEncode(this string input)
        {
            return System.Net.WebUtility.UrlEncode(input);
        }

        /// <summary>
        /// URL decode a string
        /// </summary>
        /// <param name="input"></param>
        public static string UrlDecode(this string input)
        {
            return System.Net.WebUtility.UrlDecode(input);
        }

        public static string HtmlDecode(this string input)
        {
            return System.Net.WebUtility.HtmlDecode(input);
        }

        public static string HtmlEncode(this string input)
        {
            return System.Net.WebUtility.HtmlEncode(input);
        }

        /// <summary>
        /// Replace function that is case insensitive
        /// </summary>
        public static string ReplaceCaseInsensitive(this string input, string oldValue, string newValue)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            var sb = new StringBuilder();

            var previousIndex = 0;
            var index = input.IndexOf(oldValue, StringComparison.CurrentCultureIgnoreCase);
            while (index != -1)
            {
                sb.Append(input[previousIndex..index]);
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = input.IndexOf(oldValue, index, StringComparison.CurrentCultureIgnoreCase);
            }
            sb.Append(input[previousIndex..]);

            return sb.ToString();
        }

        /// <summary>
        /// Converts a string to a SEO-friendly variant.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
        /// <param name="input"></param>
        /// <param name="encloseInQuotes">Whether the value should be enclosed in quotes. You should never set this to <see langword="false"/>, unless you add quotes manually in your query! Otherwise SQL injection will still be possible!</param>
        /// <returns></returns>
        public static string ToMySqlSafeValue(this string input, bool encloseInQuotes)
        {
            if (input == null)
            {
                return null;
            }

            var result = MySql.Data.MySqlClient.MySqlHelper.EscapeString(input);
            return encloseInQuotes ? $"'{result}'" : result;
        }

        /// <summary>
        /// Encrypts a value with AES.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <param name="withDateTime"></param>
        /// <returns></returns>
        public static string EncryptWithAes(this string input, string key = "", bool withDateTime = false)
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
                throw new Exception("EncryptWithAes: No AES secret key set.");
            }

            if (withDateTime)
            {
                stringToEncrypt.Append('~').Append(DateTime.Now.ToString("yyyyMMddHHmmss"));
            }

            // Salt of at least 8 bytes is requires to derive key. A basic salt of 8 bytes with value 0 is created.
            var basicSalt = new byte[8];
            using var deriveBytes = new Rfc2898DeriveBytes(encryptionKey, basicSalt, 2);
            const int KeySize = 256;
            const int BlockSize = 128;

            using var aesManaged = new AesManaged
            {
                KeySize = KeySize,
                BlockSize = BlockSize,
                Key = deriveBytes.GetBytes(Convert.ToInt32(KeySize / 8)),
                IV = deriveBytes.GetBytes(Convert.ToInt32(BlockSize / 8))
            };
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

        /// <summary>
        /// Decrypts a value with AES.
        /// </summary>
        /// <param name="input">The string to decrypt.</param>
        /// <param name="key">Optional: The encryption key to use. Default value is the value of "ExpiringEncryptionKey" from the app settings.</param>
        /// <param name="withDateTime">Optional: Set the <see langword="true"/> if the value contains a validation date and time. Default is <see langword="false"/>.</param>
        /// <param name="minutesValidOverride">Optional: If you want the encryption to be valid for a different amount of time than what it set in the appsettings, you can change that here.</param>
        /// <returns>The decrypted string.</returns>
        public static string DecryptWithAes(this string input, string key = "", bool withDateTime = false, int minutesValidOverride = 0)
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
                throw new Exception("DecryptWithAes: No AES secret key set.");
            }

            // Salt of at least 8 bytes is required to derive key. A basic salt of 8 bytes with value 0 is created.
            var basicSalt = new byte[8];
            var inputBytes = Convert.FromBase64String(input);
            string output;

            const int keySize = 256;
            const int blockSize = 128;

            using var deriveBytes = new Rfc2898DeriveBytes(encryptionKey, basicSalt, 2);
            using var aesManaged = new AesManaged
            {
                KeySize = keySize,
                BlockSize = blockSize,
                Key = deriveBytes.GetBytes(Convert.ToInt32(keySize / 8)),
                IV = deriveBytes.GetBytes(Convert.ToInt32(blockSize / 8))
            };
            using var decryptor = aesManaged.CreateDecryptor(aesManaged.Key, aesManaged.IV);
            using var ms = new MemoryStream(inputBytes);
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                using var sr = new StreamReader(cs);
                output = sr.ReadToEnd();
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
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <param name="withDateTime"></param>
        /// <returns></returns>
        public static string EncryptWithAesWithSalt(this string input, string key = "", bool withDateTime = false)
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

            // Create salt.
            var random = new Random();
            var saltSize = random.Next(8, 12);
            var saltBytes = new byte[saltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }

            using var rijndael = new RijndaelManaged
            {
                Mode = CipherMode.CBC
            };

            // Derive the key and IV from the password and the salt.
            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(encryptionKey, saltBytes, 2);
            var keyBytes = rfc2898DeriveBytes.GetBytes(rijndael.KeySize / 8);
            var ivBytes = rfc2898DeriveBytes.GetBytes(rijndael.BlockSize / 8);

            using var encryptor = rijndael.CreateEncryptor(keyBytes, ivBytes);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(inputBytes, 0, inputBytes.Length);
            cryptoStream.FlushFinalBlock();
            var resultBytes = memoryStream.ToArray();

            var outputBytes = new byte[resultBytes.Length + saltBytes.Length];
            Buffer.BlockCopy(resultBytes, 0, outputBytes, 0, resultBytes.Length);
            Buffer.BlockCopy(saltBytes, 0, outputBytes, resultBytes.Length, saltBytes.Length);

            var output = new StringBuilder(Convert.ToBase64String(outputBytes));
            output.Replace("/", "-");

            return output.ToString();
        }

        /// <summary>
        /// Decrypts a value with AES. This method uses a salt, so it can decrypt values encrypted with <see cref="EncryptWithAesWithSalt"/>.
        /// </summary>
        /// <param name="input">The string to decrypt.</param>
        /// <param name="key">Optional: The encryption key to use. Default value is the value of "ExpiringEncryptionKey" from the app settings.</param>
        /// <param name="withDateTime">Optional: Set the <see langword="true"/> if the value contains a validation date and time. Default is <see langword="false"/>.</param>
        /// <param name="minutesValidOverride">Optional: If you want the encryption to be valid for a different amount of time than what it set in the appsettings, you can change that here.</param>
        /// <returns>The decrypted string.</returns>
        public static string DecryptWithAesWithSalt(this string input, string key = "", bool withDateTime = false, int minutesValidOverride = 0)
        {
            string encryptionKey;
            var stringToDecrypt = new StringBuilder(input);

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

            stringToDecrypt.Replace("-", "/");

            var inputWithSaltBytes = Convert.FromBase64String(Uri.UnescapeDataString(stringToDecrypt.ToString()));

            var saltByteLength = inputWithSaltBytes.Length % 16;

            var inputBytes = new byte[16 * (inputWithSaltBytes.Length / 16)];
            var saltBytes = new byte[saltByteLength];

            Buffer.BlockCopy(inputWithSaltBytes, 0, inputBytes, 0, inputBytes.Length);
            Buffer.BlockCopy(inputWithSaltBytes, inputBytes.Length, saltBytes, 0, saltBytes.Length);

            using var rijndael = new RijndaelManaged { Mode = CipherMode.CBC };

            // Derive the key and IV from the password and the salt.
            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(encryptionKey, saltBytes, 2);
            var keyBytes = rfc2898DeriveBytes.GetBytes(rijndael.KeySize / 8);
            var ivBytes = rfc2898DeriveBytes.GetBytes(rijndael.BlockSize / 8);

            // Declare various usings here. They will be disposed at the end of the function.
            using var decryptor = rijndael.CreateDecryptor(keyBytes, ivBytes);
            using var memoryStream = new MemoryStream(inputBytes);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            var outputBytes = new byte[inputBytes.Length];

            // Perform the decryption.
            var bytesCount = cryptoStream.Read(outputBytes, 0, outputBytes.Length);

            // Turn the decrypted bytes into a string. It is assumed here that the string was encrypted with UTF-8.
            var output = Encoding.UTF8.GetString(outputBytes, 0, bytesCount);

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
        /// Encrypts a string using a optional or standard encryption key.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string EncryptTripleDes(this string input, string key = "")
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                key = GclSettings.Current.DefaultEncryptionKeyTripleDes;
            }

            using var des = new TripleDESCryptoServiceProvider();
            des.IV = new byte[8];
            using var pdb = new PasswordDeriveBytes(key, Array.Empty<byte>());
            des.Key = pdb.CryptDeriveKey("RC2", "MD5", 128, new byte[8]);
            var ms = new MemoryStream((input.Length * 2) - 1);
            byte[] encryptedBytes;
            using (var encStream = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
            {
                var plainBytes = Encoding.UTF8.GetBytes(input);
                encStream.Write(plainBytes, 0, plainBytes.Length);
                encStream.FlushFinalBlock();
                encryptedBytes = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(encryptedBytes, 0, (int)ms.Length);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Hashes a string, adds a salt, and returns the salt + hashed bytes as a Base64 string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToSha512ForPasswords(this string input, byte[] saltBytes = null)
        {
            using var sha512Hasher = new SHA512Managed();

            var inputBytes = Encoding.UTF8.GetBytes(input);

            if (saltBytes == null)
            {
                var random = new Random();
                var saltSize = random.Next(8, 12);
                saltBytes = new byte[saltSize];
                using var rng = new RNGCryptoServiceProvider();
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
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToSha512Simple(this string input)
        {
            using var sha512Hasher = new SHA512Managed();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashedBytes = sha512Hasher.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Function Verifies an SHA512 hash (including salt) with the given input and returns whether it matches.
        /// The salt is removed from the hash and the input is hashed with the same salt is then compared this.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
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
        /// <param name="input"></param>
        /// <param name="comparer">The <see cref="StringComparer"/> that will be used when comparing values.</param>
        /// <param name="values">The array of values to check against.</param>
        /// <returns></returns>
        public static bool InList(this string input, StringComparer comparer, params string[] values)
        {
            return values.Contains(input, comparer);
        }

        /// <summary>
        /// Determines if a string is contained in a specified sequence by using the <see cref="StringComparer.Ordinal"/> equality comparer.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="values">The array of values to check against.</param>
        /// <returns></returns>
        public static bool InList(this string input, params string[] values)
        {
            return input.InList(StringComparer.Ordinal, values);
        }

        /// <summary>
        /// Convert a string to a Dictionary, splitting rows with [RowSplitter] and cells with [ColumnSplitter].
        /// </summary>
        /// <param name="input"></param>
        /// <param name="rowSplitter"></param>
        /// <param name="columnSplitter"></param>
        /// <param name="addSortInKey"></param>
        /// <param name="useFirstEntryIfExist"></param>
        public static Dictionary<string, string> ToDictionary(this string input, string rowSplitter, string columnSplitter, bool addSortInKey = false, bool useFirstEntryIfExist = true)
        {
            List<string> informationHolder = new List<string>();
            Dictionary<string, string> parameterList = new Dictionary<string, string>();
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
        /// <param name="input"></param>
        /// <returns></returns>
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
        /// <param name="input"></param>
        /// <returns></returns>
        public static string StripIllegalFilenameCharacters(this string input)
        {
            var regexSearch = Path.GetInvalidFileNameChars().ToString();
            var regex = new Regex($"[{Regex.Escape(regexSearch)}]");
            var output = regex.Replace(input, "");
            return output.Replace("&", "_").Replace("+", "_");
        }
    }
}
