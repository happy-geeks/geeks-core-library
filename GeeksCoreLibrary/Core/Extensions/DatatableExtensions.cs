using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GeeksCoreLibrary.Modules.Templates.Models;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Core.Extensions;

public static class DataTableExtensions
{
    /// <summary>
    /// Converts a <see cref="DataTable"/> to an <see cref="JArray"/>.
    /// </summary>
    /// <param name="dataTable">The <see cref="DataRow"/> to convert.</param>
    /// <param name="encryptionKey">Optional: The key to use for encrypting and decrypting values. If empty, the key from appSettings will be used.</param>
    /// <param name="skipNullValues">Optional: Set to <see langword="true"/> to skip values that are <see langword="null"/>. Default is <see langword="false"/>.</param>
    /// <param name="allowValueDecryption">Optional: Set to <see langword="true"/> to allow values to be decrypted (for columns that contain the _decrypt suffix for example), otherwise values will be added in the <see cref="JObject"/> as is. Default value is <see langword="false"/>.</param>
    /// <returns>An <see cref="JArray"/> with the data from the <see cref="DataTable"/>.</returns>
    public static JArray ToJsonArray(this DataTable dataTable, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false)
    {
        var result = new JArray();
        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            return result;
        }

        foreach (DataRow dataRow in dataTable.Rows)
        {
            result.Add(dataRow.ToJsonObject(encryptionKey: encryptionKey, skipNullValues: skipNullValues, allowValueDecryption: allowValueDecryption));
        }

        return result;
    }

    /// <summary>
    /// Turns properties that are grouped into a sub-array.
    /// </summary>
    /// <param name="token">A <see cref="JToken"/> that should have its type set to <see cref="JTokenType.Object"/>.</param>
    /// <param name="childItemsMustHaveId">Optional: Forces child items in an object to have a non-null value in the <c>id</c> column. This is for data selectors that have optional child items.</param>
    private static void MakeSubArray(JToken token, bool childItemsMustHaveId = false)
    {
        if (token.Type != JTokenType.Object)
        {
            return;
        }

        var item = (JObject) token;
        var properties = item.Properties().ToList();

        // Determine the groups, which are essentially the keys on which properties will be grouped together.
        var groups = properties.Where(p => p.Name.Contains("~")).Select(property => property.Name.Split('~')[0]).Distinct(StringComparer.Ordinal).ToList();
        var propertiesToRemove = new List<string>();

        foreach (var group in groups)
        {
            if (!properties.Any(p => p.Name.Equals($"{group}~id")) || (childItemsMustHaveId && properties.First(p => p.Name.Equals($"{group}~id")).Value.Type == JTokenType.Null))
            {
                propertiesToRemove.AddRange(properties.Where(p => p.Name.StartsWith($"{group}~", StringComparison.Ordinal)).Select(property => property.Name));
            }
            else
            {
                var subObject = new JObject();

                foreach (var property in properties.Where(property => property.Name.StartsWith($"{group}~", StringComparison.Ordinal)))
                {
                    propertiesToRemove.Add(property.Name);
                    subObject.Add(new JProperty(property.Name.Remove(0, group.Length + 1), property.Value));
                }

                // Properties within the newly created objects should also be handled.
                MakeSubArray(subObject);

                if (!item.ContainsKey(group))
                {
                    // Create an array for this group if one hasn't been created yet.
                    item.Add(group, new JArray());
                }

                // Add the object to the sub-array.
                ((JArray) item[group])?.Add(subObject);
            }
        }

        // Remove the original properties that have been merged into an array.
        foreach (var propertyName in propertiesToRemove)
        {
            item.Remove(propertyName);
        }
    }

    /// <summary>
    /// Merges objects inside an array by key.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private static JArray MergeJsonObjectsByKey(JArray array, string key)
    {
        // If the array contains only one item, or less, there is nothing to merge.
        if (array.Count <= 1)
        {
            return array;
        }

        var jsonMergeSettings = new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Union};
        var newList = new JArray();
        foreach (var token in array)
        {
            // This function is to merge objects based on property values of those objects, so if the token is not an object, there is nothing to merge.
            if (token is not JObject item)
            {
                newList.Add(token);
                continue;
            }

            // If the object doesn't contain the key, there is nothing to merge.
            if (!item.ContainsKey(key))
            {
                newList.Add(item);
                continue;
            }

            // If the value of the key is null, there is nothing to merge.
            var keyValue = item[key]?.ToString();
            if (String.IsNullOrWhiteSpace(keyValue))
            {
                newList.Add(item);
                continue;
            }

            // If the new list doesn't contain an item with the same key value, add the item to the list.
            var existingItem = (JObject) newList.FirstOrDefault(i => i[key]?.ToString() == keyValue);
            if (existingItem == null)
            {
                newList.Add(item);
                continue;
            }

            // Merge the properties of the current item into the existing item.
            foreach (var property in item.Properties().ToList())
            {
                if (!existingItem.ContainsKey(property.Name))
                {
                    // If the existing item doesn't contain the property, add it.
                    existingItem.Add(property.Name, property.Value);
                }
                else if (existingItem[property.Name] != null && existingItem[property.Name] is JArray existingPropertyAsArray && property.Value is JArray currentPropertyAsArray)
                {
                    // If the existing item contains the property, and both the existing property and the current property are arrays, merge the arrays.
                    existingPropertyAsArray.Merge(currentPropertyAsArray, jsonMergeSettings);
                }
                else
                {
                    // If the existing item contains the property, and either the existing property or the current property is not an array, overwrite the existing property with the current property.
                    existingItem[property.Name] = property.Value;
                }
            }
        }

        // Recursively merge any other sub arrays in the new list.
        foreach (var token in newList)
        {
            if (token is not JObject item)
            {
                continue;
            }

            foreach (var property in item.Properties().ToList())
            {
                if (property.Value is not JArray currentPropertyAsArray)
                {
                    continue;
                }

                property.Value = MergeJsonObjectsByKey(currentPropertyAsArray, key);
            }
        }

        return newList;
    }

    /// <summary>
    /// Converts a <see cref="DataTable"/> to an <see cref="JArray"/>.
    /// </summary>
    /// <param name="dataTable">The <see cref="DataTable"/> to convert.</param>
    /// <param name="groupingSettings"></param>
    /// <param name="encryptionKey">Optional: The key to use for encrypting and decrypting values. If empty, the key from appSettings will be used.</param>
    /// <param name="skipNullValues">Optional: Set to <see langword="true"/> to skip values that are <see langword="null"/>. Default is <see langword="false"/>.</param>
    /// <param name="allowValueDecryption">Optional: Set to <see langword="true"/> to allow values to be decrypted (for columns that contain the _decrypt suffix for example), otherwise values will be added in the <see cref="JObject"/> as is. Default value is <see langword="false"/>.</param>
    /// <param name="recursive"></param>
    /// <param name="childItemsMustHaveId">Optional: Forces child items in an object to have a non-null value in the <c>id</c> column. This is for data selectors that have optional child items.</param>
    /// <returns>A <see cref="JArray"/> with the data from the <see cref="DataTable"/>.</returns>
    public static JArray ToJsonArray(this DataTable dataTable, QueryGroupingSettings groupingSettings, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false, bool recursive = false, bool childItemsMustHaveId = false)
    {
        var result = new JArray();
        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            return result;
        }

        if (String.IsNullOrWhiteSpace(groupingSettings?.GroupingColumn))
        {
            return dataTable.ToJsonArray(encryptionKey, skipNullValues, allowValueDecryption);
        }

        if (!recursive)
        {
            return ToJsonArrayWithGrouping(dataTable, groupingSettings, encryptionKey, skipNullValues, allowValueDecryption);
        }

        // If recursive is set, then properties in objects within the array can be grouped together and become nested arrays.
        var innerResult = dataTable.ToJsonArray(encryptionKey, skipNullValues, allowValueDecryption);
        foreach (var item in innerResult)
        {
            MakeSubArray(item, childItemsMustHaveId);
        }

        // Merge objects inside array by key.
        innerResult = MergeJsonObjectsByKey(innerResult, groupingSettings.GroupingColumn);

        return innerResult;
    }

    /// <summary>
    /// Turns a <see cref="DataTable"/> into a <see cref="JArray"/> and groups items together using rules defined in <see cref="QueryGroupingSettings"/>.
    /// </summary>
    /// <param name="dataTable">The <see cref="DataTable"/> to convert.</param>
    /// <param name="groupingSettings"></param>
    /// <param name="encryptionKey">Optional: The key to use for encrypting and decrypting values. If empty, the key from appSettings will be used.</param>
    /// <param name="skipNullValues">Optional: Set to <see langword="true"/> to skip values that are <see langword="null"/>. Default is <see langword="false"/>.</param>
    /// <param name="allowValueDecryption">Optional: Set to <see langword="true"/> to allow values to be decrypted (for columns that contain the _decrypt suffix for example), otherwise values will be added in the <see cref="JObject"/> as is. Default value is <see langword="false"/>.</param>
    /// <returns></returns>
    private static JArray ToJsonArrayWithGrouping(DataTable dataTable, QueryGroupingSettings groupingSettings, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false)
    {
        var result = new JArray();

        var subRows = new JArray();
        var subObject = new JObject();
        object lastGroupingKeyValue = null;
        var keyCounter = 1;

        foreach (DataRow dataRow in dataTable.Rows)
        {
            // Check if the current row is different from the previous row.
            if (String.IsNullOrEmpty(groupingSettings.GroupingColumn) || lastGroupingKeyValue == null || !lastGroupingKeyValue.Equals(dataRow[groupingSettings.GroupingColumn]))
            {
                if (lastGroupingKeyValue != null)
                {
                    if (groupingSettings.ObjectInsteadOfArray && subObject.Count > 0)
                    {
                        AddObject(groupingSettings.GroupingFieldsPrefix, result, subObject);
                        subObject = new JObject();
                    }
                    else if (subRows.Any())
                    {
                        AddArray(groupingSettings.GroupingFieldsPrefix, result, subRows);
                        subRows = [];
                    }
                }

                var row = dataRow.ToJsonObject(null, groupingSettings.GroupingFieldsPrefix, encryptionKey, skipNullValues, allowValueDecryption);
                result.Add(row);

                if (String.IsNullOrWhiteSpace(groupingSettings.GroupingColumn))
                {
                    continue;
                }

                lastGroupingKeyValue = dataRow[groupingSettings.GroupingColumn];

                if (groupingSettings.ObjectInsteadOfArray && !String.IsNullOrEmpty(groupingSettings.GroupingKeyColumnName) && !String.IsNullOrEmpty(groupingSettings.GroupingValueColumnName))
                {
                    var keyColumnName = $"{groupingSettings.GroupingFieldsPrefix}{groupingSettings.GroupingKeyColumnName}";
                    var key = dataRow.IsNull(keyColumnName) ? "" : Convert.ToString(dataRow[keyColumnName]);
                    if (!String.IsNullOrEmpty(key))
                    {
                        while (subObject.ContainsKey($"{key}{(keyCounter > 1 ? keyCounter.ToString() : "")}"))
                        {
                            keyCounter++;
                        }

                        subObject.Add(new JProperty($"{key}{(keyCounter > 1 ? keyCounter.ToString() : "")}", dataRow[$"{groupingSettings.GroupingFieldsPrefix}{groupingSettings.GroupingValueColumnName}"]));
                    }
                }
                else if (!groupingSettings.ObjectInsteadOfArray)
                {
                    row = dataRow.ToJsonObject(groupingSettings.GroupingFieldsPrefix, null, encryptionKey, skipNullValues, allowValueDecryption);
                    subRows.Add(row);
                }
            }
            else
            {
                if (groupingSettings.ObjectInsteadOfArray && !String.IsNullOrEmpty(groupingSettings.GroupingKeyColumnName) && !String.IsNullOrEmpty(groupingSettings.GroupingValueColumnName))
                {
                    var keyColumnName = $"{groupingSettings.GroupingFieldsPrefix}{groupingSettings.GroupingKeyColumnName}";
                    var key = dataRow.IsNull(keyColumnName) ? "" : Convert.ToString(dataRow[keyColumnName]);
                    if (!String.IsNullOrEmpty(key))
                    {
                        while (subObject.ContainsKey($"{key}{(keyCounter > 1 ? keyCounter.ToString() : "")}"))
                        {
                            keyCounter++;
                        }

                        subObject.Add(new JProperty($"{key}{(keyCounter > 1 ? keyCounter.ToString() : "")}", dataRow[$"{groupingSettings.GroupingFieldsPrefix}{groupingSettings.GroupingValueColumnName}"]));
                    }
                }
                else if (!groupingSettings.ObjectInsteadOfArray)
                {
                    var row = dataRow.ToJsonObject(groupingSettings.GroupingFieldsPrefix, null, encryptionKey, skipNullValues, allowValueDecryption);
                    subRows.Add(row);
                }
            }
        }

        if (subRows.Any())
        {
            AddArray(groupingSettings.GroupingFieldsPrefix, result, subRows);
        }

        if (subObject.Count > 0)
        {
            AddObject(groupingSettings.GroupingFieldsPrefix, result, subObject);
        }

        return result;
    }

    /// <summary>
    /// Add a <see cref="JObject"/> to a <see cref="JArray"/>.
    /// </summary>
    /// <param name="groupingFieldsPrefix">The grouping fields prefix; If you want to have JSON with 2 levels, enter the prefix of all fields that should show up on the second level.</param>
    /// <param name="parent">The <see cref="JArray"/> to add the <see cref="JObject"/> too.</param>
    /// <param name="subObject">The <see cref="JObject"/> to add.</param>
    private static void AddObject(string groupingFieldsPrefix, JArray parent, JObject subObject)
    {
        if (parent.Last() is JObject lastObject)
        {
            lastObject.Add(new JProperty(groupingFieldsPrefix, subObject));
        }
        else
        {
            parent.Last().AddAfterSelf(new JProperty(groupingFieldsPrefix, subObject));
        }
    }

    /// <summary>
    /// Add a <see cref="JArray"/> to a <see cref="JArray"/>.
    /// </summary>
    /// <param name="groupingFieldsPrefix">The grouping fields prefix; If you want to have JSON with 2 levels, enter the prefix of all fields that should show up on the second level.</param>
    /// <param name="parent">The <see cref="JArray"/> to add the <see cref="JArray"/> too.</param>
    /// <param name="subRows">The <see cref="JArray"/> to add.</param>
    private static void AddArray(string groupingFieldsPrefix, JArray parent, JArray subRows)
    {
        if (parent.Last() is JObject lastObject)
        {
            lastObject.Add(new JProperty(groupingFieldsPrefix, subRows));
        }
        else
        {
            parent.Last().AddAfterSelf(new JProperty(groupingFieldsPrefix, subRows));
        }
    }
}