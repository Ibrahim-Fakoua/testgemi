using Godot;
using System;

/// <summary>
/// Signal emitted to toggle the pause state of the simulation. Simply receiving it toggles the gameloop.
/// </summary>
[GlobalClass]
public partial class PauseGameSignal : SimulationSignal
{
	// required for GDScript to instantiate easily, apparently
	public PauseGameSignal() { }
}
