using System;
using System.Collections.Generic;
using System.Linq;

namespace GeeksCoreLibrary.Modules.DataSelector.Models;

public class Field
{
    private static readonly string[] ReservedFieldNames = ["id", "idencrypted", "itemtitle", "parentitemtitle", "moduleid", "changed_on", "changed_by", "unique_uuid", "item_ordering", "link_ordering"];

    /// <summary>
    /// Gets or sets the link type number for this field. This is needed to determine whether the item this field belongs to uses parent ID.
    /// </summary>
    public string LinkTypeNumber { get; set; }

    public string FieldName { get; set; }

    public string FieldAlias
    {
        get => String.IsNullOrWhiteSpace(fieldAlias) ? FieldName : fieldAlias;
        set => fieldAlias = value;
    }

    /// <summary>
    /// Gets or sets multiple language codes, in case the field needs to be selected for multiple languages.
    /// </summary>
    public IList<string> LanguageCodes { get; set; }

    /// <summary>
    /// Gets or sets the data type for a scope row, which is either "string", "decimal" or "datetime". If the value is null or empty, the value "string" is assumed.
    /// </summary>
    public string DataType { get; set; }

    /// <summary>
    /// Gets or sets the data type for a having row, which is either "string" or "formula". If the value is null or empty, the value "string" is assumed.
    /// </summary>
    public string HavingDataType { get; set; }

    public string Formatting { get; set; }

    public string AggregationFunction { get; set; }

    public Field[] Fields { get; set; }

    /// <summary>
    /// Internal properties. Not used for scoperow keys.
    /// </summary>
    internal string TableAliasPrefix { get; set; }

    internal string SelectAliasPrefix { get; set; }

    public bool IsLinkField { get; set; }

    public bool FieldFromField { get; set; }

    private string joinOn;
    private string fieldAlias;

    public string JoinOn
    {
        get => joinOn;
        set
        {
            if (!ReservedFieldNames.Contains(FieldName) || FieldFromField)
            {
                joinOn = value;
            }
        }
    }

    public string TableAlias => $"{TableAliasPrefix}{FieldName}{(LanguageCodes != null && LanguageCodes.Any() ? $"_{LanguageCodes.First()}" : "")}";

    /// <summary>
    /// For internal use, for correctly grouping of the output JSON.
    /// </summary>
    internal string SelectAlias
    {
        get
        {
            if (IsLinkField)
            {
                return $"{SelectAliasPrefix}linkfields|{FieldAlias}";
            }

            return SelectAliasPrefix + FieldAlias;
        }
    }

    public bool IsReservedFieldName => ReservedFieldNames.Contains(FieldName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the prefix for the dedicated table, if this fields is from an entity type that uses a dedicated table.
    /// </summary>
    public string DedicatedTablePrefix { get; set; }
}