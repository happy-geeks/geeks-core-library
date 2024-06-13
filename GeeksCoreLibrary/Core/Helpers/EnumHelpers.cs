using System;
using System.Linq;
using System.Runtime.Serialization;

namespace GeeksCoreLibrary.Core.Helpers;

public static class EnumHelpers
{
    /// <summary>
    /// Gets the string value of an enum member.
    /// This will check if an <see cref="EnumMemberAttribute"/> exists on the member and return it's value if it does or return the name of it doesn't.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="type">The enum member.</param>
    /// <returns>The string value of the enum member.</returns>
    public static string ToEnumString<T>(T type) where T : struct, IConvertible
    {
        var enumType = typeof(T);
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("T must be an enumerated type");
        }

        var name = Enum.GetName(enumType, type);
        if (name == null)
        {
            return null;
        }

        var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name)?.GetCustomAttributes(typeof(EnumMemberAttribute), true))?.SingleOrDefault();
        return enumMemberAttribute == null ? name : enumMemberAttribute.Value;
    }

    /// <summary>
    /// Parses a string to an enum member.
    /// This will check if an <see cref="EnumMemberAttribute"/> exists on the member and return it's value if it does or return the name of it doesn't.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="input">The string to parse.</param>
    /// <returns>The enum member.</returns>
    public static T ToEnum<T>(string input)
    {
        var enumType = typeof(T);
        foreach (var name in Enum.GetNames(enumType))
        {
            var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name)?.GetCustomAttributes(typeof(EnumMemberAttribute), true))?.SingleOrDefault();

            if (enumMemberAttribute != null && String.Equals(enumMemberAttribute.Value, input, StringComparison.OrdinalIgnoreCase))
            {
                return (T)Enum.Parse(enumType, name);
            }

            if (String.Equals(name, input, StringComparison.OrdinalIgnoreCase))
            {
                return (T)Enum.Parse(enumType, name);
            }
        }

        // If we have no parsed value yet, see if the string is actually an int and parse that.
        if (Int32.TryParse(input, out var intValue))
        {
            return (T)Enum.ToObject(enumType, intValue);
        }

        return default;
    }
}