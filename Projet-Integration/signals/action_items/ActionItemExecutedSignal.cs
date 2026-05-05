using Godot;

namespace Terrarium_Virtuel.signals.action_items;


/// <summary>
/// The signal that is sent when an item inside the action shelf is dragged into the simulation.
/// </summary>
[GlobalClass]
public partial class ActionItemExecutedSignal : SimulationSignal
{
	[Export] public string Id { get; set; }
	[Export] public string CategoryName { get; set; }
	
	[Export] public Vector3I Position { get; set; }
	
	public ActionItem ActionItem { get; set; }

    public ActionItemExecutedSignal(string id, string categoryName, Vector3I position, ActionItem actionItem = null)
    {
        Id = id;
        Position = position;
        ActionItem = actionItem;

        

        if (actionItem != null)
        {
            ActionItem = actionItem;
        }
    }

	public override string ToString()
	{
		return $"[ActionItemSignal: Id={Id}, CategoryName={CategoryName}, Position={Position}]";
	}
	
	public ActionItemExecutedSignal() { }
}
