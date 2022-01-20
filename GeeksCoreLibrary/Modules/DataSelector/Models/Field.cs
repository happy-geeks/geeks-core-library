using System;
using System.Linq;

namespace GeeksCoreLibrary.Modules.DataSelector.Models
{
    public class Field
    {
        private static readonly string[] ReservedFieldNames = { "id", "idencrypted", "itemtitle", "parentitemtitle", "moduleid", "changed_on", "changed_by", "unique_uuid" };

        public string FieldName { get; set; }

        public string FieldAlias { get; set; }

        private string languageCode;
        public string LanguageCode
        {
            get => (languageCode ?? "").Replace("{languagecode}", "");
            set => languageCode = value;
        }

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

        public string TableAlias => $"{TableAliasPrefix}{FieldName}{(!String.IsNullOrWhiteSpace(LanguageCode) ? "_" + LanguageCode : "")}";

        /// <summary>
        /// For internal use, for correctly grouping of the output JSON.
        /// </summary>
        internal string SelectAlias
        {
            get
            {
                if (String.IsNullOrWhiteSpace(FieldAlias))
                {
                    FieldAlias = FieldName;
                }

                if (IsLinkField)
                {
                    return $"{SelectAliasPrefix}linkfields|{FieldAlias}";
                }

                return SelectAliasPrefix + FieldAlias;
            }
        }

        public bool IsReservedFieldName => ReservedFieldNames.Contains(FieldName, StringComparer.OrdinalIgnoreCase);
    }
}
