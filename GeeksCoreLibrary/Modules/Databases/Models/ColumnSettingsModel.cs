using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Databases.Enums;
using MySql.Data.MySqlClient;

namespace GeeksCoreLibrary.Modules.Databases.Models
{
    /// <summary>
    /// A model for settings for creating or updating a MySQL database column.
    /// </summary>
    public class ColumnSettingsModel
    {
        public ColumnSettingsModel()
        {
        }

        public ColumnSettingsModel(string name, MySqlDbType type, int length = 0, int decimals = 2, string defaultValue = null, bool notNull = false, bool addIndex = false, IndexTypes indexType = IndexTypes.Normal, bool autoIncrement = false, IList<string> enumValues = null, string comment = null, string addAfterColumnName = null, bool updateTimeStampOnChange = false, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", bool isPrimaryKey = false)
        {
            Name = name;
            Type = type;
            Length = length;
            Decimals = decimals;
            DefaultValue = defaultValue;
            NotNull = notNull;
            AddIndex = addIndex;
            IndexType = indexType;
            AutoIncrement = autoIncrement;
            EnumValues = enumValues;
            Comment = comment;
            AddAfterColumnName = addAfterColumnName;
            UpdateTimeStampOnChange = updateTimeStampOnChange;
            CharacterSet = characterSet;
            Collation = collation;
            IsPrimaryKey = isPrimaryKey;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        public MySqlDbType Type { get; set; }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the amount of decimals. Default is 2.
        /// </summary>
        public int Decimals { get; set; } = 2;

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets whether this column can not be <see langword="null"/>.
        /// </summary>
        public bool NotNull { get; set; }

        /// <summary>
        /// Gets or sets whether an index should be added for this column.
        /// </summary>
        public bool AddIndex { get; set; }

        /// <summary>
        /// Gets or sets the index type to be added if <see cref="AddIndex"/> is <see langword="true" />.
        /// </summary>
        public IndexTypes IndexType { get; set; } = IndexTypes.Normal;

        /// <summary>
        /// Gets or sets whether this should be an auto increment column.
        /// </summary>
        public bool AutoIncrement { get; set; }

        /// <summary>
        /// Gets or sets the enum values, for if the column type is enum.
        /// </summary>
        public IList<string> EnumValues { get; set; }

        /// <summary>
        /// Gets or sets a comment/description that can be seen in the database.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the column name this column should be added behind. Leave empty to just add it at the end.
        /// </summary>
        public string AddAfterColumnName { get; set; }

        /// <summary>
        /// Gets or sets whether to always update the value of this column with the current timestamp, whenever something in this table gets changes.
        /// Only applicable for timestamp and datetime columns.
        /// </summary>
        public bool UpdateTimeStampOnChange { get; set; }

        /// <summary>
        /// Gets or sets the character set for text/string columns.
        /// </summary>
        public string CharacterSet { get; set; } = "utf8mb4";

        /// <summary>
        /// Gets or sets the collation for text/string columns.
        /// </summary>
        public string Collation { get; set; } = "utf8mb4_general_ci";

        /// <summary>
        /// Gets or sets whether this column should be part of the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets whether this column is a virtual column.
        /// </summary>
        public bool IsVirtual { get; set; }

        /// <summary>
        /// Gets or sets the type of virtual column.
        /// This is only used if <see cref="IsVirtual"/> is set to <see langword="true"/>.
        /// </summary>
        public VirtualTypes VirtualType { get; set; } = VirtualTypes.Virtual;

        /// <summary>
        /// Gets or sets the expression for a virtual column.
        /// This is only used if <see cref="IsVirtual"/> is set to <see langword="true"/>.
        /// </summary>
        public string VirtualExpression { get; set; }
    }
}
