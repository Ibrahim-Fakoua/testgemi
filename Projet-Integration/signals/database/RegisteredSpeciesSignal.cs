namespace Terrarium_Virtuel.signals.database;

/// <summary>
/// The signal emitted when a data entry with a unique ID is saved, return the SQL-based unique ID.
/// </summary>
public partial class RegisteredSpeciesSignal : SimulationSignal
{
    public int ReturnId { get; set; }
    
}