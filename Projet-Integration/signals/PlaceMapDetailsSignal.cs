using System.Collections.Generic;
using Godot;
using Godot.Collections; 
using Terrarium_Virtuel.scripts.world_generation;

namespace Terrarium_Virtuel.signals;

public partial class PlaceMapDetailsSignal : SimulationSignal
{

	public Array<Vector3I> Data { get; set; } = new();

	public PlaceMapDetailsSignal(List<(int x, int y, FoodType food)> rawData)
	{
		foreach (var item in rawData)
		{
		 
			var entry = new Vector3I(item.x,item.y, (int) item.food);

			Data.Add(entry);
		}
	}
}
