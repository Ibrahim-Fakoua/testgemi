using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Terrarium_Virtuel.scripts.database;

public class ToolbarItems
{
    [JsonPropertyName("label")] public string Label { get; set; }
	
    [JsonPropertyName("method_name")] public string MethodName { get; set; }
	
    [JsonPropertyName("type")] public string Type { get; set; } // "normal", "checkbox", "separator", "submenus", etc.

    [JsonPropertyName("default")] public bool DefaultValue { get; set; } = false; // for checkboxes
	
    [JsonPropertyName("sub")] public List<ToolbarItems> SubMenu { get; set; }
}