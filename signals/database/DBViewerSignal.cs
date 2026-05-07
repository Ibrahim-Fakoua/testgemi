using Godot;

namespace Terrarium_Virtuel.signals.database;

public partial class DBViewerSignal : SimulationSignal
{
    public string Command { get; }

    public DBViewerSignal(string command)
    {
        Command = command;
    }
}