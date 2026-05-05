using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
public partial class MenuOption
{
	/// <summary>
	/// The unique identifier attribute
	/// </summary>
	[JsonPropertyName("id")]
	public string ID { get; set; }
	
	/// <summary>
	/// The attribute describing the text associated with the input (type) 
	/// </summary>
	[JsonPropertyName("label")]
	public string Label { get; set; }
	
	/// <summary>
	/// The attribute associated with a targeted class of object
	/// Eg., 'slider', 'input', 'text', and more
	/// </summary>
	[JsonPropertyName("type")]
	public string Type { get; set; }
	
	/// <summary>
	/// The attribute associated with the default value of an input widget (defined by type)
	/// </summary>
	[JsonPropertyName("default")]
	public object DefaultValue { get; set; }
	
	/// <summary>
	/// The attribute associated with a minimum value an input widget can hold
	/// </summary>
	[JsonPropertyName("min")]
	public float Min { get; set; } = 0;
	
	/// <summary>
	/// The attribute associated with a maximum value an input widget can hold
	/// </summary>
	[JsonPropertyName("max")]
	public float Max { get; set; } = 100;

	/// <summary>
	/// The attribute associated with the value an input widget can step
	/// </summary>
	[JsonPropertyName("step")]
	public float Step { get; set; } = 1.0f;
	
	/// <summary>
	/// The attribute associated with the tooltip text an input widget can hold
	/// </summary>
	[JsonPropertyName("tooltip")]
	public string Tooltip { get; set; }
	

	/// <summary>
	/// The attribute associated with the possible options an input widget with multiple options
	/// </summary>
	[JsonPropertyName("options")] 
	public List<object> Options { get; set; } = new();

}

public enum SettingsWidgetType
{
	Unknown,
	Toggle,
	Slider,
	Spinbox,
	LineEdit,
	Dropdown
	// possibly more as time goes on.
}
