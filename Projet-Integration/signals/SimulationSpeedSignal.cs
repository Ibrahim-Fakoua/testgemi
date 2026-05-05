using Godot;
using System;

/// <summary>
/// Signal emitted to toggle the pause state of the simulation. Simply receiving it toggles the gameloop.
/// </summary>
[GlobalClass]
public partial class SimulationSpeedSignal : SimulationSignal
{
	public int Speed { get; set; }
	
	// required for GDScript to instantiate easily, apparently
	public SimulationSpeedSignal(int speed)
	{
		Speed = speed;
	}
	
	
}
