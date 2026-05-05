using Godot;
using System;


/// <summary>
/// Testing signal, mostly used to verify GDScript-C# integration.
/// </summary>
[GlobalClass]
public partial class TestSignal : SimulationSignal
{
	[Export] public string Message { get; set; }
	
	public TestSignal() { }
}
