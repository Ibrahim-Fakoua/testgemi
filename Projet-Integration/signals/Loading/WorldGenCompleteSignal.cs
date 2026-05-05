using Godot;

namespace Terrarium_Virtuel.signals;

/// <summary>
/// A class meant to be used when a task is complete.
/// </summary>
[GlobalClass]
public partial class WorldGenCompleteSignal : SimulationSignal
{
	/// <summary>
	/// Parameter-less constructor for Godot's engine
	/// </summary>
	public WorldGenCompleteSignal() { }
}
