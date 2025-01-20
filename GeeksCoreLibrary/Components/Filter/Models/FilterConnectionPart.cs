namespace GeeksCoreLibrary.Components.Filter.Models;

public class FilterConnectionPart(string typeName, string joinPart, bool isLinkType)
{
    public string TypeName { get; set; } = typeName;
    public string JoinPart { get; set; } = joinPart;
    public bool IsLinkType { get; set; } = isLinkType;
}