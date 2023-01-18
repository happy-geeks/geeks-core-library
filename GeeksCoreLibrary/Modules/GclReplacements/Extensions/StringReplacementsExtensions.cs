using GeeksCoreLibrary.Core.Extensions;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace GeeksCoreLibrary.Modules.GclReplacements.Extensions
{
    public static class StringReplacementsExtensions
    {
        /// <summary>
        /// This just returns the input as is. This can be used to bypass the default HtmlEncode formatter if needed.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Raw(this string input)
        {
            return input;
        }

        /// <summary>
        /// Converts a string to a SEO-friendly variant.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Seo(this string input)
        {
            return input?.ConvertToSeo();
        }

        /// <summary>
        /// Encodes special characters to make the string safe for HTML.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HtmlEncode(this string input)
        {
            return String.IsNullOrEmpty(input) ? input : System.Text.Encodings.Web.HtmlEncoder.Default.Encode(input);
        }

        /// <summary>
        /// Encodes the string to make it URL-safe. The string is escaped according to RFC 2396.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UrlEncode(this string input)
        {
            return input == null ? null : Uri.EscapeDataString(input);
        }

        /// <summary>
        /// Decodes the string from an URL-safe string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UrlDecode(this string input)
        {
            return input == null ? null : Uri.UnescapeDataString(input);
        }

        /// <summary>
        /// Cuts a string to a maximum amount of characters.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="maxLength"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string CutString(this string input, int maxLength, string suffix = "")
        {
            if (String.IsNullOrEmpty(input) || input.Length <= maxLength)
            {
                return input;
            }

            if (String.IsNullOrEmpty(suffix))
            {
                return input.Substring(0, maxLength);
            }

            if (suffix.Length > maxLength)
            {
                throw new ArgumentException($"The length of the suffix cannot exceed {nameof(maxLength)}.", nameof(suffix));
            }

            var output = new StringBuilder(input.Substring(0, maxLength - suffix.Length));
            output.Append(suffix);

            return output.ToString();
        }

        /// <summary>
        /// Converts a decimal to a numeric representation using the given number format and culture.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="numberFormat">The number format. Defaults to N2.</param>
        /// <param name="cultureName">The culture name. Defaults to nl-NL.</param>
        /// <returns></returns>
        public static string FormatNumber(this decimal input, string numberFormat = "N2", string cultureName = "nl-NL")
        {
            return input.ToString(numberFormat, CultureInfo.CreateSpecificCulture(cultureName));
        }

        /// <summary>
        /// Converts a <see cref="decimal"/> to a currency representation. This will the use default culture of the application instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="includeCurrencySymbol"></param>
        /// <param name="cultureName"></param>
        /// <returns></returns>
        public static string Currency(this decimal input, bool includeCurrencySymbol = true, string cultureName = null)
        {
            var culture = !String.IsNullOrWhiteSpace(cultureName) ? new CultureInfo(cultureName) : CultureInfo.CurrentCulture;
            var output = input.ToString(includeCurrencySymbol ? "C" : $"N{culture.NumberFormat.CurrencyDecimalDigits}", culture);

            if (culture.Name.Equals("nl", StringComparison.OrdinalIgnoreCase) || culture.Name.Equals("nl-NL", StringComparison.OrdinalIgnoreCase))
            {
                output = output.Replace(",00", ",-");
            }

            return output;
        }

        /// <summary>
        /// Converts a <see cref="decimal"/> to a currency representation, and will wrap the decimal digits inside a &lt;sup&gt; tag. This will the use default culture of the application instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="includeCurrencySymbol"></param>
        /// <param name="cultureName"></param>
        /// <returns></returns>
        public static string CurrencySup(this decimal input, bool includeCurrencySymbol = true, string cultureName = null)
        {
            var culture = !String.IsNullOrWhiteSpace(cultureName) ? new CultureInfo(cultureName) : CultureInfo.CurrentCulture;
            var output = input.ToString(includeCurrencySymbol ? "C" : $"N{culture.NumberFormat.CurrencyDecimalDigits}", culture);

            var decimalSeparator = culture.NumberFormat.CurrencyDecimalSeparator;
            var outputParts = output.Split(decimalSeparator);

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(outputParts[0]).Append($"{decimalSeparator}<sup>").Append(outputParts[1]).Append("</sup>");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Returns a string with the first character in uppercase.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UppercaseFirst(this string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            // Only one character; simply return the string in uppercase.
            if (input.Length == 1)
            {
                return input.ToUpper();
            }

            // Use a StringBuilder to optimize string manipulations.
            var stringBuilder = new StringBuilder(input);
            stringBuilder.Remove(0, 1);
            stringBuilder.Insert(0, input[0].ToString().ToUpper());
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Returns a string with the first character in lowercase.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string LowercaseFirst(this string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            // Only one character; simply return the string in lowercase.
            if (input.Length == 1)
            {
                return input.ToLower();
            }

            // Use a StringBuilder to optimize string manipulations.
            var stringBuilder = new StringBuilder(input);
            stringBuilder.Remove(0, 1);
            stringBuilder.Insert(0, input[0].ToString().ToLower());
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Strips HTML tags to leave plain text. Any script tags are removed entirely, including the content.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string StripHtml(this string input)
        {
            var tagRegex = new Regex("<(.|\\n)*?>");
            var scriptRegex = new Regex("<script.*?/script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return tagRegex.Replace(scriptRegex.Replace(input, ""), "");
        }

        /// <summary>
        /// Makes the string safe to be used in a JSON string by escaping all quotes.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string JsonSafe(this string input)
        {
            return String.IsNullOrEmpty(input) ? input : HttpUtility.JavaScriptStringEncode(input);
        }

        /// <summary>
        /// Strips the "style=..." attribute from an HTML string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string StripInlineStyle(this string input)
        {
            var regex = new Regex(" ?style=\".*?\"");
            return String.IsNullOrEmpty(input) ? input : regex.Replace(input, "");
        }

        /// <summary>
        /// Encrypts a value with AES. This method uses a salt, so it's random every time.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="withDateTime"></param>
        /// <returns></returns>
        public static string Encrypt(this string input, bool withDateTime = false)
        {
            return input?.EncryptWithAesWithSalt(withDateTime: withDateTime);
        }

        /// <summary>
        /// Encrypts a value with AES.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="withDateTime"></param>
        /// <returns></returns>
        public static string EncryptNormal(this string input, bool withDateTime = false)
        {
            return input?.EncryptWithAes(withDateTime: withDateTime);
        }

        /// <summary>
        /// Decrypts a value with AES. This method uses a salt, so it can decrypt values encrypted with <see cref="StringExtensions.EncryptWithAesWithSalt"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="withDateTime"></param>
        /// <param name="minutesValidOverride"></param>
        /// <returns></returns>
        public static string Decrypt(this string input, bool withDateTime = false, int minutesValidOverride = 0)
        {
            return input?.DecryptWithAesWithSalt(withDateTime: withDateTime, minutesValidOverride: minutesValidOverride);
        }

        /// <summary>
        /// Decrypts a value with AES. This method does not use use a salt, so it can decrypt values encrypted with <see cref="StringExtensions.EncryptWithAes"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="withDateTime"></param>
        /// <param name="minutesValidOverride"></param>
        /// <returns></returns>
        public static string DecryptNormal(this string input, bool withDateTime = false, int minutesValidOverride = 0)
        {
            return input?.DecryptWithAes(withDateTime: withDateTime, minutesValidOverride: minutesValidOverride);
        }

        /// <summary>
        /// Converts a string value to its Base64 equivalent.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Base64(this string input)
        {
            return input == null ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Converts a date time to a string with the specified format.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="format">The format of the <see cref="DateTime"/>. Can be a default format or a custom format.</param>
        /// <param name="culture">The culture to format the <see cref="DateTime"/> with.</param>
        /// <returns></returns>
        public static string DateTime(this DateTime input, string format, string culture = null)
        {
            if (String.IsNullOrWhiteSpace(culture))
            {
                return input.ToString(format);
            }
            return input.ToString(format, new CultureInfo(culture));
        }

        /// <summary>
        /// Hashes a string, and returns the hashed bytes as a Base64 string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Sha512(this string input)
        {
            return input?.ToSha512ForPasswords();
        }

        /// <summary>
        /// Converts this string to uppercase.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="useInvariantCulture">Whether to use the casing rules of the invariant culture instead of those of the current culture.</param>
        /// <returns>The string converted to uppercase.</returns>
        public static string Uppercase(this string input, bool useInvariantCulture = false)
        {
            return useInvariantCulture ? input?.ToUpperInvariant() : input?.ToUpper();
        }

        /// <summary>
        /// Converts this string to lowercase.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="useInvariantCulture">Whether to use the casing rules of the invariant culture instead of those of the current culture.</param>
        /// <returns>The string converted to lowercase.</returns>
        public static string Lowercase(this string input, bool useInvariantCulture = false)
        {
            return useInvariantCulture ? input?.ToLowerInvariant() : input?.ToLower();
        }

        /// <summary>
        /// Converts an input string to an image URL. Note that the URL is always relative, starting with a '<c>/</c>'.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <returns>The image URL.</returns>
        public static string QrCode(this string input, int width, int height)
        {
            if (String.IsNullOrWhiteSpace(input) || width <= 0 || height <= 0)
            {
                return String.Empty;
            }

            var url = $"/barcodes/generate?input={Uri.EscapeDataString(input)}&format=qr_code&width={width}&height={height}";

            return url;
        }
    }
}
