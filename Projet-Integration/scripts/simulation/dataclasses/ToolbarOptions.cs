using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// The class that is used for constructing the toolbar at the top of the screen.
/// The following properties are used to make parsing the json possible
/// </summary>
public class ToolbarOption
{

	[JsonPropertyName("label")] public string Label { get; set; }
	
	[JsonPropertyName("method_name")] public string MethodName { get; set; }
	
	[JsonPropertyName("type")] public string Type { get; set; } // "normal", "checkbox", "separator", "submenus", etc.

	[JsonPropertyName("default")] public bool DefaultValue { get; set; } = false; // for checkboxes
	
	[JsonPropertyName("sub")] public List<ToolbarOption> SubMenu { get; set; }
	
}
