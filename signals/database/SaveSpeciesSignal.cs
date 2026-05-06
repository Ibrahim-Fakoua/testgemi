namespace Terrarium_Virtuel.signals.database;


/// <summary>
/// This is the signal emitted by a creature whenever it is registered to the SQL DB.
/// </summary>
public partial class SaveSpeciesSignal : SimulationSignal
{
    public int SpeciesParentId { get; set; }
    public int BirthTick { get; set; }
    // the value that is sent by the new species to 'identify' and not receive an ID for a different species born simultaneously
    // basically, a temporary ID that is used for returning the SQL-based unique ID
    public int ReturnId { get; set; } 

    public SaveSpeciesSignal(int speciesId, int birthTick, int returnId)
    {
        SpeciesParentId = speciesId;
        BirthTick = birthTick;
        ReturnId = returnId;
    }
    public SaveSpeciesSignal() { }
}