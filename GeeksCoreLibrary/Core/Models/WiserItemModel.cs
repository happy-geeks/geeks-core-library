using GeeksCoreLibrary.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// The model for a Wiser 2.0 item.
/// This is the main thing of Wiser 2.0, almost everything is considered an item.
/// This is very dynamic and can be used for almost anything in Wiser 2.0.
/// </summary>
public class WiserItemModel
{
    private ulong id;

    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [Key]
    public ulong Id
    {
        get => id;
        set
        {
            if (id != value)
            {
                Changed = true;
            }

            id = value;
        }
    }

    private ulong originalItemId;

    /// <summary>
    /// Gets or sets the item ID of the item. This is used for having different data on different environments.
    /// An item can have different data on production than on test for example. If that happens, the ItemId will contain the Id of the original item.
    /// The first time an item is made, that is considered the original item. For those items, the Id and ItemId will be the same.
    /// </summary>
    [Key]
    public ulong OriginalItemId
    {
        get => originalItemId;
        set
        {
            if (originalItemId != value)
            {
                Changed = true;
            }

            originalItemId = value;
        }
    }

    private bool changed;
    private bool changedSetToFalseManually;

    /// <summary>
    /// Gets or sets if the item is changed and should be saved
    /// </summary>
    public bool Changed
    {
        get => changed || (id == 0 && !changedSetToFalseManually);
        set
        {
            changed = value;
            if (!value)
            {
                changedSetToFalseManually = true;
            }
        }
    }

    private string encryptedId;

    /// <summary>
    /// Gets or sets the encrypted ID of an item.
    /// This should be encrypted via the method StringHelpers.EncryptWithAes(), with the encryption key unique to the customer and the parameter "withdate" set to true.
    /// </summary>
    public string EncryptedId
    {
        get => encryptedId;
        set
        {
            if (encryptedId != value)
            {
                Changed = true;
            }

            encryptedId = value;
        }
    }

    private ulong parentItemId;

    /// <summary>
    /// Gets or sets the parent item ID of the item. This is only used for links that have been specifically set to use this property.
    /// Most links between items are saved in wiser_itemlink, in those cases this property is always 0.
    /// This setting can be changed via the table wiser_link.
    /// </summary>
    [Key]
    public ulong ParentItemId
    {
        get => parentItemId;
        set
        {
            if (parentItemId != value)
            {
                Changed = true;
            }

            parentItemId = value;
        }
    }

    private string uniqueUuid;

    /// <summary>
    /// Gets or sets the unique uuid, this is for saving IDs of external systems.
    /// If items are synchronized with other systems, the external ID should be saved here.
    /// </summary>
    public string UniqueUuid
    {
        get => uniqueUuid;
        set
        {
            if (uniqueUuid != value)
            {
                Changed = true;
            }

            uniqueUuid = value;
        }
    }

    private int ordering;

    /// <summary>
    /// Gets or sets the ordering of this item.
    /// </summary>
    public int Ordering
    {
        get => ordering;
        set
        {
            if (ordering != value)
            {
                Changed = true;
            }

            ordering = value;
        }
    }

    private int moduleId;

    /// <summary>
    /// Gets or sets the ID of the module this item belongs to.
    /// </summary>
    public int ModuleId
    {
        get => moduleId;
        set
        {
            if (moduleId != value)
            {
                Changed = true;
            }

            moduleId = value;
        }
    }

    private Environments? publishedEnvironment;

    /// <summary>
    /// Gets or sets the published environment of the item.
    /// This decides in which environment(s) this item should be visible (none, dev, test, acceptance and live).
    /// </summary>
    public Environments? PublishedEnvironment
    {
        get => publishedEnvironment;
        set
        {
            if (publishedEnvironment != value)
            {
                Changed = true;
            }

            publishedEnvironment = value;
        }
    }

    private bool? readOnly;

    /// <summary>
    /// Gets or sets whether this item is read only.
    /// </summary>
    public bool? ReadOnly
    {
        get => readOnly;
        set
        {
            if (readOnly != value)
            {
                Changed = true;
            }

            readOnly = value;
        }
    }

    private bool? removed;

    /// <summary>
    /// Gets or sets whether this item is removed/deleted.
    /// </summary>
    public bool? Removed
    {
        get => removed;
        set
        {
            if (removed != value)
            {
                Changed = true;
            }

            removed = value;
        }
    }

    private string title;

    /// <summary>
    /// Gets or sets the title of the item.
    /// </summary>
    public string Title
    {
        get => title;
        set
        {
            if (title != value)
            {
                Changed = true;
            }

            title = value;
        }
    }

    private DateTime addedOn;

    /// <summary>
    /// Gets or sets the date and time this item was created.
    /// </summary>
    public DateTime AddedOn
    {
        get => addedOn;
        set
        {
            if (addedOn != value)
            {
                Changed = true;
            }

            addedOn = value;
        }
    }

    private string addedBy;

    /// <summary>
    /// Gets or sets the name of the user that created this item.
    /// </summary>
    public string AddedBy
    {
        get => addedBy;
        set
        {
            if (addedBy != value)
            {
                Changed = true;
            }

            addedBy = value;
        }
    }

    private DateTime changedOn;

    /// <summary>
    /// Gets or sets the date and time of when this item has last been changed.
    /// </summary>
    public DateTime ChangedOn
    {
        get => changedOn;
        set
        {
            if (changedOn != value)
            {
                Changed = true;
            }

            changedOn = value;
        }
    }

    private string changedBy;

    /// <summary>
    /// Gets or sets the name of the user that last changed this item.
    /// </summary>
    public string ChangedBy
    {
        get => changedBy;
        set
        {
            if (changedBy != value)
            {
                Changed = true;
            }

            changedBy = value;
        }
    }

    private string entityType;

    /// <summary>
    /// Gets or sets the entity type of the item.
    /// </summary>
    public string EntityType
    {
        get => entityType;
        set
        {
            if (entityType != value)
            {
                Changed = true;
            }

            entityType = value;
        }
    }

    private string json;

    /// <summary>
    /// Gets or sets the JSON of the item. Used for document store and hybrid mode.
    /// </summary>
    [JsonIgnore]
    public string Json
    {
        get => json;
        set
        {
            if (json != value)
            {
                Changed = true;
            }

            json = value;
        }
    }

    private DateTime? jsonLastProcessedDate;

    /// <summary>
    /// Gets or sets the date and time of when the JSON was last processed to the details. Used for hybrid mode.
    /// </summary>
    public DateTime? JsonLastProcessedDate
    {
        get => jsonLastProcessedDate;
        set
        {
            if (jsonLastProcessedDate != value)
            {
                Changed = true;
            }

            jsonLastProcessedDate = value;
        }
    }

    /// <summary>
    /// Gets or sets the link ID when the item has been created. This is only used internally during the creation phase and is therefore ignored in the JSON.
    /// </summary>
    [JsonIgnore]
    public long NewLinkId { get; set; }

    /// <summary>
    /// Gets or sets the item details.
    /// </summary>
    public List<WiserItemDetailModel> Details { get; set; } = [];

    /// <summary>
    /// Gets an <see cref="WiserItemDetailModel"/> with the given key.
    /// An item detail is a field on an item in Wiser 2.0.
    /// </summary>
    /// <param name="key">The key of the item detail to get.</param>
    /// <returns>An <see cref="WiserItemDetailModel"/>.</returns>
    public WiserItemDetailModel GetDetail(string key)
    {
        return Details.FirstOrDefault(detail => String.Equals(detail.Key, key, StringComparison.OrdinalIgnoreCase)) ?? new WiserItemDetailModel();
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
        var detail = Details.FirstOrDefault(d => String.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase) && String.Equals(d.GroupName ?? "", groupName ?? "", StringComparison.OrdinalIgnoreCase) && String.Equals(d.LanguageCode ?? "", languageCode ?? "", StringComparison.OrdinalIgnoreCase));
        if (detail == null)
        {
            // Add new item if key doesn't exist
            detail = new WiserItemDetailModel
            {
                Key = key,
                GroupName = groupName,
                LanguageCode = languageCode
            };

            Details.Add(detail);
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

    /// <summary>
    /// Returns a <see cref="SortedList{TKey,TValue}"/> with all item details and values.
    /// This function can be used to replace an item in a template.
    /// This will ignore group names and language codes, it'll only use keys and values.
    /// </summary>
    /// <param name="withReadOnlyDetails">Optional: Set to true to include read only values. If set to false, they will be skipped. Default is false.</param>
    /// <returns>A <see cref="SortedList{TKey,TValue}"/> with all item details and values.</returns>
    public SortedList<string, string> GetSortedList(bool withReadOnlyDetails = false)
    {
        var output = new SortedList<string, string>
        {
            {"id", Id.ToString()},
            {"unique_uuid", UniqueUuid ?? ""},
            {"entity_type", EntityType},
            {"moduleid", ModuleId.ToString()},
            {"published_environment", ((int?) PublishedEnvironment ?? 4).ToString()},
            {"readonly", ReadOnly == true ? "1" : "0"},
            {"removed", Removed == true ? "1" : "0"},
            {"ordering", Ordering.ToString()}
        };

        if (Title != null)
        {
            output.Add("title", Title);
        }

        output.Add("added_on", AddedOn.ToString("yyyy-MM-dd HH:mm:ss"));
        if (AddedBy != null)
        {
            output.Add("added_by", AddedBy);
        }

        output.Add("changed_on", ChangedOn.ToString("yyyy-MM-dd HH:mm:ss"));
        if (ChangedBy != null)
        {
            output.Add("changed_by", ChangedBy);
        }

        foreach (var detail in Details.Where(detail => !String.IsNullOrWhiteSpace(detail.Key) && (!output.ContainsKey(detail.Key) || String.IsNullOrWhiteSpace(output[detail.Key])) && (!detail.ReadOnly || withReadOnlyDetails)).ToList())
        {
            if (detail.Value != null)
            {
                output[detail.Key] = detail.Value.ToString();
            }
            else
            {
                output[detail.Key] = "";
            }
        }

        return output;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Id} - {Title} ({EntityType})";
    }
}