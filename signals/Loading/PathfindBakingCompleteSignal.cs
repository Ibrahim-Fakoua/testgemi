using Godot;

namespace Terrarium_Virtuel.signals;

/// <summary>
/// A signal emitted when the pathfinding is done baking
/// </summary>
[GlobalClass]
public partial class PathfindBakingCompleteSignal : SimulationSignal
{
    /// <summary>
    /// Parameter-less constructor for Godot's engine
    /// </summary>
    public PathfindBakingCompleteSignal() { }
}