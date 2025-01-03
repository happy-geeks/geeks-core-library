using System;
using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Databases.Models
{
    /// <summary>
    /// A model for creating a definition for Wiser tables.
    /// This will be used to keep them up-to-date automatically for all customers.
    /// </summary>
    public class WiserTableDefinitionModel
    {
        /// <summary>
        /// Gets or sets the name of the database table.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets all columns for this table.
        /// </summary>
        public List<ColumnSettingsModel> Columns { get; set; } = [];

        /// <summary>
        /// Gets or sets all index for this table.
        /// </summary>
        public List<IndexSettingsModel> Indexes { get; set; } = [];

        /// <summary>
        /// Gets or sets the date that this table was updated last.
        /// This property should be changed every time someone changes the definition of a Wiser table.
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// Gets or sets the character set for the table.
        /// </summary>
        public string CharacterSet { get; set; } = "utf8mb4";

        /// <summary>
        /// Gets or sets the collation for the table.
        /// </summary>
        public string Collation { get; set; } = "utf8mb4_general_ci";
    }
}
