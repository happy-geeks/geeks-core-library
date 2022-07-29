namespace GeeksCoreLibrary.Modules.DataSelector.Models
{
    public class LinkTypeSettings
    {
        public int Type { get; set; }

        public string DestinationEntityType { get; set; }

        public string SourceEntityType { get; set; }

        public bool UseParentItemId { get; set; }

        public string DedicatedTablePrefix { get; set; }
    }
}
