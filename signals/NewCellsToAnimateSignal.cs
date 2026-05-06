
using Godot.Collections; 
using Terrarium_Virtuel.scripts.world_generation;

namespace Terrarium_Virtuel.signals;

public partial class NewCellsToAnimateSignal : SimulationSignal
{
    public bool finished = false;


    public Array<Array<float>> Info = new Array<Array<float>>();

    public NewCellsToAnimateSignal(Cell[] cells)
    {
        foreach (Cell cell in cells)
        {
            if (cell != null)
            {
                var atlas = cell.CollapsedCellData.CollapsedState.ToAtlasCoords();
                

                var cellData = new Array<float> { 
                    cell.AxialCoords().x, 
                    cell.AxialCoords().y, 
                    cell.CellData.Position.x, 
                    cell.CellData.Position.y, 
                    atlas.X, 
                    atlas.Y, 
                    0 
                };

                Info.Add(cellData);
            }
        }
    }
}