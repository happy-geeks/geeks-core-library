using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class DataRowExtensions
    {
        private const string SeoSuffix = "_seo";
        private const string HtmlEncodeAllowBrSuffix = "_htmlencode_allowbr";
        private const string HtmlEncodeSuffix = "_htmlencode";
        private const string EncryptSuffix = "_encrypt";
        private const string EncryptWithDateSuffix = "_encrypt_withdate";
        private const string EncryptListSuffix = "_encrypt_list";
        private const string EncryptListWithDateSuffix = "_encrypt_list_withdate";
        private const string NormalEncryptSuffix = "_normalencrypt";
        private const string NormalEncryptWithDateSuffix = "_normalencrypt_withdate";
        private const string NormalEncryptListSuffix = "_normalencrypt_list";
        private const string NormalEncryptListWithDateSuffix = "_normalencrypt_list_withdate";
        private const string DecryptSuffix = "_decrypt";
        private const string DecryptWithDateSuffix = "_decrypt_withdate";
        private const string NormalDecryptSuffix = "_normaldecrypt";
        private const string NormalDecryptWithDateSuffix = "_normaldecrypt_withdate";
        private const string SaferDecryptSuffix = "_saferdecrypt";
        private const string SaferEncryptSuffix = "_saferencrypt";
        private const string SaferDecryptWithDateSuffix = "_saferdecrypt_withdate";
        private const string SaferEncryptWithDateSuffix = "_saferencrypt_withdate";
        
        /// <summary>
        /// Gets a value from a <see cref="DataRow" />, if the columns exists.
        /// If the column does not exist, the default value will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataRow">The <see cref="DataRow"/> to get the value from.</param>
        /// <param name="columnName">The name of the column to get the value of.</param>
        /// <param name="defaultValue">Optional: The value to return if the column does not exist. The default value is default(T).</param>
        /// <returns></returns>
        public static T GetValueIfColumnExists<T>(this DataRow dataRow, string columnName, T defaultValue = default)
        {
            return !dataRow.Table.Columns.Contains(columnName) ? defaultValue : dataRow.Field<T>(columnName);
        }

        /// <summary>
        /// Gets a value from a <see cref="DataRow" />, if the columns exists.
        /// If the column does not exist, the default value will be returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataRow">The <see cref="DataRow"/> to get the value from.</param>
        /// <param name="columnName">The name of the column to get the value of.</param>
        /// <param name="defaultValue">Optional: The value to return if the column does not exist. The default value is default(T).</param>
        /// <returns></returns>
        public static object GetValueIfColumnExists(this DataRow dataRow, string columnName, object defaultValue = null)
        {
            return !dataRow.Table.Columns.Contains(columnName) ? defaultValue : dataRow[columnName];
        }

        /// <summary>
        /// Converts a <see cref="DataRow"/> to an <see cref="JObject"/>.
        /// This will also parse certain suffixes, such as _encrypt to encrypt the value of that column from the <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to convert.</param>
        /// <param name="requiredColumnNamePrefix">Optional: If you only want to take columns with a certain prefix from the <see cref="DataRow"/>, add that prefix here.</param>
        /// <param name="columnNamePrefixToSkip">Optional: All columns from the <see cref="DataRow"/> that have this prefix, will be skipped.</param>
        /// <param name="encryptionKey">Optional: The key to use for encrypting and decrypting values. If empty, the key from appSettings will be used.</param>
        /// <param name="skipNullValues">Optional: Set to <see langword="true"/> to skip values that are <see langword="null"/>. Default is <see langword="false"/>.</param>
        /// <param name="allowValueDecryption">Optional: Set to <see langword="true"/> to allow values to be decrypted (for columns that contain the _decrypt suffix for example), otherwise values will be added in the <see cref="JObject"/> as is. Default value is <see langword="false"/>.</param>
        /// <returns>An <see cref="JObject"/> with data from the <see cref="DataRow"/>.</returns>
        public static JObject ToJsonObject(this DataRow dataRow, string requiredColumnNamePrefix = null, string columnNamePrefixToSkip = null, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false)
        {
            if (dataRow == null)
            {
                return null;
            }

            var result = new JObject();
            foreach (DataColumn dataColumn in dataRow.Table.Columns)
            {
                // Skip values that are null, if requested.
                if (skipNullValues && dataRow.IsNull(dataColumn))
                {
                    continue;
                }

                var columnName = dataColumn.ColumnName;

                // If we have a prefix, skip columns that don't have this prefix.
                if (!String.IsNullOrWhiteSpace(requiredColumnNamePrefix) && !columnName.StartsWith(requiredColumnNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // If we have a prefix to skip, skip columns that have this prefix.
                if (!String.IsNullOrWhiteSpace(columnNamePrefixToSkip) && columnName.StartsWith(columnNamePrefixToSkip, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Add value to dictionary.
                if (columnName.EndsWith(SeoSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    result.TryAdd(TrimPrefix(columnName[..^SeoSuffix.Length], requiredColumnNamePrefix), Convert.ToString(dataRow[dataColumn]).ConvertToSeo());
                }
                else if (columnName.EndsWith(HtmlEncodeAllowBrSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    result.TryAdd(TrimPrefix(columnName[..^HtmlEncodeAllowBrSuffix.Length], requiredColumnNamePrefix), Convert.ToString(dataRow[dataColumn]).HtmlEncode().Replace("&lt;br /&gt;", "<br />"));
                }
                else if (columnName.EndsWith(HtmlEncodeSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    result.TryAdd(TrimPrefix(columnName[..^HtmlEncodeSuffix.Length], requiredColumnNamePrefix), Convert.ToString(dataRow[dataColumn]).HtmlEncode());
                }
                else if (columnName.EndsWith(EncryptSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(EncryptWithDateSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var withDate = columnName.EndsWith(EncryptWithDateSuffix, StringComparison.OrdinalIgnoreCase);
                    var value = Convert.ToString(dataRow[dataColumn]);
                    if (!String.IsNullOrWhiteSpace(value))
                    {
                        value = value.EncryptWithAesWithSalt(encryptionKey, withDate);
                    }

                    result.TryAdd(TrimPrefix(columnName[..^(withDate ? EncryptWithDateSuffix : EncryptSuffix).Length], requiredColumnNamePrefix), value.UrlEncode());
                }
                else if (columnName.EndsWith(EncryptListSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(EncryptListWithDateSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var withDate = columnName.EndsWith(EncryptListWithDateSuffix, StringComparison.OrdinalIgnoreCase);
                    var value = Convert.ToString(dataRow[dataColumn]);
                    if (!String.IsNullOrWhiteSpace(value))
                    {
                        var encryptedValues = value.Split(',').Select(subValue => subValue.EncryptWithAesWithSalt(encryptionKey, withDate).UrlEncode());
                        value = String.Join(",", encryptedValues);
                    }

                    result.TryAdd(TrimPrefix(columnName[..^(withDate ? EncryptListWithDateSuffix : EncryptListSuffix).Length], requiredColumnNamePrefix), value);
                }
                else if (columnName.EndsWith(NormalEncryptSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(NormalEncryptWithDateSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(SaferEncryptSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(SaferEncryptWithDateSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var withDate = columnName.EndsWith(NormalEncryptWithDateSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(SaferEncryptWithDateSuffix, StringComparison.OrdinalIgnoreCase);
                    var saferMethod = columnName.EndsWith(SaferEncryptSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(SaferEncryptWithDateSuffix, StringComparison.OrdinalIgnoreCase);
                    var value = Convert.ToString(dataRow[dataColumn]);
                    if (!String.IsNullOrWhiteSpace(value))
                    {
                        value = value.EncryptWithAes(encryptionKey, withDate, useSlowerButMoreSecureMethod:saferMethod);
                    }
                    if (saferMethod)
                    {
                        result.TryAdd(TrimPrefix(columnName[..^(withDate ? SaferEncryptWithDateSuffix : SaferEncryptSuffix).Length], requiredColumnNamePrefix), value.UrlEncode());
                    }
                    else
                    {
                        result.TryAdd(TrimPrefix(columnName[..^(withDate ? NormalEncryptWithDateSuffix : NormalEncryptSuffix).Length], requiredColumnNamePrefix), value.UrlEncode());
                    }
                }
                else if (columnName.EndsWith(NormalEncryptListSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(NormalEncryptListWithDateSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var withDate = columnName.EndsWith(NormalEncryptListWithDateSuffix, StringComparison.OrdinalIgnoreCase);
                    var value = Convert.ToString(dataRow[dataColumn]);
                    if (!String.IsNullOrWhiteSpace(value))
                    {
                        var encryptedValues = value.Split(',').Select(subValue => subValue.EncryptWithAes(encryptionKey, withDate).UrlEncode());
                        value = String.Join(",", encryptedValues);
                    }

                    result.TryAdd(TrimPrefix(columnName[..^(withDate ? NormalEncryptListWithDateSuffix : NormalEncryptListSuffix).Length], requiredColumnNamePrefix), value);
                }
                else if (allowValueDecryption && (columnName.EndsWith(DecryptSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(DecryptWithDateSuffix, StringComparison.OrdinalIgnoreCase)))
                {
                    var withDate = columnName.EndsWith(DecryptWithDateSuffix, StringComparison.OrdinalIgnoreCase);
                    var value = Convert.ToString(dataRow[dataColumn]) ?? "";

                    if (value.Contains(","))
                    {
                        var values = value.Split(',');
                        for (var i = 0; i < values.Length; i++)
                        {
                            values[i] = values[i].UrlDecode().Replace(" ", "+").DecryptWithAesWithSalt(encryptionKey, withDate);
                        }

                        value = String.Join(',', values);
                    }
                    else if (!String.IsNullOrWhiteSpace(value))
                    {
                        value = value.UrlDecode().Replace(" ", "+").DecryptWithAesWithSalt(encryptionKey, withDate);
                    }

                    result.TryAdd(TrimPrefix(columnName[..^(withDate ? DecryptWithDateSuffix : DecryptSuffix).Length], requiredColumnNamePrefix), value);
                }
                else if (allowValueDecryption && (columnName.EndsWith(NormalDecryptSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(NormalDecryptWithDateSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(SaferDecryptSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(SaferDecryptWithDateSuffix, StringComparison.OrdinalIgnoreCase)))
                {
                    var withDate = columnName.EndsWith(NormalDecryptWithDateSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(SaferDecryptWithDateSuffix, StringComparison.OrdinalIgnoreCase);
                    var saferMethod = columnName.EndsWith(SaferDecryptSuffix, StringComparison.OrdinalIgnoreCase) || columnName.EndsWith(SaferDecryptWithDateSuffix, StringComparison.OrdinalIgnoreCase);
                    var value = Convert.ToString(dataRow[dataColumn]) ?? "";

                    if (value.Contains(","))
                    {
                        var values = value.Split(',');
                        for (var i = 0; i < values.Length; i++)
                        {
                            values[i] = values[i].UrlDecode().Replace(" ", "+").DecryptWithAes(encryptionKey, withDate, useSlowerButMoreSecureMethod:saferMethod);
                        }

                        value = String.Join(',', values);
                    }
                    else if (!String.IsNullOrWhiteSpace(value))
                    {
                        value = value.UrlDecode().Replace(" ", "+").DecryptWithAes(encryptionKey, withDate, useSlowerButMoreSecureMethod:saferMethod);
                    }

                    result.TryAdd(TrimPrefix(columnName[..^(withDate ? (saferMethod ? SaferDecryptWithDateSuffix : NormalDecryptWithDateSuffix) : (saferMethod ? SaferDecryptSuffix : NormalDecryptSuffix)).Length], requiredColumnNamePrefix), value);
                }
                else
                {
                    result.TryAdd(TrimPrefix(columnName, requiredColumnNamePrefix), new JValue(dataRow[dataColumn]));
                }
            }

            return result;
        }

        /// <summary>
        /// Get and decrypt a secret key from a <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to get the secret key from.</param>
        /// <param name="itemDetailKey">The name of the column that contains the secret key, without the environment suffix.</param>
        /// <returns>The decrypted secret key, if it exists, or null if it doesn't.</returns>
        public static string GetAndDecryptSecretKey(this DataRow dataRow, string itemDetailKey)
        {
            var result = dataRow.Field<string>(itemDetailKey);
            return String.IsNullOrWhiteSpace(result) ? result : result.DecryptWithAesWithSalt();
        }

        /// <summary>
        /// Get and attempt to convert a value from a <see cref="DataRow"/> to an <see cref="Enum"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to get the enum value from.</param>
        /// <param name="columnName">The name of the column that contains the enum value.</param>
        /// <typeparam name="TEnum">An <see cref="Enum"/> type.</typeparam>
        /// <returns>The value of the enum, or the default value of the enum if the value couldn't be parsed.</returns>
        public static TEnum GetEnumValue<TEnum>(this DataRow dataRow, string columnName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse(dataRow.Field<string>(columnName), out TEnum result))
            {
                result = default;
            }

            return result;
        }

        /// <summary>
        /// Trims a prefix from a column name.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="prefix">The prefix to trim.</param>
        /// <returns>The column name without the prefix.</returns>
        private static string TrimPrefix(string columnName, string prefix)
        {
            if (!String.IsNullOrWhiteSpace(columnName) && !String.IsNullOrWhiteSpace(prefix) && columnName.StartsWith(prefix))
            {
                return columnName[prefix.Length..];
            }

            return columnName;
        }
    }
}