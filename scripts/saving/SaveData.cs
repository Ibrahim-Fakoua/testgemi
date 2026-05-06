using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

namespace Terrarium_Virtuel.scripts.saving;

public class SaveData
{
    [JsonPropertyName("seed")]
    public int Seed { get; set; }
    
    [JsonPropertyName("tick")]
    public int Tick { get; set; }
    
    [JsonPropertyName("entities")]
    public List<Entity> Entities { get; set; }

    public SaveData(int seed, int tick)
    {
        Seed = seed;
        Tick = tick;
        Entities = new List<Entity>();
    }

    public SaveData()
    {
        Entities = new List<Entity>();
    }
}

public class Entity
{
    [JsonPropertyName("species")]
    public string Species { get; set; }
    
    [JsonPropertyName("position")]
    public string Position { get; set; }
    
    [JsonPropertyName("category")]
    public string Category { get; set; }

}