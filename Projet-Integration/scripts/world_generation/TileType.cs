using System;
using System.Collections.Generic;
using Godot;
using Microsoft.CSharp.RuntimeBinder;

namespace Terrarium_Virtuel.scripts.world_generation;

public enum TileType
{
    Grass,
    Sand,
    Water,
    Snow,
    Mountain,
    SnowSmallTrees,
    SnowBigTrees
}




public static class TileTypeExtensions
{
    
    
    
    public static Dictionary<TileType, int> KeyAndZero = new()
    {
        { TileType.Grass ,0 },
        { TileType.Sand, 0 },
        { TileType.Water, 0 },
        { TileType.Snow , 0},
        { TileType.Mountain , 0},
        { TileType.SnowSmallTrees , 0},
        { TileType.SnowBigTrees , 0},
    };
    public static Dictionary<TileType, float> KeyAndOnes = new()
    {
        { TileType.Grass ,1 },
        { TileType.Sand, 1},
        { TileType.Water, 1 },
        { TileType.Snow , 1},
        { TileType.Mountain , 1},
        { TileType.SnowSmallTrees , 1},
        { TileType.SnowBigTrees , 1},
    };
    public static Dictionary<TileType, Vector2I> KeyAndVectorMinusOnes = new()
    {
        { TileType.Grass , new Vector2I(-1, -1) },
        { TileType.Sand, new Vector2I(-1, -1) },
        { TileType.Water, new Vector2I(-1, -1)  },
        { TileType.Snow , new Vector2I(-1, -1) },
        { TileType.Mountain , new Vector2I(-1, -1) },
        { TileType.SnowSmallTrees , new Vector2I(-1, -1) },
        { TileType.SnowBigTrees , new Vector2I(-1, -1) },
    };
    //(PeakHeight, StandardDeviation)
    public static Dictionary<TileType, (float PeakHeight, float Spread)> ElevationProfiles = new()
    {
        { TileType.Water, (0f, 0.15f) },
        { TileType.Sand, (0.3f, 0.1f) },
        { TileType.Grass, (0.34f, 0.2f) },
        { TileType.Snow, (0.45f, 0.15f) },
        { TileType.SnowSmallTrees, (0.65f, 0.09f) },
        { TileType.SnowBigTrees, (0.72f, 0.08f) },
        { TileType.Mountain, (0.85f, 0.15f) }
    };

  
    public static Dictionary<TileType, (float ClusterRadius, float Exponent, float Multiplier)> CenterOfMassProfiles = new()
    {
        { TileType.Water, (51f, 0.055f, 2.3f) },
        { TileType.Sand, (51f, 0.055f, 2.3f) },
        { TileType.Grass, (51f, 0.055f, 2.3f) }, 
        { TileType.Snow, (51f, 0.055f, 2.3f) },
        { TileType.SnowSmallTrees,(51f, 0.055f, 2.3f)},
        { TileType.SnowBigTrees, (51f, 0.055f, 2.3f) },
        { TileType.Mountain, (51f, 0.055f, 2.3f) }
    };

    public static Dictionary<TileType, float> Probabilities = new()
    {
        { TileType.Grass ,10 },
        { TileType.Sand, 8 },
        { TileType.Water, 10 },
        { TileType.Snow , 24},
        { TileType.Mountain , 0},
        { TileType.SnowSmallTrees , 0},
        { TileType.SnowBigTrees , 0},
    };
    
    

    public static readonly Dictionary<TileType, String[][]> Sockets = new()
    {
        // 1rst priority
        { TileType.Grass, [
            ["Snow", "Sand"], ["Snow", "Sand"], ["Snow", "Sand"], 
            ["Snow", "Sand"], ["Snow", "Sand"], ["Snow", "Sand"] 
        ]},
    
        { TileType.Sand, [
            ["Grass"], ["Grass"], ["Grass"], 
            ["Grass"], ["Grass"], ["Grass"] 
        ]},
        { TileType.Snow ,  [["Grass"] ,["Grass"] ,["Grass"]  ,["Grass"] ,["Grass"]  ,["Grass"]  ]},
        // 2nd priority
        { TileType.Water, [["Sand" ] ,["Sand" ] ,["Sand" ] ,["Sand" ]  ,["Sand" ]  ,["Sand" ]  ]},
        { TileType.SnowSmallTrees, [["Snow"] ,["Snow"] ,["Snow"]  ,["Snow"] ,["Snow"]  ,["Snow"]  ]},      
        { TileType.SnowBigTrees, [["SnowSmallTrees"] ,["SnowSmallTrees"] ,["SnowSmallTrees"]  ,["SnowSmallTrees"] ,["SnowSmallTrees"]  ,["SnowSmallTrees"]  ]},
        // 3rd priority
        
        { TileType.Mountain , [["SnowBigTrees"] ,["SnowBigTrees"] ,["SnowBigTrees"]  ,["SnowBigTrees"] ,["SnowBigTrees"]  ,["SnowBigTrees"]  ]}
    };


    public static (float PeakHeight, float Spread) GetElevationProfile(this TileType type)
    {
        return ElevationProfiles[type];
    }

    public static (float ClusterRadius, float Exponent, float Multiplier) GetCenterOfMassProfile(this TileType type)
    {
        return CenterOfMassProfiles[type];
    }

    public static void ModifyProbabilitiesCuzElevation(Cell cell,CellData cellData, float currentElevation)
    {
        float sum = 0;
        float weight ;
        foreach (var tile in cellData.GetTilesKeyCollection())
        {
            weight = GetElevationWeight(tile, currentElevation * 1.2f);
            cellData.MultiplyTileByWeight(tile, (float) Math.Pow(weight + 1, 6));
            sum += cellData.GetTileProbability(tile);
        }
        foreach (var tile in cellData.GetTilesKeyCollection())
        {
            cellData.MultiplyTileByWeight(tile, 1 / sum);

        }
    }
    public static float GetElevationWeight(this TileType type, float currentElevation)
    {
        var profile = ElevationProfiles[type];
        float numerator = (currentElevation - profile.PeakHeight) * (currentElevation - profile.PeakHeight);
        float denominator = 2f * (profile.Spread * profile.Spread);
        return Mathf.Exp(-numerator / denominator);
    }

    public static String[][] GetSockets(this TileType type)
    {
        return Sockets[type];
    }
    
    public static float GetProb(this TileType type)
    {
        return Probabilities[type];
    }

    public static void SetProb(this TileType type, int newProb)
    {
        Probabilities[type] = newProb;
    }
    
    public static Vector2I ToAtlasCoords(this TileType type)
    {
        return type switch
        {
            TileType.Grass => new Vector2I(0, 0),
            TileType.Sand  => new Vector2I(6, 1),
            TileType.Water => new Vector2I(6, 0),
            TileType.Snow  => new Vector2I(0, 2),
            TileType.SnowSmallTrees => new Vector2I(1, 2),
            TileType.SnowBigTrees => new Vector2I(2, 2),
            TileType.Mountain => new Vector2I(5, 0),
            _ => throw new RuntimeBinderException(null, null)
        };
    }

    public static int GetSourceId(this TileType type)
    {
        return type switch
        {
            TileType.Grass => 2,
            TileType.Sand => 0,
            TileType.Water => 0,
            TileType.Snow => 3,
            TileType.SnowSmallTrees => 3,
            TileType.SnowBigTrees => 3,
            TileType.Mountain => 3,
            _ => throw new RuntimeBinderException(null, null)
        };
    }
    
    
}