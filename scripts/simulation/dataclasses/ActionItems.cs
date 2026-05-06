using Godot;
using System;
using System.Text.Json.Serialization;
using Terrarium_Virtuel.signals.action_items;

/// <summary>
/// The interface used for the items inside the ActionShelf itself.
/// Not to be confused with the signal sent to the Scheduler.
/// </summary>
public interface IActionItem
{
	string ID { get;  } // unique-enough identifier for the action
	string Label { get; } // Main label text
	
	string Category { get; set; } // mostly for the action shelf
	string TooltipText { get; } // Text for tooltip or something
	void ExecuteAction(Vector3I clickPosition);
}

// Inheriting from RefCounted allows this C# class to be stored in 
// Godot.Collections (GArray/GDictionary) so your GDScript teammates can see it.
public partial class ActionItem : RefCounted, IActionItem
{
	[JsonPropertyName("id")]
	public string ID { get; }
	
	public string Category { get; set; }
	
	[JsonPropertyName("label")]
	public string Label { get; }
	
	[JsonPropertyName("tooltip_text")]
	public string TooltipText { get; }
	
	
	public void ExecuteAction(Vector3I clickPosition) // the specific method called when the item is clicked or dragged, whatever.
	{
		// GD.Print($"[ActionItem --- Execute Action] ID : {ID},  DisplayName : {Label}, POS: {clickPosition}, {Category}");
		var signal = new ActionItemExecutedSignal(ID, Category, clickPosition);
		signal.CategoryName = Category;
		Controller.Instance.Emit(signal);
	}   

	public ActionItem(string id, string label, string tooltipText, string category = "Miscellaneous")
	{
		ID = id;
		Label = label;
		TooltipText = tooltipText;
		Category = category;
	}

	public override string ToString()
	{
		return $"ActionItem (ID: {ID}, Category: {Category}, DisplayName: {Label})";
	}
}
