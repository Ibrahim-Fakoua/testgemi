using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Terrarium_Virtuel.signals;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot.Collections;
using Terrarium_Virtuel.signals.action_items;

namespace Terrarium_Virtuel.scripts.world_generation;
public delegate void MyMethodDelegate(Cell[] cells);

public partial class GenerationManager : Node
{
	private bool AlreadySentSignal = false;
	public static bool isItDebugMode = false;
	private int _generationSpeed = 100; 
	private volatile bool _isGenerating = false; 
	private volatile bool _isPaused = false;     
	private volatile bool _singleStepRequested = false;

	private HeightNoiseMap _heightNoiseMap;
	private WaveFunctionCollapse _wfc;
	private (int height, int width) GridSize;
	private ulong _seed;
	public RandomNumberGenerator NoiseRng;
	public TileMapLayer WorldTileMap;

	private ConcurrentQueue<Cell[]> _animationQueue = new ConcurrentQueue<Cell[]>();
	private CancellationTokenSource _generationCts;

	private List<(int x, int y, FoodType food)> _pendingFoods = null;

	[ExportGroup("Tile Probabilities")]
	[Export] public float GrassProb = 10f;
	[Export] public float SandProb = 8f;
	[Export] public float WaterProb = 10f;
	[Export] public float SnowProb = 24f;
	[Export] public float MountainProb = 0f;
	[Export] public float SnowSmallTreesProb = 0f;
	[Export] public float SnowBigTreesProb = 0f;
	
	[ExportGroup("Elevation Profiles (X = Peak, Y = Spread)")]
	[Export] public Vector2 GrassElevation = new Vector2(0.4f, 0.2f);
	[Export] public Vector2 SandElevation = new Vector2(0.3f, 0.1f);
	[Export] public Vector2 WaterElevation = new Vector2(0.1f, 0.15f);
	[Export] public Vector2 SnowElevation = new Vector2(0.45f, 0.15f);
	[Export] public Vector2 MountainElevation = new Vector2(0.85f, 0.15f);
	[Export] public Vector2 SnowSmallTreesElevation = new Vector2(0.65f, 0.09f);
	[Export] public Vector2 SnowBigTreesElevation = new Vector2(0.72f, 0.08f);

	[ExportGroup("Center of Mass (X = Radius, Y = Exponent, Z = Multiplier)")]
	[Export] public Vector3 GrassCoM = new Vector3(51f, 0.055f, 2.3f);
	[Export] public Vector3 SandCoM = new Vector3(51f, 0.055f, 2.3f);
	[Export] public Vector3 WaterCoM = new Vector3(51f, 0.055f, 2.3f);
	[Export] public Vector3 SnowCoM = new Vector3(51f, 0.055f, 2.3f);
	[Export] public Vector3 MountainCoM = new Vector3(51f, 0.055f, 2.3f);
	[Export] public Vector3 SnowSmallTreesCoM = new Vector3(51f, 0.055f, 2.3f);
	[Export] public Vector3 SnowBigTreesCoM = new Vector3(51f, 0.055f, 2.3f);
	
	public void StartGeneration(int x , int y, TileMapLayer worldTileMap,int seed_to_load)
	{
		WorldTileMap = worldTileMap;
		NoiseRng = new RandomNumberGenerator();
		_seed = Convert.ToUInt64(seed_to_load);
		NoiseRng.Seed = _seed;
		GridSize = (50, 50); 
		_heightNoiseMap = new HeightNoiseMap(GridSize, NoiseRng);
		_heightNoiseMap.Update();
		
		UpdateGenerationParameters();
		
		_wfc = new WaveFunctionCollapse(GridSize, _heightNoiseMap.HeightMap, _seed);
		
		_isGenerating = true;
		_isPaused = false; // Start unpaused
		StartGenerationLoop();
	}
private void UpdateGenerationParameters()
	{
		// 1. Update Probabilities
		TileTypeExtensions.Probabilities[TileType.Grass] = GrassProb;
		TileTypeExtensions.Probabilities[TileType.Sand] = SandProb;
		TileTypeExtensions.Probabilities[TileType.Water] = WaterProb;
		TileTypeExtensions.Probabilities[TileType.Snow] = SnowProb;
		TileTypeExtensions.Probabilities[TileType.Mountain] = MountainProb;
		TileTypeExtensions.Probabilities[TileType.SnowSmallTrees] = SnowSmallTreesProb;
		TileTypeExtensions.Probabilities[TileType.SnowBigTrees] = SnowBigTreesProb;

		// 2. Update Elevation Profiles (Mapping Vector2.X to Peak, Vector2.Y to Spread)
		TileTypeExtensions.ElevationProfiles[TileType.Grass] = (GrassElevation.X, GrassElevation.Y);
		TileTypeExtensions.ElevationProfiles[TileType.Sand] = (SandElevation.X, SandElevation.Y);
		TileTypeExtensions.ElevationProfiles[TileType.Water] = (WaterElevation.X, WaterElevation.Y);
		TileTypeExtensions.ElevationProfiles[TileType.Snow] = (SnowElevation.X, SnowElevation.Y);
		TileTypeExtensions.ElevationProfiles[TileType.Mountain] = (MountainElevation.X, MountainElevation.Y);
		TileTypeExtensions.ElevationProfiles[TileType.SnowSmallTrees] = (SnowSmallTreesElevation.X, SnowSmallTreesElevation.Y);
		TileTypeExtensions.ElevationProfiles[TileType.SnowBigTrees] = (SnowBigTreesElevation.X, SnowBigTreesElevation.Y);

		// 3. Update Center of Mass (Mapping Vector3 X, Y, Z to Radius, Exponent, Multiplier)
		TileTypeExtensions.CenterOfMassProfiles[TileType.Grass] = (GrassCoM.X, GrassCoM.Y, GrassCoM.Z);
		TileTypeExtensions.CenterOfMassProfiles[TileType.Sand] = (SandCoM.X, SandCoM.Y, SandCoM.Z);
		TileTypeExtensions.CenterOfMassProfiles[TileType.Water] = (WaterCoM.X, WaterCoM.Y, WaterCoM.Z);
		TileTypeExtensions.CenterOfMassProfiles[TileType.Snow] = (SnowCoM.X, SnowCoM.Y, SnowCoM.Z);
		TileTypeExtensions.CenterOfMassProfiles[TileType.Mountain] = (MountainCoM.X, MountainCoM.Y, MountainCoM.Z);
		TileTypeExtensions.CenterOfMassProfiles[TileType.SnowSmallTrees] = (SnowSmallTreesCoM.X, SnowSmallTreesCoM.Y, SnowSmallTreesCoM.Z);
		TileTypeExtensions.CenterOfMassProfiles[TileType.SnowBigTrees] = (SnowBigTreesCoM.X, SnowBigTreesCoM.Y, SnowBigTreesCoM.Z);
	}
	public override void _Process(double delta)
	{
		
		if (_wfc.IsAllWorldGenerated && !AlreadySentSignal)
		{
			Controller.Instance.WorldManager.Call("on_generate_regions");
			AlreadySentSignal = true;
		}
		
		// Process all waiting batches in the queue
		while (_animationQueue.TryDequeue(out Cell[] cellsBatch))
		{
			foreach (Cell cell in cellsBatch)
			{
				if (cell == null) break;
				var coords = cell.AxialCoords();
				Vector2I gridPos = new Vector2I(coords.x, coords.y);
				Vector2I atlasCoords = cell.CollapsedCellData.CollapsedState.ToAtlasCoords();
				WorldTileMap.SetCell(gridPos, cell.CollapsedCellData.CollapsedState.GetSourceId(), atlasCoords);
			}
		}
	}

	public void SetCellsToAnimateFromThread(Cell[] cellsToAnimate)
	{
		_animationQueue.Enqueue(cellsToAnimate);
	}

	public void OnReceivePatches(Godot.Collections.Dictionary<Resource, Array<Array<Vector2I>>> patches)
	{
		Controller.Instance.Emit(new WorldGenCompleteSignal());

		int i = 0;
		
		foreach (var key in patches.Keys)
		{
			var value = patches[key];
		
			for (int j = 0; j < value.Count; j++)
			{
				var innerArray = value[j];
		
				for (int k = 0; k < innerArray.Count; k++)
				{
					innerArray[k] = FromAxialToOddQ(innerArray[k] );
				}
			}
		
		
			i++;
		}
		
		
		GD.Print("Received patches");
		DetailGenerator detailGenerator = new DetailGenerator(_wfc.Grid,patches, _seed, _heightNoiseMap.HeightMap);
		
		
		_pendingFoods = detailGenerator.GeneratedFood;
		Controller.Instance.Subscribe<PathfindBakingCompleteSignal>(OnPathfindingReady);
		
	
	}

	private void OnPathfindingReady(PathfindBakingCompleteSignal signal)
	{

		
		
		Controller.Instance.Unsubscribe<PathfindBakingCompleteSignal>(OnPathfindingReady);
		
		if (_pendingFoods == null) return;
		
		if (ActionShelf.Instance?.CategoryItems == null)
		{
			GD.PrintErr("ActionShelf not initialized when placing food");
			return;
		}
		
		if (!ActionShelf.Instance.CategoryItems.ContainsKey("Nourriture"))
		{
			GD.PrintErr("ActionShelf missing 'Nourriture' category");
			return;
		}
		
		var foodsListNames = ActionShelf.Instance.CategoryItems["Nourriture"];
		if (foodsListNames.Count == 0)
		{
			GD.PrintErr("'Nourriture' category is empty");
			return;
		}
		
		foreach (var food in _pendingFoods)
		{
		
			foodsListNames[((int)food.food) % foodsListNames.Count].ExecuteAction(AxialToCube(food.x, food.y));

		}
		
		_pendingFoods = null;
	}
	public static Vector3I AxialToCube(int col, int row)
	{
		int x = col - row;
		int y = row;
		int z = -col ;
		return new Vector3I(x, y, z);
	}
	private async void StartGenerationLoop()
	{
		if (_generationCts != null && !_generationCts.IsCancellationRequested)
		{

			return;
		}

		_generationCts = new CancellationTokenSource();
		CancellationToken token = _generationCts.Token;
		_isGenerating = true; 

		await Task.Run(async () =>
		{
			while (!token.IsCancellationRequested && !_wfc.IsAllWorldGenerated)
			{
				if (_isPaused && !_singleStepRequested)
				{
					
					await Task.Delay(100, token); 
					continue;
				}

				int stepsToTake = _singleStepRequested ? 1 : _generationSpeed;
				_singleStepRequested = false;

				if (stepsToTake > 0)
				{
					_wfc.GenerateWithAnimation(stepsToTake, SetCellsToAnimateFromThread, token);
				}

				if (_wfc.IsAllWorldGenerated)
				{
					
					break;
				}

			
				if (!_isPaused)
				{
					await Task.Delay(10, token); 
				}
			}
			_isGenerating = false; 

		}, token);
	}

	public Vector2I FromAxialToOddQ(Vector2I samsPos)
	{
		int alexX = samsPos.X - samsPos.Y;

		int alexY;
		if (alexX % 2 == 0)
		{
			alexY = (samsPos.X + samsPos.Y) / 2;
		}
		else
		{
			alexY = (samsPos.X + samsPos.Y- 1) / 2;
		}

		return new Vector2I(alexX, alexY);
	}
}
