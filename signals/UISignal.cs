using Godot;
using System;

[GlobalClass]
public partial class UISignal : SimulationSignal
{
	public string Command { get; }

	public UISignal() { }
	public UISignal(string command)
	{
		Command = command;
	}
}
