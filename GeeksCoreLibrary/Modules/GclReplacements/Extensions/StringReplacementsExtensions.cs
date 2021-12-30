using GeeksCoreLibrary.Core.Extensions;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GeeksCoreLibrary.Modules.GclReplacements.Extensions
{
    public static class StringReplacementsExtensions
    {
        /// <summary>
        /// Converts a string to a SEO-friendly variant.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string Seo(this string input)
        {
            return input?.ConvertToSeo();
        }

        /// <summary>
        /// Encodes special characters to make the string safe for HTML.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string HtmlEncode(this string input)
        {
            return String.IsNullOrEmpty(input) ? input : System.Text.Encodings.Web.HtmlEncoder.Default.Encode(input);
        }

        /// <summary>
        /// Encodes the string to make it URL-safe. The string is escaped according to RFC 2396.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string UrlEncode(this string input)
        {
            return input == null ? null : Uri.EscapeDataString(input);
        }

        /// <summary>
        /// Cuts a string to a maximum amount of characters.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="maxLength"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
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
        // ReSharper disable once UnusedMember.Global
        public static string FormatNumber(this decimal input, string numberFormat = "N2", string cultureName = "nl-NL")
        {
            return input.ToString(numberFormat, CultureInfo.CreateSpecificCulture(cultureName));
        }

        /// <summary>
        /// Converts a <see cref="decimal"/> to a currency representation without the currency symbol. This will the use default culture of the application instance.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string Valuta(this decimal input)
        {
            return input.ToString("F2", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Converts a <see cref="decimal"/> to a currency representation, and will wrap the decimal digits inside a &lt;sup&gt; tag. This will the use default culture of the application instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="includeCurrencySymbol"></param>
        /// <param name="cultureString"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string ValutaSup(this decimal input, bool includeCurrencySymbol = true, string cultureString = null)
        {
            var culture = !String.IsNullOrWhiteSpace(cultureString) ? new CultureInfo(cultureString) : CultureInfo.CurrentCulture;

            var output = input.ToString(includeCurrencySymbol ? "C" : $"N{culture.NumberFormat.CurrencyDecimalDigits}", culture);

            var decimalSeparator = culture.NumberFormat.CurrencyDecimalSeparator;
            var outputParts = output.Split(decimalSeparator);

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(outputParts[0]).Append($"{decimalSeparator}<sup>").Append(outputParts[1]).Append("</sup>");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts a <see cref="decimal"/> to a currency representation. This will the use default culture of the application instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="includeCurrencySymbol"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string Currency(this decimal input, bool includeCurrencySymbol = true)
        {
            var culture = CultureInfo.CurrentCulture;
            var output = input.ToString(includeCurrencySymbol ? "C" : $"N{culture.NumberFormat.CurrencyDecimalDigits}", culture);

            if (culture.Name.Equals("nl", StringComparison.OrdinalIgnoreCase) || culture.Name.Equals("nl-NL", StringComparison.OrdinalIgnoreCase))
            {
                output = output.Replace(",00", ",-");
            }

            return output;
        }

        /// <summary>
        /// Returns a string with the first character in upper-case.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string UppercaseFirst(this string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            // Only one character; simply return the string in upper-case.
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
        /// Returns a string with the first character in lower-case.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string LowercaseFirst(this string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            // Only one character; simply return the string in lower-case.
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
        // ReSharper disable once UnusedMember.Global
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
        // ReSharper disable once UnusedMember.Global
        public static string JsonSafe(this string input)
        {
            return String.IsNullOrEmpty(input) ? input : input.Replace("\"", "\\\"");
        }

        /// <summary>
        /// Strips the "style=..." attribute from an HTML string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
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
        // ReSharper disable once UnusedMember.Global
        public static string Encrypt(this string input, bool withDateTime = false)
        {
            return input?.EncryptWithAesWithSalt(withDateTime: withDateTime);
        }

        /// <summary>
        /// Decrypts a value with AES. This method uses a salt, so it can decrypt values encrypted with <see cref="StringExtensions.EncryptWithAesWithSalt"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="withDateTime"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string Decrypt(this string input, bool withDateTime = false)
        {
            return input?.DecryptWithAesWithSalt(withDateTime: withDateTime);
        }

        /// <summary>
        /// Converts a string value to its Base64 equivalent.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string Base64(this string input)
        {
            return input == null ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Converts a date time to a string with the specified format.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string DateTime(this DateTime input, string format)
        {
            return input.ToString(format);
        }

        /// <summary>
        /// Hashes a string, and returns the hashed bytes as a Base64 string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static string Sha512(this string input)
        {
            return input?.ToSha512ForPasswords();
        }
    }
}
