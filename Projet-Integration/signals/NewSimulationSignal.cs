using Godot;
namespace Terrarium_Virtuel.signals;

[GlobalClass]
public partial class NewSimulationSignal : SimulationSignal
{
	public int Seed { get; set; }
	
	public NewSimulationSignal()
	{
		
	}
}
