using System.ComponentModel.DataAnnotations;

namespace GeeksCoreLibrary.Core.Models;

public class WiserItemDetailModel
{
    private ulong id;

    /// <summary>
    /// Gets or sets the ID of the item detail.
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

    private bool changed;
    private bool changedSetToFalseManually;

    /// <summary>
    /// Gets or sets if the detail is changed and should be saved
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

    private string languageCode;

    /// <summary>
    /// Gets or sets the language code.
    /// </summary>
    public string LanguageCode
    {
        get => languageCode;
        set
        {
            if (languageCode != value)
            {
                Changed = true;
            }

            languageCode = value;
        }
    }

    private string groupName;

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string GroupName
    {
        get => groupName;
        set
        {
            if (groupName != value)
            {
                Changed = true;
            }

            groupName = value;
        }
    }

    private string key;

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public string Key
    {
        get => key;
        set
        {
            if (key != value)
            {
                Changed = true;
            }

            key = value;
        }
    }

    private object val;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public object Value
    {
        get => val;
        set
        {
            if (val != value)
            {
                Changed = true;
            }

            val = value;
        }
    }

    private bool isLinkProperty;

    /// <summary>
    /// Gets or sets whether this is an item link detail, instead of a normal item detail.
    /// </summary>
    public bool IsLinkProperty
    {
        get => isLinkProperty;
        set
        {
            if (isLinkProperty != value)
            {
                Changed = true;
            }

            isLinkProperty = value;
        }
    }

    private ulong itemLinkId;

    /// <summary>
    /// Gets or sets the ID of the item link, if this is a link property.
    /// </summary>
    public ulong ItemLinkId
    {
        get => itemLinkId;
        set
        {
            if (itemLinkId != value)
            {
                Changed = true;
            }

            itemLinkId = value;
        }
    }

    /// <summary>
    /// Gets or sets the type of link, if this is a link property.
    /// </summary>
    public int LinkType { get; set; }

    private bool readOnly;

    /// <summary>
    /// Gets or sets whether this detail is read only.
    /// When some additional details are loaded from the database, don't save these details to the database.
    /// </summary>
    public bool ReadOnly
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

    /// <summary>
    /// Gets or sets whether this detail should be saved as is, without any modifications.
    /// This is useful for when you already hashed a password for example, then the GCL won't hash it again when saving to database.
    /// </summary>
    public bool SaveAsIs { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Key}: {Value}";
    }
}