using System;

namespace GeeksCoreLibrary.Core.Cms.Attributes;

public class CmsAttributes
{
    public enum CmsTabName
    {
        Behavior,
        DataSource,
        Developer,
        Layout,
        Misc,
        Obsolete
    }

    public enum CmsGroupName
    {
        Basic,
        Common,
        Demo,
        Caching,
        [CmsEnum(PrettyName = "Data handling")]
        DataHandling,
        Handling,
        Wiser,
        [CmsEnum(PrettyName = "Wiser 2")]
        Wiser2,
        [CmsEnum(PrettyName = "Custom database")]
        CustomDatabase,
        [CmsEnum(PrettyName = "Custom SQL")]
        CustomSql,
        Json,
        Filesystem,
        Validation,
        [CmsEnum(PrettyName = "Selected state")]
        SelectedState,
        Seo,
        Templates,
        [CmsEnum(PrettyName = "Templates for mobile devices")]
        TemplatesForMobileDevices,
        [CmsEnum(PrettyName = "Templates level 1")]
        TemplatesLevel1,
        [CmsEnum(PrettyName = "Templates level 2")]
        TemplatesLevel2,
        [CmsEnum(PrettyName = "Templates level 3")]
        TemplatesLevel3,
        [CmsEnum(PrettyName = "Templates level 4")]
        TemplatesLevel4,
        [CmsEnum(PrettyName = "Templates level 5")]
        TemplatesLevel5,
        [CmsEnum(PrettyName = "Mail template")]
        MailTemplate,
        [CmsEnum(PrettyName = "Advanced templates")]
        AdvancedTemplates,
        [CmsEnum(PrettyName = "Custom table")]
        CustomTable,
        Columns,
        Xml,
        Advanced,
        [CmsEnum(PrettyName = "Price calculation")]
        PriceCalculation,
        Debugging,
        Misc,
        Obsolete,
        [CmsEnum(PrettyName = "Sessions / Cookies")]
        SessionCookie,
        Search
    }

    /// <summary>
    /// The types of form elements that can be used in Wiser for edititing properties of modules.
    /// </summary>
    public enum CmsTextEditorType
    {
        /// <summary>
        /// Automatically decide which type to use.
        /// </summary>
        Auto,
        /// <summary>
        /// A normal single-line text field.
        /// </summary>
        TextField,
        /// <summary>
        /// A multi-line text field (textarea).
        /// </summary>
        TextBox,
        /// <summary>
        /// A CodeMirror editor with syntax highlighting for SQL.
        /// </summary>
        QueryEditor,
        /// <summary>
        /// A CodeMirror editor with syntax highlighting for HTML.
        /// </summary>
        HtmlEditor,
        /// <summary>
        /// A CodeMirror editor with syntax highlighting for XML.
        /// </summary>
        XmlEditor,
        /// <summary>
        /// A CodeMirror editor with syntax highlighting for JavaScript.
        /// </summary>
        JsEditor,
        /// <summary>
        /// A CodeMirror editor with syntax highlighting for JSON.
        /// </summary>
        JsonEditor,
        /// <summary>
        /// A CodeMirror editor for plain text.
        /// </summary>
        TextEditor
    }
}

/// <summary>
/// This attribute is to extract information data from a CMS object to the CMS or for reflection in general.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CmsObjectAttribute : Attribute
{
    // Set default values to empty string to prevent null reference errors.
    // Setup integer with defaults

    // Setup boolean with defaults

    /// <summary>
    /// Gets or sets the property's name that Wiser will use that is easier to read.
    /// </summary>
    public string PrettyName { get; set; } = "";

    /// <summary>
    /// Gets or sets a basic description on how this property is used or how it functions.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Gets or sets technical remarks intended for the developer, for extra information about the property.
    /// </summary>
    public string DeveloperRemarks { get; set; } = "";

    /// <summary>
    /// This is used to set the display order(in the group) in the CMS (Wiser) 0 to show first >99 to show last.
    /// </summary>
    public int ObjectId { get; set; } = 0;

    /// <summary>
    /// This is used to hide this property in the CMS (Wiser)
    /// </summary>
    public bool HideInCms { get; set; } = false;
}

/// <summary>
/// This attribute is to extract information data from a CMS property to the CMS or for reflection in general.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CmsPropertyAttribute : Attribute
{
    // Set default values to empty string to prevent null reference errors.
    // Setup enums with default groups

    /// <summary>
    /// Gets or sets the property's name that Wiser will use that is easier to read.
    /// </summary>
    public string PrettyName { get; set; } = "";

    /// <summary>
    /// Gets or sets a basic description on how this property is used or how it functions.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Gets or sets technical remarks intended for the developer, for extra information about the property.
    /// </summary>
    public string DeveloperRemarks { get; set; } = "";

    /// <summary>
    /// Gets or sets the component mode this property belongs to. Leave empty if the property is available for more than one component mode.
    /// This is used in Wiser so that you can hide all fields that you don't need for the selected component mode.
    /// </summary>
    public string ComponentMode { get; set; } = "";

    /// <summary>
    /// Gets or sets the tab this property belongs to in Wiser.
    /// </summary>
    public CmsAttributes.CmsTabName TabName { get; set; } = CmsAttributes.CmsTabName.Misc;

    /// <summary>
    /// Gets or sets the group this property belongs to in Wiser.
    /// </summary>
    public CmsAttributes.CmsGroupName GroupName { get; set; } = CmsAttributes.CmsGroupName.Misc;

    /// <summary>
    /// Gets or sets which editor should be used in Wiser. This only applies to String types.
    /// </summary>
    public CmsAttributes.CmsTextEditorType TextEditorType { get; set; } = CmsAttributes.CmsTextEditorType.Auto;

    /// <summary>
    /// Gets or sets the order this item will be displayed in (within the group) in Wiser. The order is ascending.
    /// </summary>
    public int DisplayOrder { get; set; } = 999;

    /// <summary>
    /// Gets or sets whether this item is hidden from Wiser.
    /// </summary>
    public bool HideInCms { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this property is read-only in Wiser.
    /// </summary>
    public bool ReadOnlyInCms { get; set; } = false;

    /// <summary>
    /// Gets or sets the old (deprecated) JSON key this property used to use. If set, it will check that JSON property's value first.
    /// </summary>
    /// <returns></returns>
    public string OldJsonKey { get; set; } = "";
}

[AttributeUsage(AttributeTargets.Field)]
public class CmsEnumAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the pretty name for an enum's value as it will appear in Wiser.
    /// </summary>
    public string PrettyName { get; set; } = "";

    /// <summary>
    /// Gets or sets whether this item is hidden from Wiser.
    /// </summary>
    public bool HideInCms { get; set; } = false;
}