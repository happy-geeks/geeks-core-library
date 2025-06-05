using System;

namespace GeeksCoreLibrary.Core.Extensions;

public static class TypeExtensions
{
    /// <summary>
    /// Get the default value for a given type.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get the default value of.</param>
    /// <returns>The default value for the given <see cref="Type"/>.</returns>
    public static object GetDefaultValue(this Type type)
    {
        // For reference types and nullable types, this is null.
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
        {
            return null;
        }

        // For value types, return default(T).
        return Activator.CreateInstance(type);
    }
}