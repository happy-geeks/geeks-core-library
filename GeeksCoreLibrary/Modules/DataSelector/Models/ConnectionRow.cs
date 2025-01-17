using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.DataSelector.Models;

public class ConnectionRow
{
    public string Name { get; set; }

    public string[] Modes { get; set; }

    [JsonProperty("entity")]
    public string EntityName { get; set; }

    [JsonProperty("typenr")]
    public int TypeNumber { get; set; }

    public ulong[] ItemIds { get; set; }

    public Field[] Fields { get; set; }

    public Field[] LinkFields { get; set; }

    [JsonProperty("scope")]
    public Scope[] Scopes { get; set; }

    public Connection[] Connections { get; set; }
}