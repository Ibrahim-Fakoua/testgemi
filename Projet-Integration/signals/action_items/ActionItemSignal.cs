using Godot;

namespace Terrarium_Virtuel.signals.action_items;


/// <summary>
/// The signal that is sent when a new actionItem is created and should be added to the action shelf
/// </summary>
[GlobalClass]
public partial class ActionItemSignal : SimulationSignal
{
	[Export] public string Id { get; set; }
	[Export] public string CategoryName { get; set; }
	
	public ActionItem ActionItem { get; set; } // the ActionItem is only useful within C# so no export here

	public ActionItemSignal(string id, ActionItem actionItem = null)
	{
		Id = id;
		ActionItem = actionItem;

		if (actionItem != null)
		{
			ActionItem = actionItem;
		}
	}

	public override string ToString()
	{
		return $"[ActionItemSignal: Id={Id}, CategoryName={CategoryName}]";
	}
	
	public ActionItemSignal() { }
}
