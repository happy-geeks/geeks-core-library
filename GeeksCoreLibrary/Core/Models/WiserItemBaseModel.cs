using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// A base class for the <see cref="WiserItemModel"/> and <see cref="WiserItemLinkModel"/> classes to share functionality for details.
/// </summary>
public abstract class WiserItemBaseModel
{
    private readonly List<WiserItemDetailModel> details = new();

    /// <summary>
    /// Gets or sets the item details.
    /// </summary>
    public IReadOnlyCollection<WiserItemDetailModel> Details
    {
        init
        {
            foreach (var detail in value)
            {
                SetDetail(detail);
            }
        }
        get => details.AsReadOnly();
    }

    /// <summary>
    /// Gets an <see cref="WiserItemDetailModel"/> with the given key.
    /// An item detail is a field on an item in Wiser 2.0.
    /// </summary>
    /// <param name="key">The key of the item detail to get.</param>
    /// <returns>An <see cref="WiserItemDetailModel"/>.</returns>
    public WiserItemDetailModel GetDetail(string key)
    {
        return details.FirstOrDefault(detail => String.Equals(detail.Key, key, StringComparison.OrdinalIgnoreCase)) ?? new WiserItemDetailModel();
    }

    /// <summary>
    /// Gets an item value as <see cref="String"/> with the given key. This function never returns NULL, only an empty string if there is no value.
    /// </summary>
    /// <param name="key">The key of the item detail value to get.</param>
    /// <returns>A <see cref="String"/> containing the value.</returns>
    public T GetDetailValue<T>(string key)
    {
        var stringValue = GetDetail(key)?.Value?.ToString() ?? "";
        var result = GetDetail(key)?.Value;

        if (result == null)
        {
            return default(T);
        }

        if (typeof(T) == result.GetType())
        {
            return (T) result;
        }

        if (typeof(T) == typeof(string))
        {
            result = stringValue;
        }
        else if (typeof(T) == typeof(decimal))
        {
            result = String.IsNullOrWhiteSpace(stringValue) ? 0 : Convert.ToDecimal(stringValue.Replace(",", "."), new CultureInfo("en-US"));
        }
        else if (typeof(T) == typeof(bool))
        {
            if (String.IsNullOrWhiteSpace(stringValue) || stringValue.Equals("0"))
            {
                result = false;
            }
            else if (stringValue.Equals("1"))
            {
                result = true;
            }
            else
            {
                result = Convert.ToBoolean(stringValue);
            }
        }
        else
        {
            result = Convert.ChangeType(stringValue, typeof(T));
        }

        return (T) result;
    }

    /// <summary>
    /// Gets an item value as <see cref="String"/> with the given key. This function never returns NULL, only an empty string if there is no value.
    /// </summary>
    /// <param name="key">The key of the item detail value to get.</param>
    /// <returns>A <see cref="String"/> containing the value.</returns>
    public string GetDetailValue(string key)
    {
        return GetDetailValue<string>(key);
    }

    /// <summary>
    /// Check whether or not this item contains a value for the given key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>A <see cref="Boolean"/> indicating whether this item contains a non-empty value with the given key or not.</returns>
    public bool ContainsDetail(string key)
    {
        return !String.IsNullOrEmpty(GetDetailValue(key));
    }

    /// <summary>
    /// Overwrites the detail if exists, adds the detail if it does not exist.
    /// When append=true the existing value will not be overwritten, but the new value is appended to the old value.
    /// </summary>
    /// <param name="detail">The detail to set.</param>
    /// <param name="append">Optional: If set to true, this will append a newline and the new value, instead of overwriting the value. Default is false.</param>
    /// <param name="markChangedAsFalse">Optional: Whether or not to mark the Changed property to false, so that it won't be saved if you save this item without changing this value after calling this function.</param>
    /// <param name="format">Optional: Formatting for converting certain types (such as numbers and dates) to string. Default is null.</param>
    /// <param name="saveAsIs">Optional: Set to <c>true</c> to skip any conversion or hashing that the GCL might do when saving this value to the database. Such as for passwords, when you already hashed them yourself.</param>
    public void SetDetail(WiserItemDetailModel detail, bool append = false, bool markChangedAsFalse = false, string format = null, bool saveAsIs = false)
    {
        SetDetail(detail.Key, detail.Value, append, detail.ReadOnly, detail.GroupName, detail.LanguageCode, markChangedAsFalse, format, saveAsIs);
    }

    /// <summary>
    /// Overwrites the detail if exists, adds the detail if it does not exist.
    /// When append=true the existing value will not be overwritten, but the new value is appended to the old value.
    /// </summary>
    /// <param name="key">The key of the item detail to set.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="append">Optional: If set to true, this will append a newline and the new value, instead of overwriting the value. Default is false.</param>
    /// <param name="enableReadOnly">Optional: Make the new value read only. Default is false.</param>
    /// <param name="groupName">Optional: The group name of the detail. Default is null.</param>
    /// <param name="languageCode">Optional: The language code of the detail. Default is null.</param>
    /// <param name="markChangedAsFalse">Optional: Whether or not to mark the Changed property to false, so that it won't be saved if you save this item without changing this value after calling this function.</param>
    /// <param name="format">Optional: Formatting for converting certain types (such as numbers and dates) to string. Default is null.</param>
    /// <param name="saveAsIs">Optional: Set to <c>true</c> to skip any conversion or hashing that the GCL might do when saving this value to the database. Such as for passwords, when you already hashed them yourself.</param>
    public void SetDetail(string key, object value, bool append = false, bool enableReadOnly = false, string groupName = null, string languageCode = null, bool markChangedAsFalse = false, string format = null, bool saveAsIs = false)
    {
        // TODO: Add a check for read only, so that we can't use this function to update read only values?
        var detail = details.FirstOrDefault(d => String.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase) && String.Equals(d.GroupName ?? "", groupName ?? "", StringComparison.OrdinalIgnoreCase) && String.Equals(d.LanguageCode ?? "", languageCode ?? "", StringComparison.OrdinalIgnoreCase));
        if (detail == null)
        {
            // Add new item if key doesn't exist
            detail = new WiserItemDetailModel
            {
                Key = key,
                GroupName = groupName,
                LanguageCode = languageCode
            };

            details.Add(detail);
        }

        detail.ReadOnly = enableReadOnly;

        switch (value)
        {
            case string valueAsString when append && !String.IsNullOrWhiteSpace(detail.Value?.ToString()):
                detail.Value += Environment.NewLine + valueAsString;
                break;
            case string valueAsString:
                detail.Value = valueAsString;
                break;
            case decimal valueAsDecimal when !String.IsNullOrEmpty(format):
                detail.Value = valueAsDecimal.ToString(format, new CultureInfo("en-US"));
                break;
            case decimal valueAsDecimal:
                detail.Value = valueAsDecimal.ToString(new CultureInfo("en-US"));
                break;
            case double valueAsDouble when !String.IsNullOrEmpty(format):
                detail.Value = valueAsDouble.ToString(format, new CultureInfo("en-US"));
                break;
            case double valueAsDouble:
                detail.Value = valueAsDouble.ToString(new CultureInfo("en-US"));
                break;
            case DateTime valueAsDateTime:
                if (String.IsNullOrWhiteSpace(format))
                {
                    format = "yyyy-MM-dd HH:mm";
                }

                detail.Value = valueAsDateTime.ToString(format);
                break;
            default:
                if (append && !String.IsNullOrWhiteSpace(detail.Value?.ToString()))
                {
                    detail.Value += Environment.NewLine + value;
                }
                else
                {
                    detail.Value = value;
                }

                break;
        }

        if (markChangedAsFalse)
        {
            detail.Changed = false;
        }

        detail.SaveAsIs = saveAsIs;
    }
}