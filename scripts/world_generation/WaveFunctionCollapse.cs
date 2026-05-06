using System;
using System.Collections.Generic;
using Godot;
using Terrarium_Virtuel.signals;
using static Terrarium_Virtuel.scripts.world_generation.TileType;
using System.Numerics;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Terrarium_Virtuel.signals;
using static Terrarium_Virtuel.scripts.world_generation.TileType;

namespace Terrarium_Virtuel.scripts.world_generation;

public partial class WaveFunctionCollapse : RefCounted
{

	public (int height, int width) GridSize;
	public Cell[,] Grid;
	public volatile bool IsAllWorldGenerated = false;
	public List<int> KeyToArea = new List<int>();
	private List<Vector2> KeyToCenterOfMass = new List<Vector2>();
	public float[,] HeightMap;
	public bool shouldprop = false;
	public Cell CellToPropDebug;
	public RandomNumberGenerator Rng;
	public List<Cell> FrontierCells;
	public WaveFunctionCollapse((int height, int width) gridSize ,float[,] heightMap, ulong seed)
	{
		FrontierCells = new List<Cell>(gridSize.width * 2);
		Rng = new RandomNumberGenerator();
		Rng.Seed = seed;
		HeightMap = heightMap;
		GridSize = gridSize;

		GenerateGrid();
	}
	
	private void CollapseCell(Cell cell)
	{
		cell.PickRandomWeightedTile();


		if (cell.FrontierIndex != -1)
		{
			int indexToRemove = cell.FrontierIndex;
			int lastIndex = FrontierCells.Count - 1;
		
		
			Cell lastCell = FrontierCells[lastIndex];
		

			FrontierCells[indexToRemove] = lastCell;
			lastCell.FrontierIndex = indexToRemove; 
		

			FrontierCells.RemoveAt(lastIndex);
		

			cell.FrontierIndex = -1;
		}
	}

	public void GenerateWithAnimation(int step, MyMethodDelegate CallBack, CancellationToken cancellationToken)
	{
		if (IsAllWorldGenerated) return;

		Cell[] cells = new Cell[step];
		int stepCount = 0;

		while (stepCount < step)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				GD.Print("Generation thread terminated.");
				return;
			}

			Cell cell = GetCellWithLowestEntropy();
			if (cell == null)
			{
				IsAllWorldGenerated = true;
				break;
			}

			CollapseCell(cell);
			InitialiseCell(cell);
			PropagateRestrictions(cell);

			cells[stepCount] = cell;
			stepCount++;
		}

		if (stepCount > 0)
		{
			CallBack(cells);
		}

		if (IsAllWorldGenerated)
		{
			Console.WriteLine("Generation FINISHED.");
		}
	}


	public void InitialiseCell(Cell cell)
	{

		if (cell.CollapsedCellData.CollapsedAreaKey == -1)
		{
			KeyToArea.Add(1);
			if (cell.CollapsedCellData.CollapsedAreaKey == -1)
				if (cell.CollapsedCellData.CollapsedAreaKey == -1)
					cell.CollapsedCellData.CollapsedAreaKey = KeyToArea.Count - 1;
		}
		if (cell.CollapsedCellData.CollapsedCenterOfMassKey == -1)
		{

			KeyToCenterOfMass.Add(new Vector2(cell.CellData.Position.x, cell.CellData.Position.y));
			cell.CollapsedCellData.CollapsedCenterOfMassKey = KeyToCenterOfMass.Count - 1;
		}
	}


	

	private void GenerateGrid()
	{
		Cell[,] grid = new Cell[GridSize.width, GridSize.height];
		Cell cell;
		for (int y = 0; y < GridSize.width; y++)
		{
			for (int x = 0; x < GridSize.height; x++)
			{
				cell = new Cell((x, y), GridSize, KeyToArea, KeyToCenterOfMass, Rng);
				TileTypeExtensions.ModifyProbabilitiesCuzElevation(cell,cell.CellData, HeightMap[y, x]);
				grid[y, x] = cell;
			}
		}
		
		this.Grid = grid;

	}

	private void PropagateFromNoneCollapsedCells(Cell currentCell,Queue<Cell> cellsToUpdate  )
	{
		List<(int x, int y)> neighboursPos = Helpers.GetNeighbours((currentCell.CellData.Position.x, currentCell.CellData.Position.y), GridSize);

		foreach (var neighbourPos in neighboursPos)
		{
			Cell neighbour = Grid[neighbourPos.y, neighbourPos.x];

			if (neighbour.IsCollapsed) continue;

			int neighbourPervEntropy = neighbour.CellData.Entropy;

			neighbour.NoneCollapseRestriction(currentCell, 0);
			if (neighbour.CellData.Entropy < neighbourPervEntropy)
			{
		   
				cellsToUpdate.Enqueue(neighbour);
			}

		}
	}

	private void PropagateFromCollapsedCells(Cell currentCell, Queue<Cell> cellsToUpdate)
	{
		List<(int x, int y)> neighboursPos = Helpers.GetNeighbours((currentCell.CellData.Position.x, currentCell.CellData.Position.y), GridSize);

		foreach (var neighbourPos in neighboursPos)
		{
			Cell neighbour = Grid[neighbourPos.y, neighbourPos.x];

			if (neighbour.IsCollapsed) continue;
			if (neighbour.FrontierIndex == -1)
			{
				neighbour.FrontierIndex = FrontierCells.Count;
				FrontierCells.Add(neighbour);
			}
			int neighbourPervEntropy = neighbour.CellData.Entropy;

			neighbour.NewRestrictionFromCollapsedCell(currentCell.CollapsedCellData.CollapsedState, 0 , currentCell);
			if (neighbour.CellData.Entropy < neighbourPervEntropy)
			{
				// if ( !neighbour.IsCollapsed)
				cellsToUpdate.Enqueue(neighbour);
			}

		}
	}
	private void PropagateRestrictions( Cell startCell)
	{

		Queue<Cell> cellsToUpdate = new Queue<Cell>();
		cellsToUpdate.Enqueue(startCell);
		TileType tileRestriction;


		
		while (cellsToUpdate.Count > 0)
		{
			Cell currentCell = cellsToUpdate.Dequeue();

			if (currentCell.IsCollapsed)
			{
				
				PropagateFromCollapsedCells(currentCell, cellsToUpdate);
			}
			else
			{

				PropagateFromNoneCollapsedCells(currentCell, cellsToUpdate);
			}


		}

	}


	private Cell? GetCellWithLowestEntropy()
	{
		Cell lowestEntropyCell = null;
		float bestScore = float.MinValue;

		if (FrontierCells.Count > 0)
		{
			for (int i = 0; i < FrontierCells.Count; i++)
			{
				Cell cell = FrontierCells[i];
				if (cell.IsCollapsed) continue; 
			
				float score = cell.GetHighestTileProbability();
				if (lowestEntropyCell == null || score > bestScore)
				{
					bestScore = score;
					lowestEntropyCell = cell;
				}
			}
			return lowestEntropyCell;
		}


		int width = Grid.GetLength(0);
		int height = Grid.GetLength(1);

		for (int y = 0; y < width; y++)
		{
			for (int x = 0; x < height; x++)
			{
				Cell cell = Grid[y, x];
				if (cell.IsCollapsed) continue;
			
				float score = cell.GetHighestTileProbability();
				if (lowestEntropyCell == null || score > bestScore)
				{
					bestScore = score;
					lowestEntropyCell = cell;
				}
			}
		}

		return lowestEntropyCell;
	}
	

	private int[][][] ConvertToDisplayFormat(Cell[,] grid)
	{
		int height = grid.GetLength(0);
		int width = grid.GetLength(1);
		(int q, int r) AxialCoords;

		int[][][] displayGrid = new int[height][][];

		for (int y = 0; y < height; y++)
		{

			displayGrid[y] = new int[width][];

			for (int x = 0; x < width; x++)
			{
				var cell = grid[y, x];

				if (cell.IsCollapsed)
				{
					var atlas = cell.CollapsedCellData.CollapsedState.ToAtlasCoords();
					AxialCoords = cell.AxialCoords();
					displayGrid[y][x] = new int[] { AxialCoords.q, AxialCoords.r,x,y, atlas.X, atlas.Y, 0 };
				}
				else
				{
					throw new Exception($"Generation Error: Cell at ({x}, {y}) never collapsed!");
				}
			}
		}

		return displayGrid;
	}

}
