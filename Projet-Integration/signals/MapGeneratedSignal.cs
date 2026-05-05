using Godot;
using Godot.Collections; // Important: Use Godot's Collections
namespace Terrarium_Virtuel.scripts.world_generation;


public partial class MapGeneratedSignal  : SimulationSignal
{
	// Use Godot.Collections.Array so GDScript can read it
	public Array MapData { get; set; } 

	public MapGeneratedSignal(int[][][] data)
	{
		MapData = new Godot.Collections.Array();

		foreach (int[][] row in data)
		{
			var rowArray = new Godot.Collections.Array();
		
			foreach (int[] cell in row)
			{
				var cellArray = new Godot.Collections.Array();
				foreach (int val in cell) 
				{
					cellArray.Add(val); // Manually adding handles the int -> Variant conversion
				}
				rowArray.Add(cellArray);
			}
		
			MapData.Add(rowArray);
		}
	}
}
