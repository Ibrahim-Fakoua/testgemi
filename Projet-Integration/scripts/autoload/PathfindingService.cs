using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Pathfinding service for hexagonal tilemaps. Pure C# implementation for performance.
/// </summary>
public partial class PathfindingService : Node
{
    // Radius of an area - changing this breaks hardcoded calculations
    private const int Radius = 4;

    private MainLayer _tilemap;
    private List<Vector2I> _areasPos = new List<Vector2I>();
    private List<Area> _areas = new List<Area>();

    // Direction vectors for horizontal hex layout (cube coordinates)
    private static readonly Vector3I[] CubeDirections = new Vector3I[]
    {
        new Vector3I(1, -1, 0),   // TOP_RIGHT
        new Vector3I(1, 0, -1),   // RIGHT
        new Vector3I(0, 1, -1),   // BOTTOM_RIGHT
        new Vector3I(-1, 1, 0),   // BOTTOM_LEFT
        new Vector3I(-1, 0, 1),   // LEFT
        new Vector3I(0, -1, 1)    // TOP_LEFT
    };

    public PathfindingService() { }

    public void Initialize(MainLayer currentTilemap)
    {
        _tilemap = currentTilemap;
    }

    public void CreatePathfinding()
    {
        var areas = DesignateAreas();
        CreateAreas(areas);
        FixAdjacencyForChunks(areas);
    }

    #region Pure C# Hex Math Implementation

    /// <summary>
    /// Converts cube coordinates to map (2D) coordinates for horizontal stacked layout.
    /// </summary>
    public static Vector2I CubeToMap(Vector3I cubePosition)
    {
        int lX = cubePosition.X + ((cubePosition.Y & ~1) >> 1);
        int lY = cubePosition.Y;
        return new Vector2I(lX, lY);
    }

    /// <summary>
    /// Converts map (2D) coordinates to cube coordinates for horizontal stacked layout.
    /// </summary>
    public static Vector3I MapToCube(Vector2I mapPosition)
    {
        int lX = mapPosition.X - ((mapPosition.Y & ~1) >> 1);
        int lY = mapPosition.Y;
        return new Vector3I(lX, lY, -lX - lY);
    }

    /// <summary>
    /// Calculates the hex distance between two cube coordinates.
    /// </summary>
    public static int CubeDistance(Vector3I a, Vector3I b)
    {
        return Math.Max(Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)), Math.Abs(a.Z - b.Z));
    }

    /// <summary>
    /// Gets all 6 adjacent neighbors of a hex in cube coordinates.
    /// </summary>
    public static Vector3I[] CubeNeighbors(Vector3I cube)
    {
        var result = new Vector3I[6];
        for (int i = 0; i < 6; i++)
        {
            result[i] = cube + CubeDirections[i];
        }
        return result;
    }

    /// <summary>
    /// Gets all hexes within a certain distance of a center hex.
    /// </summary>
    public static List<Vector3I> CubeRange(Vector3I center, int distance)
    {
        var results = new List<Vector3I>();
        if (distance == 0)
        {
            results.Add(center);
            return results;
        }

        for (int q = -distance; q <= distance; q++)
        {
            int r1 = Math.Max(-distance, -q - distance);
            int r2 = Math.Min(distance, -q + distance);
            for (int r = r1; r <= r2; r++)
            {
                int s = -q - r;
                results.Add(center + new Vector3I(q, r, s));
            }
        }
        return results;
    }

    /// <summary>
    /// Gets all hexes at exactly a certain distance from the center (ring).
    /// </summary>
    public static List<Vector3I> CubeRing(Vector3I center, int radius)
    {
        var results = new List<Vector3I>();
        if (radius < 1)
        {
            results.Add(center);
            return results;
        }

        // Start at top-right direction scaled by radius
        Vector3I hex = center + CubeDirections[0] * radius;

        // Walk around the ring - for each of 6 directions, walk radius steps
        for (int i = 2; i < 8; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                results.Add(hex);
                hex += CubeDirections[i % 6];
            }
        }
        return results;
    }

    /// <summary>
    /// Explores connected hexes using a filter callback (flood fill).
    /// This is a pure C# implementation to avoid Callable overhead.
    /// </summary>
    public List<Vector3I> CubeExplore(Vector3I start, Func<Vector3I, bool> filter)
    {
        var result = new List<Vector3I>();
        var visited = new HashSet<Vector3I>();
        var queue = new Queue<Vector3I>();

        visited.Add(start);
        if (filter(start))
        {
            result.Add(start);
        }
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var neighbors = CubeNeighbors(current);

            foreach (var neighbor in neighbors)
            {
                if (visited.Contains(neighbor))
                    continue;

                visited.Add(neighbor);

                if (filter(neighbor))
                {
                    result.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return result;
    }

    #endregion

    #region Area Management

    /// <summary>
    /// Returns an array of vectors for all portions of the tilemap that can house a valid area.
    /// </summary>
    private List<Vector2I> DesignateAreas()
    {
        var validAreas = new List<Vector2I>();
        int increment = 0;

        while (true)
        {
            var possibleAreas = CubeRing(Vector3I.Zero, increment);
            bool continueRing = false;

            foreach (var area3i in possibleAreas)
            {
                var area2i = CubeToMap(area3i);
                if (ValidateArea(area2i))
                {
                    validAreas.Add(area2i);
                    continueRing = true;
                }
            }

            if (!continueRing)
                break;

            increment++;
        }

        return validAreas;
    }

    private void CreateAreas(List<Vector2I> areasToMake)
    {
        foreach (var areaVector in areasToMake)
        {
            int index = BinarySearchInsert(areaVector);

            if (index >= _areasPos.Count || _areasPos[index] != areaVector)
            {
                _areasPos.Insert(index, areaVector);
                var newArea = new Area(_tilemap, this);
                _areas.Insert(index, newArea);
            }
            else
            {
                var existingArea = _areas[index];
                existingArea.Destroy();
                var newArea = new Area(_tilemap, this);
                _areas[index] = newArea;
            }

            var currentArea = _areas[index];
            var tilesInArea = GetAllTilesInArea(areaVector);
            currentArea.RebuildChunks(areaVector, tilesInArea);
        }
    }

    private void FixAdjacencyForChunks(List<Vector2I> areasToCheck)
    {
        foreach (var areaVector in areasToCheck)
        {
            int index = BinarySearch(areaVector);
            if (index < 0) continue;

            var area = _areas[index];
            var neighbors = CubeNeighbors(MapToCube(areaVector));
            var chunkQueue = new List<Chunk>();

            foreach (var initialChunk in area.Chunks)
            {
                // Check adjacency within same area
                foreach (var destinationChunk in area.Chunks)
                {
                    if (initialChunk != destinationChunk &&
                        !destinationChunk.AreYouMyNeighbor(initialChunk) &&
                        AreChunksAdjacent(initialChunk, destinationChunk))
                    {
                        initialChunk.Neighbors.Add(destinationChunk);
                        destinationChunk.Neighbors.Add(initialChunk);
                    }
                }

                // Check adjacency with neighboring areas
                foreach (var neighboringArea in neighbors)
                {
                    var neighboringArea2i = CubeToMap(neighboringArea);
                    int neighboringAreaIndex = BinarySearch(neighboringArea2i);

                    if (neighboringAreaIndex >= 0 && neighboringAreaIndex < _areasPos.Count &&
                        _areasPos[neighboringAreaIndex] == neighboringArea2i)
                    {
                        var actualNeighboringArea = _areas[neighboringAreaIndex];
                        foreach (var chunk in actualNeighboringArea.Chunks)
                        {
                            chunkQueue.Add(chunk);
                        }
                    }
                }

                foreach (var destinationChunk in chunkQueue)
                {
                    if (!destinationChunk.AreYouMyNeighbor(initialChunk) &&
                        AreChunksAdjacent(initialChunk, destinationChunk))
                    {
                        initialChunk.Neighbors.Add(destinationChunk);
                        destinationChunk.Neighbors.Add(initialChunk);
                    }
                }

                chunkQueue.Clear();
            }
        }
    }

    #endregion

    #region Public API Methods

    /// <summary>
    /// Takes a tile and returns all possible moves from this tile's position.
    /// </summary>
    public Godot.Collections.Array<Vector3I> PossibleMoves(Vector3I origin)
    {
        DisableTile(origin, false);
        if (ValidateTile(origin))
        {
            var originId = _tilemap.PathfindingGetPointId(CubeToMap(origin));
            var potentiallyValidMoves = _tilemap.Astar.GetPointConnections(originId);
            var validMoves = new Godot.Collections.Array<Vector3I>();

            foreach (var move in potentiallyValidMoves)
            {
                if (!_tilemap.Astar.IsPointDisabled(move))
                {
                    var cubeMove = LocalToCube(_tilemap.Astar.GetPointPosition(move));
                    if (ValidateTile(cubeMove))
                    {
                        validMoves.Add(cubeMove);
                    }
                }
            }

            DisableTile(origin, true);
            if (validMoves.Count == 0)
            {
                validMoves.Add(origin);
            }
            return validMoves;
        }
        else
        {
            GD.PushWarning("Origin tile does not exist");
            return new Godot.Collections.Array<Vector3I>();
        }
    }

    /// <summary>
    /// Returns the tile the moving entity needs to go to get closer to the given tags.
    /// Returns null if the target is too far, not reachable, or non-existent.
    /// </summary>
    public Variant MoveToClosestThing(Vector3I origin, Godot.Collections.Array<string> tags, Godot.Collections.Array<string> filter = null)
    {
        filter ??= new Godot.Collections.Array<string>();
        var foundChunk = SearchForThings(origin, 12, tags, filter);

        if (foundChunk != null)
        {
            var originChunk = GetChunkAtTile(origin);
            if (originChunk == foundChunk && originChunk != null)
            {
                var thing = foundChunk.FindNearestThing(this, origin, tags, filter);
                if (thing != null)
                {
                    var point1 = _tilemap.PathfindingGetPointId(CubeToMap(origin));
                    var point2 = _tilemap.PathfindingGetPointId(CubeToMap(thing.Position));
                    var path = Pathfind(point1, point2);

                    if (path != null && path.Length > 1)
                    {
                        if (path.Length <= 2)
                            return thing;
                        else
                            return LocalToCube(path[1]);
                    }
                }
                return default;
            }
            else
            {
                var point1 = _tilemap.PathfindingGetPointId(CubeToMap(origin));
                var point2 = _tilemap.PathfindingGetPointId(CubeToMap(foundChunk.Tiles[0]));
                var path = Pathfind(point1, point2);

                if (path != null && path.Length > 0)
                    return LocalToCube(path[1]);
            }
        }
        return default;
    }

    /// <summary>
    /// Returns null if the target is not in danger, or returns the closest threat.
    /// </summary>
    public GodotObject AmIInDanger(Vector3I startingPosition, int fightscore, int morale, Godot.Collections.Array<string> filter = null)
    {
        filter ??= new Godot.Collections.Array<string>();
        var chunk = GetChunkAtTile(startingPosition);

        if (chunk != null)
        {
            GodotObject closestDanger = null;
            var chunksToCheck = new List<Chunk> { chunk };
            chunksToCheck.AddRange(chunk.Neighbors);

            foreach (var individualChunk in chunksToCheck)
            {
                var potentialDangers = individualChunk.GetAllWithTags(startingPosition, new Godot.Collections.Array<string> { "critter" }, filter);

                foreach (var danger in potentialDangers)
                {
                    bool valid = true;
                    foreach (var filteredTag in filter)
                    {
                        if (danger.Tags.Contains(filteredTag))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid && CubeDistance(startingPosition, danger.Position) <= 8)
                    {
                        int dangerScore = danger.GetIntimidation() - fightscore;
                        if (dangerScore > morale)
                        {
                            if (closestDanger != null)
                            {
                                var closestDangerEntity = closestDanger as GenericEntity;
                                if (closestDangerEntity != null &&
                                    CubeDistance(startingPosition, danger.Position) < CubeDistance(startingPosition, closestDangerEntity.Position))
                                {
                                    closestDanger = danger;
                                }
                            }
                            else
                            {
                                closestDanger = danger;
                            }
                        }
                    }
                }
            }

            return closestDanger;
        }
        else
        {
            GD.PushError("Invalid Chunk");
            return null;
        }
    }

    public Vector3I MoveAwayFrom(Vector3I origin, GodotObject scary)
    {
        var scaryEntity = scary as GenericEntity;
        if (scaryEntity == null) return origin;

        var moves = PossibleMoves(origin);
        var furthestMoves = new List<Vector3I>();
        int d = 0;

        foreach (var move in moves)
        {
            int d1 = CubeDistance(move, scaryEntity.Position);
            if (d1 > d)
            {
                d = d1;
                furthestMoves.Clear();
                furthestMoves.Add(move);
            }
            else if (d1 == d)
            {
                furthestMoves.Add(move);
            }
        }

        return furthestMoves[GD.RandRange(0, furthestMoves.Count - 1)];
    }

    /// <summary>
    /// Returns null if no prey can be found within 12 chunks, otherwise returns a critter
    /// with a lower food chain score than this critter.
    /// </summary>
    public GodotObject GetNearestPrey(Vector3I origin, int foodChain, Godot.Collections.Array<string> filter = null)
    {
        filter ??= new Godot.Collections.Array<string>();
        var chunk = GetChunkAtTile(origin);

        if (chunk == null)
        {
            GD.PushError("Invalid chunk for finding prey");
            return null;
        }

        var queue = new List<Chunk> { chunk };
        var chunksToReset = new List<Chunk> { chunk };
        chunk.Reset();
        chunk.From = new List<Chunk> { chunk };

        var nextQueue = new List<Chunk>();
        int maxDepth = 12;
        int depth = 0;
        var results = new List<GenericEntity>();

        while (depth <= maxDepth)
        {
            if (queue.Count == 0)
            {
                if (results.Count == 0)
                {
                    depth++;
                    if (nextQueue.Count == 0)
                        break;

                    foreach (var nextChunk in nextQueue)
                    {
                        queue.Add(nextChunk);
                        nextChunk.Depth = depth;
                    }
                    nextQueue.Clear();
                }
                else
                {
                    break;
                }
            }
            else
            {
                var source = queue[0];
                var targets = source.GetAllWithTags(origin, new Godot.Collections.Array<string> { "critter" }, filter);

                // Remove entities with food chain >= ours
                targets.RemoveAll(critter => critter.GetFoodChain() >= foodChain);

                if (targets.Count > 0)
                {
                    results.AddRange(targets);
                }
                else
                {
                    foreach (var neighbor in source.Neighbors)
                    {
                        if (neighbor.Unexplored && !nextQueue.Contains(neighbor))
                        {
                            nextQueue.Add(neighbor);
                            neighbor.Unexplored = false;
                            neighbor.Depth = depth + 1;
                            chunksToReset.Add(neighbor);
                        }
                        if (depth < 1)
                        {
                            if (!neighbor.From.Contains(neighbor))
                                neighbor.From.Add(neighbor);
                        }
                        else
                        {
                            foreach (var i in source.From)
                            {
                                if (neighbor.Depth == depth + 1 && !neighbor.From.Contains(i))
                                {
                                    neighbor.From.Add(i);
                                }
                            }
                        }
                    }
                }
                queue.RemoveAt(0);
            }
        }

        GodotObject finalChoice = null;
        if (results.Count > 0)
        {
            int d = int.MaxValue;
            var closestPrey = new List<GenericEntity>();

            foreach (var potentialPrey in results)
            {
                int dist = CubeDistance(potentialPrey.Position, origin);
                if (dist < d)
                {
                    d = dist;
                    closestPrey.Clear();
                    closestPrey.Add(potentialPrey);
                }
                else if (dist == d)
                {
                    closestPrey.Add(potentialPrey);
                }
            }

            finalChoice = closestPrey[GD.RandRange(0, closestPrey.Count - 1)];
        }

        foreach (var chunkToFix in chunksToReset)
        {
            chunkToFix.Reset();
        }

        return finalChoice;
    }

    public Chunk SearchForThings(Vector3I startingPosition, int maxDepth, Godot.Collections.Array<string> things, Godot.Collections.Array<string> filter)
    {
        var chunk = GetChunkAtTile(startingPosition);
        if (chunk == null)
        {
            GD.PushError("Invalid chunk for searching");
            return null;
        }

        var queue = new List<Chunk> { chunk };
        var chunksToReset = new List<Chunk> { chunk };
        chunk.Reset();
        chunk.From = new List<Chunk> { chunk };

        var nextQueue = new List<Chunk>();
        int depth = 0;
        var results = new List<Chunk>();

        while (depth <= maxDepth)
        {
            if (queue.Count == 0)
            {
                if (results.Count == 0)
                {
                    depth++;
                    if (nextQueue.Count == 0)
                        break;

                    foreach (var nextChunk in nextQueue)
                    {
                        queue.Add(nextChunk);
                        nextChunk.Depth = depth;
                    }
                    nextQueue.Clear();
                }
                else
                {
                    break;
                }
            }
            else
            {
                var source = queue[0];
                var targets = source.GetAllWithTags(startingPosition, things, filter);

                if (targets.Count > 0)
                {
                    results.Add(source);
                }
                else
                {
                    foreach (var neighbor in source.Neighbors)
                    {
                        if (neighbor.Unexplored && !nextQueue.Contains(neighbor))
                        {
                            nextQueue.Add(neighbor);
                            neighbor.Unexplored = false;
                            neighbor.Depth = depth + 1;
                            chunksToReset.Add(neighbor);
                        }
                        if (depth < 1)
                        {
                            if (!neighbor.From.Contains(neighbor))
                                neighbor.From.Add(neighbor);
                        }
                        else
                        {
                            foreach (var i in source.From)
                            {
                                if (neighbor.Depth == depth + 1 && !neighbor.From.Contains(i))
                                {
                                    neighbor.From.Add(i);
                                }
                            }
                        }
                    }
                }
                queue.RemoveAt(0);
            }
        }

        Chunk finalChoice = null;
        if (results.Count > 0)
        {
            var chosenChunk = results[GD.RandRange(0, results.Count - 1)];
            finalChoice = chosenChunk.From[GD.RandRange(0, chosenChunk.From.Count - 1)];
        }

        foreach (var chunkToFix in chunksToReset)
        {
            chunkToFix.Reset();
        }

        return finalChoice;
    }

    #endregion

    #region Utility Methods

    public void DisableTile(Vector3I coords, bool disable = true)
    {
        var id = _tilemap.PathfindingGetPointId(CubeToMap(coords));
        _tilemap.Astar.SetPointDisabled(id, disable);
    }

    public void DisablePoint(int id, bool disable = true)
    {
        _tilemap.Astar.SetPointDisabled(id, disable);
    }

    public Vector2[] Pathfind(int point1, int point2)
    {
        DisablePoint(point1, false);
        DisablePoint(point2, false);
        var path = _tilemap.Astar.GetPointPath(point1, point2);
        DisablePoint(point1, true);
        DisablePoint(point2, true);
        return path;
    }

    public bool ValidateTile(Vector3I coords)
    {
        var tile2i = CubeToMap(coords);
        return _tilemap.GetCellTileData(tile2i) != null;
    }

    public Vector3I BigHexToSmall(Vector2I bigHex)
    {
        var x = bigHex.X * new Vector3I(9, -5, -4);
        var y = bigHex.Y * new Vector3I(-5, -4, 9);
        return x + y;
    }

    public Vector2I SmallHexToBig(Vector3I smallHex)
    {
        const float area = 61f;
        const float shift = 14f;

        float x = smallHex.X;
        float z = smallHex.Y;
        float y = smallHex.Z;

        float xh = Mathf.Floor((y + shift * x) / area);
        float yh = Mathf.Floor((z + shift * y) / area);
        float zh = Mathf.Floor((x + shift * z) / area);

        int i = (int)Mathf.Floor((1 + xh - yh) / 3);
        int j = (int)Mathf.Floor((1 + yh - zh) / 3);
        int k = (int)Mathf.Floor((1 + zh - xh) / 3);

        return CubeToMap(new Vector3I(i, j, k));
    }

    public List<Vector3I> GetAllTilesInArea(Vector2I coords)
    {
        var center = BigHexToSmall(coords);
        return CubeRange(center, Radius);
    }

    public bool ValidateArea(Vector2I coords)
    {
        var possibleTiles = GetAllTilesInArea(coords);
        foreach (var tile in possibleTiles)
        {
            if (ValidateTile(tile))
                return true;
        }
        return false;
    }

    public Area GetAreaFromTile(Vector3I tile)
    {
        if (ValidateTile(tile))
        {
            var areaCoord = SmallHexToBig(tile);
            int index = BinarySearch(areaCoord);
            if (index >= 0 && index < _areas.Count)
            {
                return _areas[index];
            }
            else
            {
                GD.PushError("Area cannot be fetched");
            }
        }
        else
        {
            GD.PushError("Tile does not exist");
        }
        return null;
    }

    public Chunk GetChunkAtTile(Vector3I tile)
    {
        var area = GetAreaFromTile(tile);
        if (area == null) return null;

        foreach (var chunk in area.Chunks)
        {
            if (chunk.IsTileInChunk(tile))
                return chunk;
        }
        GD.PushError("TILE NOT IN AREA");
        return null;
    }

    public bool AmISwitchingChunks(GodotObject thing, Vector3I tile1, Vector3I tile2)
    {
        if (tile1 == tile2)
        {
            GD.PushWarning("Destination is current position");
        }

        var chunk1 = GetChunkAtTile(tile1);
        var chunk2 = GetChunkAtTile(tile2);

        if (chunk2 == null)
        {
            GD.PushError("Non-existent destination");
            return false;
        }

        if (chunk1 != chunk2)
        {
            var entity = thing as GenericEntity;
            if (entity != null)
            {
                chunk1?.MoveToChunk(entity, chunk2);
            }
            return true;
        }
        return false;
    }

    public bool AreChunksAdjacent(Chunk entryChunk, Chunk exitChunk)
    {
        if (entryChunk == exitChunk)
        {
            GD.PushWarning("Entry chunk is identical to exit chunk");
            return false;
        }

        foreach (var entryTile in entryChunk.Tiles)
        {
            foreach (var exitTile in exitChunk.Tiles)
            {
                var astarEntryTile = _tilemap.PathfindingGetPointId(CubeToMap(entryTile));
                var astarExitTile = _tilemap.PathfindingGetPointId(CubeToMap(exitTile));
                if (_tilemap.Astar.ArePointsConnected(astarEntryTile, astarExitTile))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private Vector3I LocalToCube(Vector2 localPosition)
    {
        return MapToCube(_tilemap.LocalToMap(localPosition));
    }

    private int BinarySearch(Vector2I value)
    {
        int low = 0;
        int high = _areasPos.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            int cmp = CompareVectors(_areasPos[mid], value);

            if (cmp == 0)
                return mid;
            else if (cmp < 0)
                low = mid + 1;
            else
                high = mid - 1;
        }
        return low;
    }

    private int BinarySearchInsert(Vector2I value)
    {
        return BinarySearch(value);
    }

    private static int CompareVectors(Vector2I a, Vector2I b)
    {
        if (a.Y != b.Y)
            return a.Y.CompareTo(b.Y);
        return a.X.CompareTo(b.X);
    }

    #endregion
}

/// <summary>
/// Represents an area containing multiple chunks in the pathfinding grid.
/// </summary>
public partial class Area : GodotObject
{
    public List<Chunk> Chunks { get; } = new List<Chunk>();
    private List<Vector3I> _remainingTiles;
    private MainLayer _tilemap;
    private PathfindingService _pathfinding;

    public Area(MainLayer tilemap, PathfindingService pathfinding)
    {
        _tilemap = tilemap;
        _pathfinding = pathfinding;
    }

    public void Destroy()
    {
        foreach (var chunk in Chunks)
        {
            chunk.Destroy();
        }
    }

    public void RebuildChunks(Vector2I areaVector, List<Vector3I> tilesInArea)
    {
        _remainingTiles = new List<Vector3I>(tilesInArea);

        while (_remainingTiles.Count > 0)
        {
            var startTile = _remainingTiles[0];
            var data = _tilemap.GetCellTileData(PathfindingService.CubeToMap(startTile));

            if (data == null || (bool)data.GetCustomData("Impassable"))
            {
                _remainingTiles.RemoveAt(0);
            }
            else
            {
                // Use pure C# flood fill
                var results = _pathfinding.CubeExplore(startTile, FilterForChunks);

                if (results.Count > 0)
                {
                    var newChunk = new Chunk(areaVector, results);
                    Chunks.Add(newChunk);

                    foreach (var resultTile in results)
                    {
                        _remainingTiles.Remove(resultTile);
                    }
                }
                else
                {
                    GD.PushError("Nothing to explore");
                    _remainingTiles.RemoveAt(0);
                }
            }
        }
    }

    private bool FilterForChunks(Vector3I tile)
    {
        var data = _tilemap.GetCellTileData(PathfindingService.CubeToMap(tile));
        if (data != null && !(bool)data.GetCustomData("Impassable"))
        {
            return _remainingTiles.Contains(tile);
        }
        return false;
    }
}

/// <summary>
/// Represents a chunk of connected tiles within an area.
/// </summary>
public partial class Chunk : GodotObject
{
    public List<Vector3I> Tiles { get; }
    public List<GenericEntity> Contents { get; } = new List<GenericEntity>();
    public int Depth { get; set; }
    public bool Unexplored { get; set; } = true;
    public List<Chunk> From { get; set; } = new List<Chunk>();
    public Vector2I Area { get; }
    public List<Chunk> Neighbors { get; } = new List<Chunk>();

    public Chunk(Vector2I area, List<Vector3I> tilesInChunk)
    {
        Area = area;
        Tiles = tilesInChunk;
    }

    public void Reset()
    {
        Depth = 0;
        Unexplored = true;
        From = new List<Chunk>();
    }

    public void Destroy()
    {
        foreach (var neighbor in Neighbors)
        {
            neighbor.Neighbors.Remove(this);
        }
    }

    public void AddToChunk(GenericEntity thing)
    {
        // Insert sorted by main tag
        int index = Contents.BinarySearch(thing, new EntityTagComparer());
        if (index < 0) index = ~index;
        Contents.Insert(index, thing);
    }

    public void RemoveFromChunk(GenericEntity thing)
    {
        Contents.Remove(thing);
    }

    public void MoveToChunk(GenericEntity thing, Chunk destination)
    {
        Contents.Remove(thing);
        destination.AddToChunk(thing);
    }

    public bool AreYouMyNeighbor(Chunk potentialNeighbor)
    {
        return Neighbors.Contains(potentialNeighbor);
    }

    public bool IsTileInChunk(Vector3I potentialTile)
    {
        return Tiles.Contains(potentialTile);
    }

    public List<GenericEntity> GetAllWithTags(Vector3I startingPosition, Godot.Collections.Array<string> searchedTags, Godot.Collections.Array<string> filter = null)
    {
        filter ??= new Godot.Collections.Array<string>();
        var result = new List<GenericEntity>();

        foreach (var entity in Contents)
        {
            if (entity.Position != startingPosition && !result.Contains(entity))
            {
                int count = 0;
                foreach (var searchedTag in searchedTags)
                {
                    if (entity.Tags.Contains(searchedTag))
                        count++;
                }

                if (count >= searchedTags.Count)
                {
                    result.Add(entity);
                }
            }
        }

        // Filter out entities with filtered tags
        result.RemoveAll(entity =>
        {
            foreach (var tag in filter)
            {
                if (entity.Tags.Contains(tag))
                    return true;
            }
            return false;
        });

        return result;
    }

    public GenericEntity FindNearestThing(PathfindingService pathfinding, Vector3I origin, Godot.Collections.Array<string> searchedTags, Godot.Collections.Array<string> filter = null)
    {
        filter ??= new Godot.Collections.Array<string>();
        int distance = int.MaxValue;
        GenericEntity chosenThing = null;

        foreach (var thing in GetAllWithTags(origin, searchedTags, filter))
        {
            int d = PathfindingService.CubeDistance(origin, thing.Position);
            if (d < distance)
            {
                distance = d;
                chosenThing = thing;
            }
        }

        if (chosenThing == null)
        {
            GD.PushWarning("You are searching for nothing.");
        }

        return chosenThing;
    }

    private class EntityTagComparer : IComparer<GenericEntity>
    {
        public int Compare(GenericEntity a, GenericEntity b)
        {
            if (a.Tags.Count == 0 || b.Tags.Count == 0)
            {
                GD.PushWarning("Creature has no tags!");
                return 0;
            }
            return string.Compare(a.Tags[0], b.Tags[0], StringComparison.Ordinal);
        }
    }
}
