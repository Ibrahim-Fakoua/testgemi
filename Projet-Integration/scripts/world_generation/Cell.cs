using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using Godot;
using Vector2 = System.Numerics.Vector2;


namespace Terrarium_Virtuel.scripts.world_generation;

public static class Helpers
{
    private static readonly (int dx, int dy)[][] Offset = new (int dx, int dy)[][]
    {
        // Even X offsets
        new[] { (0, -1), (1, -1), (1, 0), (0, 1), (-1, 0), (-1, -1) },
        // Odd X offsets
        new[] { (0, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0) }
    };

    public static List<(int x, int y)> GetNeighbours((int x, int y) tilePos, (int x, int y) gridSize)
    {
        List<(int x, int y)> myCoords = new List<(int, int)>();

        for (int i = 0; i < 6; i++)
        {
            if (TryGetNeighbour(tilePos, i, gridSize, out var neighbour))
            {
                myCoords.Add(neighbour);
            }
        }

        return myCoords;
    }

    public static bool TryGetNeighbour((int x, int y) tilePos, int direction, (int x, int y) gridSize,
        out (int x, int y) neighbour)
    {
        var offset = Offset[tilePos.x & 1][direction];
        neighbour.x = tilePos.x + offset.dx;
        neighbour.y = tilePos.y + offset.dy;

        return neighbour.x >= 0 && neighbour.x < gridSize.x && neighbour.y >= 0 && neighbour.y < gridSize.y;
    }

    public static float GetDistanceBetweenHexes(Vector2 hex1, Vector2 hex2)
    {

   
            float x1 = 1.5f * hex1.X;
            float y1 = 1.73205f * (hex1.Y + 0.5f * ((int)Math.Floor(hex1.X) & 1));

            float x2 = 1.5f * hex2.X;
            float y2 = 1.73205f * (hex2.Y + 0.5f * ((int)Math.Floor(hex2.X) & 1));

            float dx = x1 - x2;
            float dy = y1 - y2;

            return (float)Math.Sqrt((dx * dx) + (dy * dy));
        
        
    }

    public static void IncreaseAreaByOne(int key, List<int> KeyToArea)
    {
        if (key == -1) return;
        int root = GetRootAreaKey(key, KeyToArea);
        KeyToArea[root] += 1;
    }

    public static void MergeCenterOfMassKeys(Vector2I centerOfMassKeys, List<Vector2> KeyToCenterOfMass, List<int> KeyToArea, out int NewKey)
    {
        int root1 = GetRootCenterOfMassKey(centerOfMassKeys.X, KeyToCenterOfMass);
        int root2 = GetRootCenterOfMassKey(centerOfMassKeys.Y, KeyToCenterOfMass);


        if (root1 == root2)
        {
            NewKey = root1;
            return;
        }

        Vector2 newCenter = GetCenterOfMassFromVectorKey(centerOfMassKeys, KeyToCenterOfMass, KeyToArea);
        KeyToCenterOfMass.Add(newCenter);

        int newRoot = KeyToCenterOfMass.Count - 1;
    

        KeyToCenterOfMass[root1] = new Vector2(-newRoot, -newRoot);
        KeyToCenterOfMass[root2] = new Vector2(-newRoot, -newRoot);
    
        NewKey = newRoot;
    }

    public static Vector2 GetCenterOfMassFromVectorKey(Vector2I centerOfMassKey, List<Vector2> KeyToCenterOfMass, List<int> KeyToArea)
    {
        if (centerOfMassKey.X == -1) throw new Exception("centerOfMassKey == -1");
        int root1 = GetRootCenterOfMassKey(centerOfMassKey.X, KeyToCenterOfMass);
        int root2 = GetRootCenterOfMassKey(centerOfMassKey.Y, KeyToCenterOfMass);


        if (root1 == root2) return KeyToCenterOfMass[root1];

        if (root2 == -1)
        {
            return KeyToCenterOfMass[GetRootCenterOfMassKey(centerOfMassKey.X, KeyToCenterOfMass)];
        }
        else
        {
            float area1 = GetAreaFromKey(root1, KeyToArea);
            float area2 = GetAreaFromKey(root2, KeyToArea);
            return (KeyToCenterOfMass[root1] * (area1 / (area1 + area2))) +
                    (KeyToCenterOfMass[root2] * (area2 / (area1 + area2)));
        }
    }

    public static float CenterOfMassSensitivity = 0.5f; 

    public static void AddToCenterOfMass(int centerOfMassKey, int areaKey, (int x, int y) pos,
        List<Vector2> KeyToCenterOfMass, List<int> KeyToArea)
    {
        if (centerOfMassKey == -1) return;

        float area = (float)GetAreaFromKey(areaKey, KeyToArea);
        Vector2 currentCenter = KeyToCenterOfMass[centerOfMassKey];
        Vector2 newPos = new Vector2(pos.x, pos.y);


        if (area <= 1)
        {
            KeyToCenterOfMass[centerOfMassKey] = newPos;
            return;
        }


        float weight = (1.0f / area) * CenterOfMassSensitivity;

  
        KeyToCenterOfMass[centerOfMassKey] = Vector2.Lerp(currentCenter, newPos, weight);
    }

    public static void MergeAreaKeys(Vector2I areaKeys, List<int> KeyToArea, out int NewAreaKey)
    {
        int root1 = GetRootAreaKey(areaKeys.X, KeyToArea);
        int root2 = GetRootAreaKey(areaKeys.Y, KeyToArea);



        if (root1 == root2) 
        {
            NewAreaKey = root1;
            return; 
        }

        int combinedArea = GetAreaFromVectorKey(areaKeys, KeyToArea);
        KeyToArea.Add(combinedArea);

        int newRoot = KeyToArea.Count - 1;
        KeyToArea[root1] = -newRoot;
        KeyToArea[root2] = -newRoot;
        NewAreaKey = newRoot;
    }

    public static int GetAreaFromVectorKey(Vector2I areaKey, List<int> KeyToArea)
    {
        if (areaKey.Y == -1)
        {
            return KeyToArea[GetRootAreaKey(areaKey.X, KeyToArea)];
        }
        int root1 = GetRootAreaKey(areaKey.X, KeyToArea);
        int root2 = GetRootAreaKey(areaKey.Y, KeyToArea);
    

        if (root1 == root2) return KeyToArea[root1];
    
        return KeyToArea[root1] + KeyToArea[root2];
    }

    public static int GetRootCenterOfMassKey(int key, List<Vector2> KeyToCenterOfMass)
    {
        if (key == -1) return -1;
        int root = key;
        while (KeyToCenterOfMass[root].X < 0)
        {
            root = (int)-KeyToCenterOfMass[root].X;
        }

        return root;
    }

    public static int GetRootAreaKey(int key, List<int> KeyToArea)
    {
        int rootx = key;

        if (key == -1) return -1;
        int root = key;
        while (KeyToArea[root] < 0)
        {
            root = -KeyToArea[root];
        }

        return root;
    }

    public static ref int GetAreaFromKey(int key, List<int> KeyToArea)
    {
        if (key == -1) throw new Exception("Key is -1");

        int root = key;
        while (KeyToArea[root] < 0)
        {
            root = -KeyToArea[root];
        }

        return ref CollectionsMarshal.AsSpan(KeyToArea)[root];
    }
}

public class CollapsedCellData
{
    public int CollapsedAreaKey;
    public TileType CollapsedState;
    public int CollapsedCenterOfMassKey;
    private Cell cell;
    private RandomNumberGenerator Rng;

    public CollapsedCellData(Cell cell, TileType CollapsedState, int CollapsedCenterOfMassKey, int CollapsedAreaKey)
    {
        this.cell = cell;
        this.CollapsedState = CollapsedState;
        this.CollapsedAreaKey = CollapsedAreaKey;
        this.CollapsedCenterOfMassKey = CollapsedCenterOfMassKey;
    }



    public int GetCenterOfMassKey()
    {
        return Helpers.GetRootCenterOfMassKey(CollapsedCenterOfMassKey, cell.KeyToCenterOfMass);
    }

    public int GetArea()
    {
        return Helpers.GetAreaFromKey(CollapsedAreaKey, cell.KeyToArea);
    }

    public int GetAreaKey()
    {
        return Helpers.GetRootAreaKey(CollapsedAreaKey, cell.KeyToArea);
    }
}

public class CellData
{
    public static class TileTypeConstants 
    {
        public static readonly TileType[] AllTiles = (TileType[])Enum.GetValues(typeof(TileType));
        public const int Count = 7; 
    }
    private Cell cell;
    public (int x, int y) Position;
    public int Entropy { get; private set; }

    public float HighestTileProb = 1;

    public TileType TheTileTypeThatIsTouchingThisCellMost = TileType.Grass;
    // To change all the Dicts to lists More Efficient
    private readonly float[] _tileProbabilities = new float[TileTypeConstants.Count];
    public Dictionary<TileType, Vector2I> TileToPossibleAreaKeys;
    public Dictionary<TileType, Vector2I> TileToCenterOfMassKeys;
    public Dictionary<TileType, int> TileToNumberOfSameTileTouching;
    public Dictionary<TileType, float> TileToWeight;

    public CellData(Cell cell, (int x, int y) position)
    {
        this.cell = cell;
        this.TileToNumberOfSameTileTouching = new Dictionary<TileType, int>(TileTypeExtensions.KeyAndZero);
        this.TileToWeight = new Dictionary<TileType, float>(TileTypeExtensions.KeyAndOnes);
        this.Position = position;
        this.TileToPossibleAreaKeys = new Dictionary<TileType, Vector2I>(TileTypeExtensions.KeyAndVectorMinusOnes);
        this.TileToCenterOfMassKeys = new Dictionary<TileType, Vector2I>(TileTypeExtensions.KeyAndVectorMinusOnes);
        foreach (TileType tile in TileTypeConstants.AllTiles)
        {
            _tileProbabilities[(int)tile] = TileTypeExtensions.Probabilities[tile];
        }

        Entropy = _tileProbabilities.Length;
    }

    public void SetEntropy(int entropy)
    {
        Entropy = entropy;
    }
    public bool WouldThisTileTouchSameTilesIfPlaced(TileType tile)
    {
        if (TileToPossibleAreaKeys[tile].X == -1) return false;
        return true;
    }

    public int GetVectorArea(TileType tile)
    {
        return Helpers.GetAreaFromVectorKey(TileToPossibleAreaKeys[tile], cell.KeyToArea);
    }
    public float GetTileProbability(TileType tile)
    {

        return _tileProbabilities[(int)tile];
    }

    public void MultiplyTileByWeight(TileType tile, float weight)
    {
        int index = (int)tile;
        float oldProb = _tileProbabilities[index];
        
 
        if (oldProb <= 0) return;

        _tileProbabilities[index] *= weight;

    
        if (_tileProbabilities[index] <= 0) 
        {
            _tileProbabilities[index] = 0; 
            Entropy -= 1;
        }
    }

    public float GetTilesProbabilitySum()
    {
        float sum = 0;

        for (int i = 0; i < _tileProbabilities.Length; i++)
        {
            sum += _tileProbabilities[i];
        }
        return sum;
    }

    public TileType[] GetTilesKeyCollection()
    {
        return TileTypeConstants.AllTiles;
    }

    public bool IsTileJoiningArea(TileType tile)
    {

        if (TileToPossibleAreaKeys[tile].Y != -1)
        {
            if (Helpers.GetRootAreaKey(TileToPossibleAreaKeys[tile].X, cell.KeyToArea) ==
                Helpers.GetRootAreaKey(TileToPossibleAreaKeys[tile].Y, cell.KeyToArea))
            {
                return false;
            }
            return true;
        }
        return false;
    }

public void SetTileProbability(TileType tile, float probability)
    {
        int index = (int)tile;
        if (probability == 0 && _tileProbabilities[index] > 0) Entropy -= 1;
        _tileProbabilities[index] = probability;
    }

    public void AddAreaKey(TileType tile, int areaKey)
    {

        int newRoot = Helpers.GetRootAreaKey(areaKey, cell.KeyToArea);
        int currentRootX = Helpers.GetRootAreaKey(TileToPossibleAreaKeys[tile].X, cell.KeyToArea);

        if (newRoot == currentRootX) return; 
        if (TileToPossibleAreaKeys[tile].Y != -1)
        {
            int test = Helpers.GetRootAreaKey(areaKey, cell.KeyToArea);
            int test2 = Helpers.GetRootAreaKey(TileToPossibleAreaKeys[tile].Y, cell.KeyToArea);
            if ((test != test2) && (test != newRoot)) throw new Exception("Tile already has an area key");
        }

        if (TileToPossibleAreaKeys[tile].X == -1)
        {
            TileToPossibleAreaKeys[tile] = new Vector2I(areaKey, -1);
        }
        else
        {
            TileToPossibleAreaKeys[tile] = new Vector2I(TileToPossibleAreaKeys[tile].X, areaKey);
        }
    }

    public void AddCenterOfMassKey(TileType tile, int centerOfMassKey)
    {
        
        
        int newRoot = Helpers.GetRootCenterOfMassKey(centerOfMassKey, cell.KeyToCenterOfMass);
        int currentRootX = Helpers.GetRootCenterOfMassKey(TileToCenterOfMassKeys[tile].X, cell.KeyToCenterOfMass);


        if (newRoot == currentRootX) return; 
        if (TileToCenterOfMassKeys[tile].Y != -1)
        {
            int test = Helpers.GetRootCenterOfMassKey(centerOfMassKey, cell.KeyToCenterOfMass);
            int test2 = Helpers.GetRootCenterOfMassKey(TileToCenterOfMassKeys[tile].Y, cell.KeyToCenterOfMass);
            if ((test != test2) && (test != newRoot)) throw new Exception("Tile already has a CenterOfMass key");
        }
        if (newRoot == currentRootX) return; 
        if (TileToCenterOfMassKeys[tile].X == -1)
        {
            TileToCenterOfMassKeys[tile] = new Vector2I(centerOfMassKey, -1);
        }
        else
        {
            TileToCenterOfMassKeys[tile] = new Vector2I(TileToCenterOfMassKeys[tile].X, centerOfMassKey);
        }
    }

    public Vector2 GetTileCenterOfMass(TileType tile)
    {
        return Helpers.GetCenterOfMassFromVectorKey(TileToCenterOfMassKeys[tile], cell.KeyToCenterOfMass, cell.KeyToArea);
    }

    public int GetTileNumberOfSameTileTouching(TileType tile)
    {
        return TileToNumberOfSameTileTouching[tile];
    }


}

public class Cell
{

    public int FrontierIndex = -1;
    public bool IsCollapsed = false;
    public List<Vector2> KeyToCenterOfMass;
    public List<int> KeyToArea;
    public CellData CellData;
    public CollapsedCellData CollapsedCellData;
    private RandomNumberGenerator Rng;


    public Cell((int x, int y) position, (int x, int y) GridSize, List<int> keyToArea, List<Vector2> keyToCenterOfMass,
        RandomNumberGenerator rng)
    {
        Rng = rng;
        CellData = new CellData(this, position);
        KeyToArea = keyToArea;
        KeyToCenterOfMass = keyToCenterOfMass;
    }

    public float GetHighestTileProbability()
    {
        float highestProb = 0;
        foreach (var tile in CellData.GetTilesKeyCollection())
        {
            if (CellData.GetTileProbability(tile) <= 0 || CellData.TileToPossibleAreaKeys[tile].X == -1) continue;


             float prob =GetRealTimeTileWeight(tile);
             highestProb = Math.Max(highestProb, prob);
             
        }
        return highestProb;
    }
    public void PickTile(TileType tile)
    {

        if (CellData.TileToPossibleAreaKeys[tile].X != -1)
        {
            // Console.WriteLine(("Area punishement " +GetAreaRewardAndPunishment(tile) ));
            // Console.WriteLine(("Center of mass punishement " +GetCenterOfMassPunishment(tile) ));
        }

        
        IsCollapsed = true;

        if (CellData.IsTileJoiningArea(tile))
        {
            Helpers.MergeCenterOfMassKeys(CellData.TileToCenterOfMassKeys[tile], KeyToCenterOfMass,KeyToArea,
                out int NewCenterOfMassKey);
            Helpers.MergeAreaKeys(CellData.TileToPossibleAreaKeys[tile], KeyToArea, out int NewAreaKey);

            CollapsedCellData = new CollapsedCellData(this, tile, NewCenterOfMassKey, NewAreaKey);
        }
        else
        {
            CollapsedCellData = new CollapsedCellData(this, tile, CellData.TileToCenterOfMassKeys[tile].X,
                CellData.TileToPossibleAreaKeys[tile].X);
        }

        Helpers.IncreaseAreaByOne(CellData.TileToPossibleAreaKeys[tile].X, KeyToArea);
        Helpers.AddToCenterOfMass(CollapsedCellData.GetCenterOfMassKey(), CollapsedCellData.GetAreaKey(),
            CellData.Position, KeyToCenterOfMass, KeyToArea);
    }

    public void PickRandomWeightedTile()
    {

        if (IsTileSurroundedMostlyByOtherTiles(out TileType otherTile))
        {
            PickTile(otherTile);
            return;
        }
        
        if (CellData.Entropy == 0)
        {
            PickTile(TileType.Mountain);

            return;
            // throw new Exception("CellData.Entropy is zero WTF");
        }

        foreach (var tile in CellData.GetTilesKeyCollection())
        {
            if (CellData.TileToNumberOfSameTileTouching[tile] >= 5 && CellData.GetTileProbability(tile) > 0)
            {
                PickTile(tile);
                return;
            }
            if (CellData.GetTileProbability(tile) <= 0 || CellData.TileToPossibleAreaKeys[tile].X == -1) continue;


            CellData.MultiplyTileByWeight(tile, GetRealTimeTileWeight(tile));
        }

        float totalWeight = CellData.GetTilesProbabilitySum();

        float choice = Rng.Randf() * totalWeight;

        foreach (var tile in CellData.GetTilesKeyCollection())
        {
            if (CellData.GetTileProbability(tile) <= 0) continue;

            if (choice <= CellData.GetTileProbability(tile))
            {
                PickTile(tile);
                return;
            }

            choice -= CellData.GetTileProbability(tile);
        }

        throw new Exception("Should not be here");
    }

// TO check
    public void NoneCollapseRestriction(Cell cellRestriction, int directionToOther)
    {
        List<TileType> allowedTiles = new List<TileType>(CellData.GetTilesKeyCollection().Length);
        foreach (var thisTile in CellData.GetTilesKeyCollection())
        {
            if (CellData.GetTileProbability(thisTile) == 0) continue;

            foreach (var tile in cellRestriction.CellData.GetTilesKeyCollection())
            {
                if (cellRestriction.CellData.GetTileProbability(tile) == 0) continue;
                if (IsTileAllowed(tile, thisTile, directionToOther))
                {
                    allowedTiles.Add(thisTile);
                    break;
                }
            }
        }


        foreach (var tile in CellData.GetTilesKeyCollection())
        {
            if (!allowedTiles.Contains(tile))
            {
                CellData.SetTileProbability(tile, 0);
            }
        }
        CellData.SetEntropy(allowedTiles.Count) ;
    }

    public bool IsTileAllowed(TileType otherTile, TileType thisTile, int directionToOther)
    {
        int oppositeDirection = (directionToOther + 3) % 6;

        string otherName = otherTile.ToString();

        string[] allowedByOther = Array.Empty<string>();
        var otherSockets = otherTile.GetSockets();
        if (otherSockets != null && otherSockets.Length > 0)
        {
            allowedByOther = otherSockets[oppositeDirection];
        }


        if (CellData.GetTileProbability(thisTile) <= 0) return true;

        string thisName = thisTile.ToString();


        if (thisTile == otherTile)
        {
            return true;
        }

        ;


        string[] allowedByThis = Array.Empty<string>();
        var thisSockets = thisTile.GetSockets();
        if (thisSockets != null && thisSockets.Length > 0)
        {
            allowedByThis = thisSockets[directionToOther];
        }

        bool thisAllowsOther = allowedByThis.Contains(otherName);
        bool otherAllowsThis = allowedByOther.Contains(thisName);
        // I CHANGED THIS IT MAY BE A BUG
        if (!thisAllowsOther && !otherAllowsThis)
        {
            return false;
        }

        return true;
    }

    private float GetCenterOfMassWeight(TileType thisTile)
    {
        var BlobCenterOfMass = CellData.GetTileCenterOfMass(thisTile);

        float dist =
            Helpers.GetDistanceBetweenHexes(BlobCenterOfMass, new Vector2(CellData.Position.x, CellData.Position.y));
        // GD.Print("Distance: " + dist + "");
        
        var profile = thisTile.GetCenterOfMassProfile();

        // float weigh = profile.Multiplier / ((float)Math.Pow(dist / profile.ClusterRadius, profile.Exponent));
        float weigh = Logistic(dist,profile.ClusterRadius, profile.Exponent, profile.Multiplier);
        return weigh;
        
    }

    public static float Logistic(float x, float a, float b, float c)
    {
        return ((float) (a / (1.0 + Math.Exp(b * x - c))));
    }
    private float GetAreaRewardAndPunishment(TileType thisTile)
    {

        float area = CellData.GetVectorArea(thisTile);
        if (thisTile == TileType.Water) area *= 4;
        float x = 160f / (float)Math.Pow(area, 0.5);


        return x;
    }

    public float GetRealTimeTileWeight(TileType tile)
    {
        return GetAreaRewardAndPunishment(tile)
               * GetCenterOfMassWeight(tile);
    }

    private void ProcessForSameTileFromCollapsedCell(TileType thisTile, int directionToOther, Cell cellRestriction)
    {
        CellData.AddAreaKey(thisTile, cellRestriction.CollapsedCellData.GetAreaKey());
        CellData.AddCenterOfMassKey(thisTile, cellRestriction.CollapsedCellData.GetCenterOfMassKey());
    }

    public void NewRestrictionFromCollapsedCell(TileType otherTile, int directionToOther, Cell cellRestriction)
    {
        
        CellData.TileToNumberOfSameTileTouching[cellRestriction.CollapsedCellData.CollapsedState] += 1;
        if (CellData.TileToNumberOfSameTileTouching[cellRestriction.CollapsedCellData.CollapsedState] >
            CellData.TileToNumberOfSameTileTouching[CellData.TheTileTypeThatIsTouchingThisCellMost])
        {
            CellData.TheTileTypeThatIsTouchingThisCellMost = cellRestriction.CollapsedCellData.CollapsedState;
        }
        foreach (var thisTile in CellData.GetTilesKeyCollection())
        {
            if (thisTile == otherTile)
            {
                ProcessForSameTileFromCollapsedCell(thisTile, directionToOther, cellRestriction);
            }
            else
            {
                if (!IsTileAllowed(otherTile, thisTile, directionToOther))
                {
                    CellData.SetTileProbability(thisTile, 0);

                }
            }


            if (CellData.GetTileProbability(thisTile) > CellData.HighestTileProb)
                CellData.HighestTileProb = CellData.GetTileProbability(thisTile);
        }
    }

    private bool IsTileSurroundedMostlyByOtherTiles(out TileType other)
    {
        if (CellData.TileToNumberOfSameTileTouching[CellData.TheTileTypeThatIsTouchingThisCellMost] >= 4)
        {
            other = CellData.TheTileTypeThatIsTouchingThisCellMost;

            return true;
        }

        other = TileType.Mountain;
        return false;
    }
    public (int x, int y) AxialCoords()
    {
        int x;
        int y;
        if ((CellData.Position.x == 1) && (CellData.Position.y == 0))
        {
            return (1, 0);
        }

        if (CellData.Position.x % 2 == 0)
        {
            x = (CellData.Position.x / 2) + CellData.Position.y;
            y = -(CellData.Position.x / 2) + CellData.Position.y;
        }
        else
        {
            x = (CellData.Position.x / 2) + 1 + CellData.Position.y;
            y = -(CellData.Position.x / 2) + CellData.Position.y;
        }

        return (x, y);
    }
}