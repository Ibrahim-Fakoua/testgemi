namespace Terrarium_Virtuel.signals.database;

public partial class SimulationRegisteredSignal : SimulationSignal
{
    public int SimulationId { get; private set; }

    public SimulationRegisteredSignal(int id)
    {
        SimulationId = id;
    }
    
    public SimulationRegisteredSignal() { }
}