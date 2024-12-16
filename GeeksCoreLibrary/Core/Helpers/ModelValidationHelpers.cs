using System.Reflection;

namespace GeeksCoreLibrary.Core.Helpers;

public static class ModelValidationHelpers
{
    public static bool IsValid<T>(T model)
    {
        if (model == null) return false;

        // Get all public instance properties of the class
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Check if any property is null or empty (for string properties)
        foreach (var property in properties)
        {
            var value = property.GetValue(model);

            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return false;
            }
        }

        return true;
    }
}