using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Array = System.Array;

namespace Terrarium_Virtuel.scripts.world_generation;

/// <summary>
/// Tunable parameters for food/detail generation.
/// Adjust these to control the appearance of food placement.
/// </summary>
public static class DetailGenerationParams
{
    // ============== NOISE PARAMETERS ==============


    public static float NoiseScale = 0.01f;

    /// <summary>
    /// How much the noise affects food placement (0 = pure random, 1 = fully noise-based).
    /// Range: 0.0 - 1.0. Default: 0.6
    /// </summary>
    public static float NoiseInfluence = 1f;

    /// <summary>
    /// Noise threshold for food placement. Higher = sparser food (more cells become None).
    /// Range: 0.0 - 1.0. Default: 0.4
    /// </summary>
    public static float NoiseThreshold = 0.1f;


    public static int NoiseOctaves = 3;


    public static float NoiseLacunarity = 2.0f;


    public static float NoiseGain = 0.5f;

  

    /// <summary>
    /// How strongly elevation affects food type selection.
    /// Range: 0.0 - 1.0. Default: 0.8
    /// </summary>
    public static float ElevationInfluence = 0.8f;

    /// <summary>
    /// Exponent for elevation weight calculation. Higher = sharper elevation preferences.
    /// Range: 1 - 30. Default: 20
    /// </summary>
    public static int ElevationExponent = 20;

    // ============== DENSITY PARAMETERS ==============

    /// <summary>
    /// Base probability multiplier for FoodType.None. Higher = sparser food.
    /// Range: 1.0 - 50.0. 
    /// </summary>
    public static float NoneMultiplier = 2.0f;

    /// <summary>
    /// Global density multiplier for all non-None food types.
    /// </summary>
    public static float FoodDensityMultiplier = 1.0f;

    // ============== VARIETY PARAMETERS ==============

    /// <summary>
    /// Use separate noise layers for each food type (more variety) vs single noise.
    /// </summary>
    public static bool UsePerFoodTypeNoise = true;

    /// <summary>
    /// Offset multiplier between food type noise layers. Higher = more distinct patterns per food.
    /// Range: 100 - 10000. 
    /// </summary>
    public static int FoodTypeNoiseOffset = 1000;
}

/// <summary>
/// Generates scattered food/details for biome patches using noise-based patterns.
/// Each cell is processed based on noise, elevation, and base probabilities.
/// </summary>
public partial class DetailGenerator : RefCounted
{
    public (int height, int width) GridSize;
    public Cell[,] OriginalGrid;
    public float[,] HeightMap;
    public RandomNumberGenerator Rng;


    private FastNoiseLite _noiseGenerator;
    private int _baseSeed;


    public List<(int q, int r, FoodType food)> GeneratedFood = new List<(int, int, FoodType)>();

    public DetailGenerator(Cell[,] grid, Godot.Collections.Dictionary<Resource, Array<Array<Vector2I>>> patches, ulong seed, float[,] heightMap)
    {
        OriginalGrid = grid;
        HeightMap = heightMap;
        GridSize = (grid.GetLength(0), grid.GetLength(1));
        Rng = new RandomNumberGenerator();
        Rng.Seed = seed;
        _baseSeed = (int)(seed % int.MaxValue);

        // Initialize noise generator
        _noiseGenerator = new FastNoiseLite();
        _noiseGenerator.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        _noiseGenerator.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
        _noiseGenerator.FractalOctaves = DetailGenerationParams.NoiseOctaves;
        _noiseGenerator.FractalLacunarity = DetailGenerationParams.NoiseLacunarity;
        _noiseGenerator.FractalGain = DetailGenerationParams.NoiseGain;
        _noiseGenerator.Frequency = DetailGenerationParams.NoiseScale;

        foreach (var entry in patches)
        {
            var biome = entry.Key;
            var biomePatches = entry.Value;

            foreach (var patch in biomePatches)
            {
                GenerateDetailForPatch(patch, biome);
            }
        }
    }


    private float GetNoiseValue(int x, int y, int foodTypeOffset = 0)
    {
        _noiseGenerator.Seed = _baseSeed + foodTypeOffset;
        float noise = _noiseGenerator.GetNoise2D(x, y);
        return (noise + 1f) / 2f;
    }


    private void GenerateDetailForPatch(Array<Vector2I> patch, Resource biome)
    {
        BiomeType biomeType = (BiomeType)Enum.Parse(typeof(BiomeType), biome.ResourceName);


        if (biomeType == BiomeType.Water) return;

        
        var validFoodTypes = FoodTypeExtensions.BiomeToFood[biomeType];
        if (validFoodTypes.Count == 0) return;


        foreach (var pos in patch)
        {
            int y = pos.Y;
            int x = pos.X;

    
            float baseNoise = GetNoiseValue(x, y, 0);

           
            float adjustedThreshold = DetailGenerationParams.NoiseThreshold;
            float randomFactor = Rng.Randf() * (1f - DetailGenerationParams.NoiseInfluence);
            float combinedValue = baseNoise * DetailGenerationParams.NoiseInfluence + randomFactor;

            if (combinedValue < adjustedThreshold)
            {

                continue;
            }

  
            var cell = new FoodCell((x, y), Rng, biomeType);

        
            cell.FoodCellData.MultiplyFoodTypeByWeight(FoodType.None, DetailGenerationParams.NoneMultiplier);

            float elevation = HeightMap[y, x];
            ApplyElevationModification(cell.FoodCellData, elevation);

        
            if (DetailGenerationParams.UsePerFoodTypeNoise)
            {
                ApplyPerFoodTypeNoise(cell.FoodCellData, x, y, validFoodTypes);
            }
            else
            {
                ApplyUniformNoise(cell.FoodCellData, baseNoise);
            }


            foreach (var foodType in validFoodTypes)
            {
                if (foodType == FoodType.None) continue;
                cell.FoodCellData.MultiplyFoodTypeByWeight(foodType, DetailGenerationParams.FoodDensityMultiplier);
            }


            cell.PickRandomWeightedFoodType();


            if (cell.CollapsedFoodCellData.CollapsedState != FoodType.None)
            {
                var axial = AxialCoords(x, y);
                GeneratedFood.Add((axial.q, axial.r, cell.CollapsedFoodCellData.CollapsedState));
            }
        }
    }


    private void ApplyElevationModification(FoodCellData cellData, float currentElevation)
    {
        if (DetailGenerationParams.ElevationInfluence <= 0) return;

        float sum = 0;
        foreach (FoodType foodType in cellData.GetValidFoodTypes())
        {
            if (foodType == FoodType.None) continue;

            float weight = foodType.GetElevationWeight(currentElevation * 1.2f);

            float blendedWeight = Mathf.Lerp(1f, weight, DetailGenerationParams.ElevationInfluence);
            float finalWeight = (float)Math.Pow(blendedWeight + 1, DetailGenerationParams.ElevationExponent);

            cellData.MultiplyFoodTypeByWeight(foodType, finalWeight);
            sum += cellData.GetFoodTypeProbability(foodType);
        }


        if (sum > 0)
        {
            foreach (FoodType foodType in cellData.GetValidFoodTypes())
            {
                if (foodType == FoodType.None) continue;
                cellData.MultiplyFoodTypeByWeight(foodType, 1f / sum);
            }
        }
    }


    private void ApplyPerFoodTypeNoise(FoodCellData cellData, int x, int y, List<FoodType> validFoodTypes)
    {
        foreach (var foodType in validFoodTypes)
        {
            if (foodType == FoodType.None) continue;


            int offset = (int)foodType * DetailGenerationParams.FoodTypeNoiseOffset;
            float noise = GetNoiseValue(x, y, offset);


            float noiseMultiplier = Mathf.Lerp(1f, noise * 2f, DetailGenerationParams.NoiseInfluence);
            cellData.MultiplyFoodTypeByWeight(foodType, noiseMultiplier);
        }
    }


    private void ApplyUniformNoise(FoodCellData cellData, float baseNoise)
    {
        float noiseMultiplier = Mathf.Lerp(1f, baseNoise * 2f, DetailGenerationParams.NoiseInfluence);

        foreach (var foodType in cellData.GetValidFoodTypes())
        {
            if (foodType == FoodType.None) continue;
            cellData.MultiplyFoodTypeByWeight(foodType, noiseMultiplier);
        }
    }


    public (int q, int r) AxialCoords(int p_x, int p_y)
    {
        int x, y;
        if ((p_x == 1) && (p_y == 0))
        {
            return (1, 0);
        }

        if (p_x % 2 == 0)
        {
            x = (p_x / 2) + p_y;
            y = -(p_x / 2) + p_y;
        }
        else
        {
            x = (p_x / 2) + 1 + p_y;
            y = -(p_x / 2) + p_y;
        }

        return (x, y);
    }


    public int[][][] ConvertToDisplayFormat()
    {
        var byY = new System.Collections.Generic.Dictionary<int, List<int[]>>();

        foreach (var (q, r, food) in GeneratedFood)
        {
            int offset_x = q;
            int offset_y = r + (q - (q & 1)) / 2;

            if (!byY.ContainsKey(offset_y))
            {
                byY[offset_y] = new List<int[]>();
            }

            var atlas = food.ToAtlasCoords();
            byY[offset_y].Add(new int[] { q, r, offset_x, offset_y, atlas.X, atlas.Y, food.GetSourceId() });
        }

        int maxY = 0;
        foreach (var y in byY.Keys)
        {
            if (y > maxY) maxY = y;
        }

        int[][][] result = new int[maxY + 1][][];
        for (int y = 0; y <= maxY; y++)
        {
            if (byY.ContainsKey(y))
            {
                result[y] = byY[y].ToArray();
            }
            else
            {
                result[y] = Array.Empty<int[]>();
            }
        }

        return result;
    }
}
