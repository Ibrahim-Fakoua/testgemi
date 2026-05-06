using Godot;
using System;

/// <summary>
/// Placeholder signal for logging events in the database / in a side log. It should technically only have one or two subscriber.
/// </summary>

[GlobalClass]
public partial class LogMessageSignal : SimulationSignal
{
	
	public string Message { get; set; }
	public int Severity { get; set; } // Level 0 is bad, level 8 is just a scratch (remains to be seen)

	// required for GDScript to instantiate easily
	public LogMessageSignal() { }
}
