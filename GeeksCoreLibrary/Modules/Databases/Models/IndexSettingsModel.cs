using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Databases.Enums;

namespace GeeksCoreLibrary.Modules.Databases.Models
{
    public class IndexSettingsModel
    {
        /// <summary>
        /// Gets or sets the name of the index.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of index. Default value is <see cref="IndexTypes.Normal"/>.
        /// </summary>
        public IndexTypes Type { get; set; } = IndexTypes.Normal;

        /// <summary>
        /// The name of the table that this index belongs to.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the fields that are part of this index.
        /// </summary>
        public List<string> Fields { get; set; } = new();
    }
}
