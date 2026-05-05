using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Pathfinding service that manages areas and chunks for efficient pathfinding in a hex-based tilemap.
/// </summary>
public partial class PathfindingService : Node
{
    // Radius of an area - changing this breaks calculations as they are hardcoded to 4
    public const int Radius = 4;

    private GodotObject _tilemap;

    private List<Vector2I> _areasPos = new();
    private List<Area> _areas = new();

    /// <summary>
    /// Gets the tilemap (MainLayer) this service operates on.
    /// </summary>
    public GodotObject Tilemap => _tilemap;

    /// <summary>
    /// Default constructor required for GDScript instantiation.
    /// </summary>
    public PathfindingService()
    {
    }

    /// <summary>
    /// Creates a new PathfindingService for the given tilemap (C# usage).
    /// </summary>
    public PathfindingService(GodotObject currentTilemap)
    {
        _tilemap = currentTilemap;
    }

    /// <summary>
    /// Initialize the service with the tilemap (for GDScript usage).
    /// Call this after creating the instance with new().
    /// </summary>
    public void Initialize(GodotObject currentTilemap)
    {
        _tilemap = currentTilemap;
    }

    /// <summary>
    /// Creates the pathfinding system by designating areas, creating them, and fixing chunk adjacency.
    /// </summary>
    public void CreatePathfinding()
    {
        var areas = DesignateAreas();
        foreach (var VARIABLE in areas)
        {
            var test = FromAxialToOddQ(VARIABLE.X, VARIABLE.Y);
            if (test.X < 0 || test.Y < 0)
            {
                GD.PushError("HEMMMMMMMMM");
                return;
            }
        }
        CreateAreas(areas);
        FixAdjacencyForChunks(areas);
    }
    

    public static Vector3I AxialToCube(Vector2I mapPosition)
    {
        int l_y = mapPosition.Y;
        int l_z = -mapPosition.X;

        return new Vector3I(-l_y -l_z, l_y, l_z);
    }
    // static func _cube_to_vertical_diamond_down(cube_position: Vector3i) -> Vector2i:
    // var l_x = -cube_position.z
    // var l_y = cube_position.y
    //     return Vector2i(l_x, l_y)
    public static Vector2I CubeToAxial(Vector3I cubePosition)
    {
        int l_x = -cubePosition.Z;
        int l_y = cubePosition.Y;

        return new Vector2I(l_x, l_y);
    }


    public static (int X, int Y) FromAxialToOddQ(int x, int y)
    {
        int alexX = x - y;

        int alexY;
        if (alexX % 2 == 0)
        {
            alexY = (x + y) / 2;
        }
        else
        {
            alexY = (x + y - 1) / 2;
        }

        return (alexX, alexY);
    }
    public static (int X, int Y) CubeToOddQ(int x, int y, int z)
    {
        if (x + y + z != 0)
            throw new ArgumentException("Invalid cube coordinates: x + y + z must be 0.");

        int oddX = x;

        int oddY;
        if (x % 2 == 0)
        {
            oddY = x / 2 + y;
        }
        else
        {
            oddY = (x - 1) / 2 + y;
        }

        return (oddX, oddY);
    }
    /// <summary>
    /// Returns an array of vectors for all portions of the tilemap that can house a valid area.
    /// </summary>
    private List<Vector2I> DesignateAreas()
    {
        return (_tilemap as TileMapLayer).GetUsedCells().ToList();
    }

    private void CreateAreas(List<Vector2I> areasToMake)
    {
        foreach (var areaVector in areasToMake)
        {
            int index = BinarySearch(_areasPos, areaVector);

            _areasPos.Insert(index, areaVector);
            var newArea = new Area(_tilemap);
            _areas.Insert(index, newArea);


            var currentArea = _areas[index];
            var tilesInArea = GetAllTilesInArea(areaVector);

            currentArea.RebuildChunks(areaVector, tilesInArea);
        }
    }

    private void FixAdjacencyForChunks(List<Vector2I> areasToCheck)
    {
        foreach (var areaVector in areasToCheck)
        {
            int index = BinarySearch(_areasPos, areaVector);
            var area = _areas[index];
            var neighbors = CubeNeighbors(AxialToCube(areaVector));
            var chunkQueue = new List<Chunk>();

            foreach (var initialChunk in area.Chunks)
            {
                foreach (var destinationChunk in area.Chunks)
                {
                    if (initialChunk != destinationChunk)
                    {
                        if (!destinationChunk.AreYouMyNeighbor(initialChunk))
                        {
                            if (AreChunksAdjacent(initialChunk, destinationChunk))
                            {
                                initialChunk.Neighbors.Add(destinationChunk);
                                destinationChunk.Neighbors.Add(initialChunk);
                            }
                        }
                    }
                }

                foreach (var neighboringArea in neighbors)
                {
                    var neighboringArea2i = CubeToAxial(neighboringArea);
                    int actualNeighboringAreaIndex = BinarySearch(_areasPos, neighboringArea2i);

                    if (actualNeighboringAreaIndex < _areasPos.Count &&
                        neighboringArea2i.Equals(_areasPos[actualNeighboringAreaIndex]))
                    {
                        var actualNeighboringArea = _areas[actualNeighboringAreaIndex];

                        foreach (var chunk in actualNeighboringArea.Chunks)
                        {
                            chunkQueue.Add(chunk);
                        }
                    }
                }

                foreach (var destinationChunk in chunkQueue)
                {
                    if (!destinationChunk.AreYouMyNeighbor(initialChunk))
                    {
                        if (AreChunksAdjacent(initialChunk, destinationChunk))
                        {
                            initialChunk.Neighbors.Add(destinationChunk);
                            destinationChunk.Neighbors.Add(initialChunk);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns all possible moves from this tile's position, or an empty array if no moves are possible.
    /// </summary>
    public Godot.Collections.Array<Vector3I> PossibleMoves(Vector3I origin)
    {
        return new Godot.Collections.Array<Vector3I>(PossibleMovesList(origin));
    }

    private List<Vector3I> PossibleMovesList(Vector3I origin)
    {
        DisableTile(origin, false);

        if (ValidateTile(origin))
        {
            long originId = PathfindingGetPointId(CubeToAxial(origin));
            var astar = GetAstar();
            var potentiallyValidMoves = astar.GetPointConnections(originId);
            var validMoves = new List<Vector3I>();

            foreach (long move in potentiallyValidMoves)
            {
                if (!astar.IsPointDisabled(move))
                {
                    var cubeMove = LocalToCube(astar.GetPointPosition(move));
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
            return new List<Vector3I>();
        }
    }

    /// <summary>
    /// Returns the tile to move to to get closer to the given tags.
    /// Returns null if the target is too far, not reachable, or non-existent.
    /// </summary>
    public Variant MoveToClosestThing(Vector3I origin, Godot.Collections.Array gdTags,
        Godot.Collections.Array gdFilter = null)
    {
        var tags = ToStringArray(gdTags);
        var filter = gdFilter != null ? ToStringArray(gdFilter) : Array.Empty<string>();

        var foundChunk = SearchForThings(origin, 12, tags, filter);

        if (foundChunk != null)
        {
            var originChunk = GetChunkAtTile(origin);

            if (originChunk == foundChunk && originChunk != null)
            {
                var thing = foundChunk.FindNearestThing(_tilemap, origin, tags, filter);

                if (thing.VariantType != Variant.Type.Nil)
                {
                    long point1 = PathfindingGetPointId(CubeToAxial(origin));
                    var thingPos = (Vector3I)thing.AsGodotObject().Get("position");
                    long point2 = PathfindingGetPointId(CubeToAxial(thingPos));
                    var path = Pathfind(point1, point2);

                    if (path != null && path.Length > 1)
                    {
                        if (path.Length <= 2)
                        {
                            return thing;
                        }
                        else
                        {
                            return LocalToCube(path[1]);
                        }
                    }
                    else
                    {
                        return default;
                    }
                }
                else
                {
                    return default;
                }
            }
            else
            {
                long point1 = PathfindingGetPointId(CubeToAxial(origin));
                long point2 = PathfindingGetPointId(CubeToAxial(foundChunk.Tiles[0]));
                var path = Pathfind(point1, point2);

                if (path != null && path.Length > 0)
                {
                    return LocalToCube(path[1]);
                }
                else
                {
                    return default;
                }
            }
        }
        else
        {
            return default;
        }
    }

    /// <summary>
    /// Returns null if the target is not in danger, or returns the closest threat.
    /// </summary>
    public Variant AmIInDanger(Vector3I startingPosition, int fightscore, int morale,
        Godot.Collections.Array gdFilter = null)
    {
        var filter = gdFilter != null ? ToStringArray(gdFilter) : Array.Empty<string>();

        var chunk = GetChunkAtTile(startingPosition);

        if (chunk != null)
        {
            GodotObject closestDanger = null;
            var chunksToCheck = new List<Chunk> { chunk };

            foreach (var neighboringChunk in chunk.Neighbors)
            {
                chunksToCheck.Add(neighboringChunk);
            }

            foreach (var individualChunk in chunksToCheck)
            {
                var potentialDangers = individualChunk.GetAllWithTags(startingPosition, new[] { "critter" });

                foreach (var danger in potentialDangers)
                {
                    bool valid = true;
                    var dangerTags = (Godot.Collections.Array<string>)danger.Get("tags");

                    foreach (var filteredTag in filter)
                    {
                        if (dangerTags.Contains(filteredTag))
                        {
                            valid = false;
                            break;
                        }
                    }

                    var dangerPos = (Vector3I)danger.Get("position");

                    if (valid && CubeDistance(startingPosition, dangerPos) <= 8)
                    {
                        int dangerIntimidation = (int)danger.Call("get_intimidation");
                        int dangerScore = dangerIntimidation - fightscore;

                        if (dangerScore > morale)
                        {
                            if (closestDanger != null)
                            {
                                var closestDangerPos = (Vector3I)closestDanger.Get("position");
                                if (CubeDistance(startingPosition, dangerPos) <
                                    CubeDistance(startingPosition, closestDangerPos))
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

            if (closestDanger != null)
            {
                return Variant.From(closestDanger);
            }
            else
            {
                return default;
            }
        }
        else
        {
            GD.PushError("Invalid Chunk");
            return default;
        }
    }

    /// <summary>
    /// Gets a move that is furthest away from the scary entity.
    /// </summary>
    public Vector3I MoveAwayFrom(Vector3I origin, GodotObject scary)
    {
        var moves = PossibleMovesList(origin);
        var furthestMoves = new List<Vector3I>();
        var scaryPos = (Vector3I)scary.Get("position");

        int d = 0;
        foreach (var move in moves)
        {
            int d1 = CubeDistance(move, scaryPos);
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
    /// Returns null if no prey found within 12 chunks, otherwise returns a critter with lower food chain score.
    /// </summary>
    public Variant GetNearestPrey(Vector3I origin, int foodChain, Godot.Collections.Array gdFilter = null)
    {
        var filter = gdFilter != null ? ToStringArray(gdFilter) : Array.Empty<string>();

        var chunk = GetChunkAtTile(origin);

        if (chunk != null)
        {
            var queue = new List<Chunk> { chunk };
            var chunksToReset = new List<Chunk> { chunk };
            chunk.Reset();
            chunk.From = new List<Chunk> { chunk };

            var nextQueue = new List<Chunk>();
            int maxDepth = 12;
            int depth = 0;

            var results = new List<GodotObject>();

            while (depth <= maxDepth)
            {
                if (queue.Count == 0)
                {
                    if (results.Count == 0)
                    {
                        depth++;
                        if (nextQueue.Count == 0)
                        {
                            break;
                        }
                        else
                        {
                            foreach (var nextChunk in nextQueue)
                            {
                                queue.Add(nextChunk);
                                nextChunk.Depth = depth;
                            }

                            nextQueue.Clear();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    var source = queue[0];
                    var targets = source.GetAllWithTags(origin, new[] { "critter" }, filter);

                    // Filter out critters with food chain >= our food chain
                    var filteredTargets = targets.Where(critter =>
                    {
                        int critterFoodChain = (int)critter.Call("get_food_chain");
                        return critterFoodChain < foodChain;
                    }).ToList();

                    if (filteredTargets.Count > 0)
                    {
                        results.AddRange(filteredTargets);
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
                var closestPrey = new List<GodotObject>();

                foreach (var potentialPrey in results)
                {
                    var preyPos = (Vector3I)potentialPrey.Get("position");
                    int dist = CubeDistance(preyPos, origin);

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

            return finalChoice != null ? Variant.From(finalChoice) : default;
        }
        else
        {
            GD.PushError("Invalid chunk for finding prey");
            return default;
        }
    }

    /// <summary>
    /// Searches for things with specified tags within a maximum depth of chunks.
    /// </summary>
    private Chunk SearchForThings(Vector3I startingPosition, int maxDepth, string[] things, string[] filter)
    {
        var chunk = GetChunkAtTile(startingPosition);

        if (chunk != null)
        {
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
                        {
                            break;
                        }
                        else
                        {
                            foreach (var nextChunk in nextQueue)
                            {
                                queue.Add(nextChunk);
                                nextChunk.Depth = depth;
                            }

                            nextQueue.Clear();
                        }
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
        else
        {
            GD.PushError("Invalid chunk for searching");
            return null;
        }
    }

    /// <summary>
    /// Highlights tiles with a specific variant.
    /// </summary>
    public void HighlightTiles(Godot.Collections.Array<Vector3I> tilesToHighlight, int variant)
    {
        foreach (var tile in tilesToHighlight)
        {
            var tile2i = CubeToAxial(tile);
            _tilemap.Call("set_cell", tile2i, 0, new Vector2I(variant, 1));
        }
    }

    /// <summary>
    /// Converts big hex coordinates to small hex coordinates.
    /// Hardcoded to tiles with a radius of 5 and a diameter of 9 tiles.
    /// </summary>
    public Vector3I BigHexToSmall(Vector2I bigHex)
    {
        var x = bigHex.X * new Vector3I(9, -5, -4);
        var y = bigHex.Y * new Vector3I(-5, -4, 9);
        return x + y;
    }

    /// <summary>
    /// Converts small hex coordinates to big hex coordinates.
    /// </summary>
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

        float i = Mathf.Floor((1 + xh - yh) / 3);
        float j = Mathf.Floor((1 + yh - zh) / 3);
        float k = Mathf.Floor((1 + zh - xh) / 3);

        var result = new Vector3I((int)i, (int)j, (int)k);
        return CubeToAxial(result);
    }

    /// <summary>
    /// Disables or enables a tile in the pathfinding graph.
    /// </summary>
    public void DisableTile(Vector3I coords, bool disable = true)
    {
        long id = PathfindingGetPointId(CubeToAxial(coords));
        if (id == -1)
        {
        
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
        
            // Alternatively, just log an error so the game doesn't crash
            GD.PrintErr($"Attempted to disable non-existent tile at {coords}");
            return; 
        }
        GetAstar().SetPointDisabled(id, disable);
    }

    /// <summary>
    /// Disables or enables a point in the pathfinding graph by its ID.
    /// </summary>
    public void DisablePoint(long id, bool disable = true)
    {
        GetAstar().SetPointDisabled(id, disable);
    }

    /// <summary>
    /// Finds a path between two points.
    /// </summary>
    public Vector2[] Pathfind(long point1, long point2)
    {
        DisablePoint(point1, false);
        DisablePoint(point2, false);
        var path = GetAstar().GetPointPath(point1, point2);
        DisablePoint(point1, true);
        DisablePoint(point2, true);
        return path;
    }

    /// <summary>
    /// Validates if a tile exists at the given cube coordinates.
    /// </summary>
    public bool ValidateTile(Vector3I coords)
    {
        var tile2i = CubeToAxial(coords);
        var tileData = _tilemap.Call("get_cell_tile_data", tile2i);
        return tileData.VariantType != Variant.Type.Nil;
    }

    /// <summary>
    /// Gets the Area containing the given tile.
    /// </summary>
    public Area GetAreaFromTile(Vector3I tile)
    {
        if (ValidateTile(tile))
        {
            var areaCoord = SmallHexToBig(tile);
            int index = BinarySearch(_areasPos, areaCoord);

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

    /// <summary>
    /// Gets all tiles within an area at the given coordinates.
    /// </summary>
    private List<Vector3I> GetAllTilesInArea(Vector2I coords)
    {
        var center = BigHexToSmall(coords);
        return CubeRange(center, Radius);
    }

    /// <summary>
    /// Validates if an area at the given coordinates has any valid tiles.
    /// </summary>
    public bool ValidateArea(Vector2I coords)
    {
        var possibleTiles = GetAllTilesInArea(coords);

        foreach (var tile in possibleTiles)
        {
            if (ValidateTile(tile))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a tile is within a specific area.
    /// </summary>
    public bool IsTileInArea(Vector3I tile, Vector2I area)
    {
        var tiles = GetAllTilesInArea(area);
        return tiles.Contains(tile);
    }

    /// <summary>
    /// Checks if two chunks are adjacent.
    /// </summary>
    public bool AreChunksAdjacent(Chunk entryChunk, Chunk exitChunk)
    {
        if (entryChunk != exitChunk)
        {
            var astar = GetAstar();

            foreach (var entryTile in entryChunk.Tiles)
            {
                foreach (var exitTile in exitChunk.Tiles)
                {
                    long astarEntryTile = PathfindingGetPointId(CubeToAxial(entryTile));
                    long astarExitTile = PathfindingGetPointId(CubeToAxial(exitTile));
                    bool result = astar.ArePointsConnected(astarEntryTile, astarExitTile);

                    if (result)
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            GD.PushWarning("Entry chunk is identical to exit chunk");
        }

        return false;
    }

    /// <summary>
    /// Checks if an entity is switching chunks when moving between tiles.
    /// </summary>
    public bool AmISwitchingChunks(GodotObject thing, Vector3I tile1, Vector3I tile2)
    {
        if (tile1 == tile2)
        {
            GD.PushWarning("destination is current position");
        }

        var chunk1 = GetChunkAtTile(tile1);
        var chunk2 = GetChunkAtTile(tile2);

        if (chunk2 == null)
        {
            GD.PushError("Non-existent destination");
        }

        if (chunk1 != chunk2)
        {
            chunk1.MoveToChunk(thing, chunk2);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the chunk at a specific tile location.
    /// </summary>
    public Chunk GetChunkAtTile(Vector3I tile)
    {
        var area = GetAreaFromTile(tile);

        if (area == null)
        {
            return null;
        }

        foreach (var chunk in area.Chunks)
        {
            if (chunk.IsTileInChunk(tile))
            {
                return chunk;
            }
        }

        GD.PushError("TILE NOT IN AREA");
        return null;
    }

    #region Tilemap Helper Methods

    private AStar2D GetAstar()
    {
        return (AStar2D)_tilemap.Get("astar");
    }

    private long PathfindingGetPointId(Vector2I coord)
    {
        return (long)_tilemap.Call("pathfinding_get_point_id", coord);
    }



    private Vector3I MapToCube(Vector2I mapPosition)
    {
        return (Vector3I)_tilemap.Call("map_to_cube", mapPosition);
    }

    private Vector3I LocalToCube(Vector2 localPosition)
    {
        return (Vector3I)_tilemap.Call("local_to_cube", localPosition);
    }

    private List<Vector3I> CubeRing(Vector3I center, int radius)
    {
        var result = (Godot.Collections.Array<Vector3I>)_tilemap.Call("cube_ring", center, radius);
        return result.ToList();
    }

    private List<Vector3I> CubeNeighbors(Vector3I cube)
    {
        var result = (Godot.Collections.Array<Vector3I>)_tilemap.Call("cube_neighbors", cube);
        return result.ToList();
    }

    private List<Vector3I> CubeRange(Vector3I center, int distance)
    {
        var result = (Godot.Collections.Array<Vector3I>)_tilemap.Call("cube_range", center, distance);
        return result.ToList();
    }

    private int CubeDistance(Vector3I a, Vector3I b)
    {
        return (int)_tilemap.Call("cube_distance", a, b);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Converts a GDScript array to a string array.
    /// </summary>
    private static string[] ToStringArray(Godot.Collections.Array gdArray)
    {
        if (gdArray == null || gdArray.Count == 0)
            return Array.Empty<string>();

        var result = new string[gdArray.Count];
        for (int i = 0; i < gdArray.Count; i++)
        {
            result[i] = gdArray[i].AsString();
        }

        return result;
    }

    private static int BinarySearch(List<Vector2I> list, Vector2I value)
    {
        int low = 0;
        int high = list.Count;

        while (low < high)
        {
            int mid = (low + high) / 2;
            int comparison = CompareVector2I(list[mid], value);

            if (comparison < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    private static int CompareVector2I(Vector2I a, Vector2I b)
    {
        if (a.X != b.X)
            return a.X.CompareTo(b.X);
        return a.Y.CompareTo(b.Y);
    }

    #endregion

    /// <summary>
    /// Represents an area containing multiple chunks.
    /// </summary>
    public partial class Area : RefCounted
    {
        public List<Chunk> Chunks { get; } = new();
        private List<Vector3I> _remainingTiles = new();
        private GodotObject _tilemap;

        public Area(GodotObject tilemap)
        {
            _tilemap = tilemap;
        }

        private bool FilterForChunks(Vector3I tile)
        {
            var data1 = _tilemap.Call("get_cell_tile_data", CubeToAxial(tile));

            if (data1.VariantType != Variant.Type.Nil && (TileData)data1 is not null )
            {
                var tileData = (TileData)data1;
                bool isImpassable = (bool)tileData.GetCustomData("Impassable");

                if (!isImpassable)
                {
                    return _remainingTiles.Contains(tile);
                }
            }

            return false;
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
        
                var data = _tilemap.Call("get_cell_tile_data", CubeToAxial(startTile));
                var axial = CubeToAxial(startTile);
                var oddq = FromAxialToOddQ( axial.X,axial.Y );
                var tileData = data.As<TileData>();

                if (data.VariantType == Variant.Type.Nil || tileData == null || (bool)tileData.GetCustomData("Impassable"))
                {
                    _remainingTiles.Remove(startTile);
                }
                else
                {
                    var results = CubeExplore(startTile);

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
                    }
                }
            }
        }

        private List<Vector3I> CubeExplore(Vector3I start)
        {
            Callable filterCallable = Callable.From<Vector3I, bool>(FilterForChunks);
            var result = (Godot.Collections.Array<Vector3I>)_tilemap.Call("cube_explore", start, filterCallable);
            return result.ToList();
        }
        
    }

    /// <summary>
    /// Represents a chunk within an area, containing tiles and contents.
    /// </summary>
    public partial class Chunk : RefCounted
    {
        public List<Vector3I> Tiles { get; }
        public List<GodotObject> Contents { get; } = new();
        public int Depth { get; set; }
        public bool Unexplored { get; set; } = true;
        public List<Chunk> From { get; set; } = new();
        public Vector2I ChunkArea { get; }
        public List<Chunk> Neighbors { get; } = new();

        public Chunk(Vector2I area, List<Vector3I> tilesInChunk)
        {
            ChunkArea = area;
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

        private static int ContentSort(GodotObject a, GodotObject b)
        {
            var tagsA = (Godot.Collections.Array<string>)a.Get("tags");
            var tagsB = (Godot.Collections.Array<string>)b.Get("tags");

            if (tagsA.Count == 0 || tagsB.Count == 0)
            {
                GD.PushWarning("Creature has no tags!");
                return 0;
            }

            return string.Compare(tagsA[0], tagsB[0], StringComparison.Ordinal);
        }

        public void AddToChunk(GodotObject thing)
        {
            int index = 0;
            for (int i = 0; i < Contents.Count; i++)
            {
                if (ContentSort(thing, Contents[i]) < 0)
                    break;
                index = i + 1;
            }

            Contents.Insert(index, thing);
        }

        public bool DoesChunkHaveTag(string thing)
        {
            foreach (var entity in Contents)
            {
                var tags = (Godot.Collections.Array<string>)entity.Get("tags");
                foreach (var tag in tags)
                {
                    if (tag == thing)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<GodotObject> GetAllWithTags(Vector3I startingPosition, string[] searchedTags,
            string[] filter = null)
        {
            filter ??= Array.Empty<string>();
            var result = new List<GodotObject>();

            foreach (var entity in Contents)
            {
                var entityPos = (Vector3I)entity.Get("position");

                if (!entityPos.Equals(startingPosition) && !result.Contains(entity))
                {
                    var entityTags = (Godot.Collections.Array<string>)entity.Get("tags");
                    int count = 0;

                    foreach (var searchedTag in searchedTags)
                    {
                        if (entityTags.Contains(searchedTag))
                        {
                            count++;
                        }
                    }

                    if (count >= searchedTags.Length)
                    {
                        result.Add(entity);
                    }
                }
            }

            // Filter out entities with filtered tags
            result.RemoveAll(entity =>
            {
                var entityTags = (Godot.Collections.Array<string>)entity.Get("tags");
                foreach (var tag in filter)
                {
                    if (entityTags.Contains(tag))
                    {
                        return true;
                    }
                }

                return false;
            });

            return result;
        }

        public void RemoveFromChunk(GodotObject thing)
        {
            Contents.Remove(thing);
        }

        public void MoveToChunk(GodotObject thing, Chunk destination)
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

        public Variant FindNearestThing(GodotObject tilemap, Vector3I origin, string[] searchedTags,
            string[] filter = null)
        {
            filter ??= Array.Empty<string>();

            int distance = int.MaxValue;
            GodotObject chosenThing = null;

            foreach (var thing in GetAllWithTags(origin, searchedTags, filter))
            {
                var thingPos = (Vector3I)thing.Get("position");
                int d = (int)tilemap.Call("cube_distance", origin, thingPos);

                if (d < distance)
                {
                    distance = d;
                    chosenThing = thing;
                }
            }

            if (chosenThing == null)
            {
                GD.PushWarning("You are searching for nothing.");
                return default;
            }

            return Variant.From(chosenThing);
        }
    }
}