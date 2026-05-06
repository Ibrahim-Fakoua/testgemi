using Godot;
using System;

[GlobalClass]
public partial class SimulationStateSignal : SimulationSignal
{
	public bool IsPaused { get; }
	public int SimulationSpeed { get; }
	public long TicksPassed { get; }
	public int EntityCount { get; }
	
	public SimulationStateSignal() { }
	
	public SimulationStateSignal(bool isPaused,  int simulationSpeed, long ticksPassed, int entityCount = 0)
	{
		IsPaused = isPaused;
		SimulationSpeed = simulationSpeed;
		TicksPassed = ticksPassed;
		EntityCount = entityCount;
	}
}
