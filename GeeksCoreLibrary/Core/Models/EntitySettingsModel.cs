using System.Collections.Generic;

namespace GeeksCoreLibrary.Core.Models
{
    public class EntitySettingsModel
    {
        public string EntityType { get; set; }

        public int ModuleId { get; set; }

        public Dictionary<string, Dictionary<string, object>> FieldOptions { get; set; } = new();

        public List<(string PropertyName, string LanguageCode)> AutoIncrementFields { get; set; } = new();

        public bool SaveTitleAsSeo { get; set; }

        public string QueryAfterInsert { get; set; }

        public string QueryAfterUpdate { get; set; }

        public string DedicatedTablePrefix { get; set; }

        public bool EnableMultipleEnvironments { get; set; }

        public List<string> AcceptedChildTypes { get; set; } = new();
    }
}
