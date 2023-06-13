using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Core.Helpers;

public class JsonHelpers
{
    /// <summary>
    /// If you have a JSON array with objects and those objects have sub objects, this function will merge the sub objects into the parent object.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static JArray FlattenJsonArray(JArray input, string prefix = "")
    {
        if (input == null || !input.Any())
        {
            return input;
        }

        var output = new JArray();
        foreach (var item in input)
        {
            if (item is not JObject itemAsObject)
            {
                output.Add(item);
            }
            else
            {
                // Collect all different kinds of properties that we'll need.
                var arrayProperties = new List<JProperty>();
                var otherProperties = new List<JProperty>();

                foreach (var property in itemAsObject.Properties())
                {
                    switch (property.Value)
                    {
                        case JArray:
                        {
                            arrayProperties.Add(property);
                            break;
                        }
                        default:
                        {
                            otherProperties.Add(property);
                            break;
                        }
                    }
                }

                if (!arrayProperties.Any())
                {
                    output.Add(item);
                    continue;
                }

                foreach (var property in arrayProperties)
                {
                    var array = FlattenJsonArray((JArray) property.Value, $"{property.Name}_");

                    if (!array.Any())
                    {
                        // Create a new flattened object.
                        var flattenedObject = new JObject();

                        // Copy the non-array properties to the flattened object.
                        foreach (var nonArrayProperty in otherProperties)
                        {
                            flattenedObject.Add(nonArrayProperty.Name, nonArrayProperty.Value);
                        }

                        output.Add(flattenedObject);
                    }
                    else
                    {
                        foreach (var arrayItem in array)
                        {
                            if (arrayItem is not JObject subItemAsObject)
                            {
                                continue;
                            }

                            // Create a new flattened object.
                            var flattenedObject = new JObject();

                            // Copy the non-array properties to the flattened object.
                            foreach (var nonArrayProperty in otherProperties)
                            {
                                flattenedObject.Add(nonArrayProperty.Name, nonArrayProperty.Value);
                            }

                            // Add the properties from the current array item to the flattened object.
                            foreach (var arrayProperty in subItemAsObject.Properties())
                            {
                                flattenedObject.Add($"{prefix}{property.Name}_{arrayProperty.Name}", arrayProperty.Value);
                            }

                            output.Add(flattenedObject);
                        }
                    }
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Get all unique property names of all objects from a JSON array.
    /// </summary>
    /// <param name="jsonArray">The array to get all unique property names from.</param>
    /// <returns>A HashSet that only stores unique values.</returns>
    public static HashSet<string> GetUniquePropertyNames(JArray jsonArray)
    {
        var uniquePropertyNames = new HashSet<string>();

        foreach (var jToken in jsonArray)
        {
            var item = (JObject) jToken;
            var properties = item.Properties();
            foreach (var property in properties)
            {
                uniquePropertyNames.Add(property.Name);
            }
        }

        return uniquePropertyNames;
    }
}