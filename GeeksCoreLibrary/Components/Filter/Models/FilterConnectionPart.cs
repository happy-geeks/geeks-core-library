namespace GeeksCoreLibrary.Components.Filter.Models
{
    public class FilterConnectionPart
    {
        public FilterConnectionPart(string typeName, string joinPart, bool isLinkType)
        {
            TypeName = typeName;
            JoinPart = joinPart;
            IsLinkType = isLinkType;
        }

        public string TypeName { get; set; } 
        public string JoinPart { get; set; } 
        public bool IsLinkType { get; set; }
    }
}
