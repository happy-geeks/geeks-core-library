using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.DataSelector.Models
{
    public class ItemsRequest
    {
        public DataSelector Selector { get; set; }

        public string ModuleId { get; set; }

        public int NumberOfLevels { get; set; }

        public bool Descendants { get; set; }

        public string LanguageCode { get; set; }

        public string NumberOfItems { get; set; }

        public int PageNumber { get; set; }

        public string ContainsPath { get; set; }

        public string ContainsUrl { get; set; }

        public string ParentId { get; set; }

        public string EntityTypes { get; set; }

        public string LinkType { get; set; }

        public string QueryAddition { get; set; }

        public string OrderPart { get; set; }

        public int Environment { get; set; }

        /// <summary>
        /// Comma separated string of filetypes.
        /// </summary>
        [JsonProperty("filetypes")]
        public string GetFileTypes { get; set; }

        #region Properties for internal use

        /// <summary>
        /// Gets or sets query retrieved from Wiser.
        /// </summary>
        internal string Query { get; set; }

        internal List<string> LinkTables { get; } = new();

        internal string AutoSortOrder { get; set; }

        internal List<Field> FieldsInternal { get; } = new();

        internal List<string> JoinLink { get; } = new();

        internal List<Field> JoinDetail { get; } = new();

        internal List<string> WhereLink { get; } = new();

        internal List<string> FileTypes { get; } = new();

        internal Dictionary<string, string> DedicatedTables { get; } = new();

        internal List<LinkTypeSettings> LinkTypeSettings { get; } = new();

        #endregion
    }
}
