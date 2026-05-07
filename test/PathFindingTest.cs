using Godot;
using GdUnit4;
using System;
using System.Collections.Generic;
using static GdUnit4.Assertions;

[TestSuite]
public class PathFindingTest
{
    // ==========================================
    // 1. FACTORY: MOCK TILEMAP GENERATOR & HELPERS
    // ==========================================
    
    private TileMapLayer CreateMockTileMap()
    {
        var tileMap = AutoFree(new TileMapLayer());

        // 1. Build a custom TileSet with the "Impassable" data layer
        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Hexagon;
        tileSet.AddCustomDataLayer();
        tileSet.SetCustomDataLayerName(0, "Impassable");
        tileSet.SetCustomDataLayerType(0, Variant.Type.Bool);

        // 2. Create a dummy texture atlas WITH actual dimensions
        var source = new TileSetAtlasSource();
        var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
        source.Texture = ImageTexture.CreateFromImage(image);
        source.TextureRegionSize = new Vector2I(16, 16);
        
        // Tile 0: Walkable
        source.CreateTile(new Vector2I(0, 0));
        source.GetTileData(new Vector2I(0, 0), 0).SetCustomData("Impassable", false);
        
        // Tile 1: Impassable (Wall)
        source.CreateTile(new Vector2I(1, 0));
        source.GetTileData(new Vector2I(1, 0), 0).SetCustomData("Impassable", true);

        tileSet.AddSource(source, 0);
        tileMap.TileSet = tileSet;

        // 3. Attach a dynamic GDScript to simulate the custom methods your map has
        var script = new GDScript();
        script.SourceCode = @"
extends TileMapLayer

var astar = AStar2D.new()

func pathfinding_get_point_id(coord: Vector2i) -> int:
    return (coord.x + 1000) * 10000 + (coord.y + 1000)

func map_to_cube(map_position: Vector2i) -> Vector3i:
    return Vector3i(map_position.x, map_position.y, -map_position.x - map_position.y)

func local_to_cube(local_position: Vector2) -> Vector3i:
    return Vector3i(int(local_position.x), int(local_position.y), int(-local_position.x - local_position.y))

func cube_distance(a: Vector3i, b: Vector3i) -> int:
    return int((abs(a.x - b.x) + abs(a.y - b.y) + abs(a.z - b.z)) / 2)

func cube_range(center: Vector3i, distance: int) -> Array[Vector3i]:
    var results: Array[Vector3i] = []
    for x in range(-distance, distance + 1):
        for y in range(max(-distance, -x - distance), min(distance, -x + distance) + 1):
            results.append(center + Vector3i(x, y, -x - y))
    return results

func cube_neighbors(cube: Vector3i) -> Array[Vector3i]:
    return [
        Vector3i(cube.x + 1, cube.y - 1, cube.z), Vector3i(cube.x + 1, cube.y, cube.z - 1),
        Vector3i(cube.x, cube.y + 1, cube.z - 1), Vector3i(cube.x - 1, cube.y + 1, cube.z),
        Vector3i(cube.x - 1, cube.y, cube.z + 1), Vector3i(cube.x, cube.y - 1, cube.z + 1)
    ]

func cube_explore(start: Vector3i, filter_callable: Callable) -> Array[Vector3i]:
    var valid: Array[Vector3i] = []
    if filter_callable.call(start): valid.append(start)
    for n in cube_neighbors(start):
        if filter_callable.call(n): valid.append(n)
    return valid
";
        script.Reload();
        tileMap.SetScript(script);

        return tileMap;
    }

    private Node CreateMockEntity(string[] tags, Vector3I position, int foodChain = 1, int intimidation = 1)
    {
        var node = AutoFree(new Node());
        var script = new GDScript();
        script.SourceCode = @"
extends Node
var tags = []
var position = Vector3i.ZERO
var food_chain = 1
var intimidation = 1

func get_food_chain(): return food_chain
func get_intimidation(): return intimidation
";
        script.Reload();
        node.SetScript(script);

        var gdTags = new Godot.Collections.Array<string>();
        foreach (var tag in tags) gdTags.Add(tag);
        node.Set("tags", gdTags);
        node.Set("position", position);
        node.Set("food_chain", foodChain);
        node.Set("intimidation", intimidation);
        return node;
    }

    private PathfindingService CreateLineService(int length, out Godot.Collections.Array<Vector3I> pathCubes)
    {
        var tileMap = CreateMockTileMap();
        var astar = tileMap.Get("astar").As<AStar2D>();
        pathCubes = new Godot.Collections.Array<Vector3I>();

        for (int i = 0; i < length; i++)
        {
            var axial = new Vector2I(i, 0);
            tileMap.SetCell(axial, 0, new Vector2I(0, 0));
            long id = (long)tileMap.Call("pathfinding_get_point_id", axial);
            astar.AddPoint(id, new Vector2(i, 0));
            pathCubes.Add(PathfindingService.AxialToCube(axial));

            if (i > 0)
            {
                long prevId = (long)tileMap.Call("pathfinding_get_point_id", new Vector2I(i - 1, 0));
                astar.ConnectPoints(prevId, id);
            }
        }

        var service = AutoFree(new PathfindingService(tileMap));
        service.CreatePathfinding();
        return service;
    }

    // ==========================================
    // 2. CORE SYSTEM & BASIC MOVEMENT TESTS
    // ==========================================

    [TestCase]
    [RequireGodotRuntime]
    public void Test_CreatePathfinding_GeneratesAreasAndChunks()
    {
        var service = CreateLineService(3, out var cubes);

        var tile1Area = service.GetAreaFromTile(cubes[0]);
        AssertThat(tile1Area).IsNotNull();
        AssertThat(tile1Area.Chunks.Count).IsGreater(0);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_PossibleMoves_UsesAStarProperly()
    {
        var service = CreateLineService(2, out var cubes);
        
        var moves = service.PossibleMoves(cubes[0]);
        AssertThat(moves.Count).IsEqual(1); // Moving to cubes[1]
        AssertThat(moves[0]).IsEqual(cubes[1]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_RebuildChunks_IgnoresImpassableTiles()
    {
        var tileMap = CreateMockTileMap();
        tileMap.SetCell(new Vector2I(0, 0), 0, new Vector2I(0, 0)); // Walkable
        tileMap.SetCell(new Vector2I(1, 0), 0, new Vector2I(1, 0)); // Impassable Wall

        var service = AutoFree(new PathfindingService(tileMap));
        service.CreatePathfinding();

        var validTile = new Vector3I(0,0,0);
        var wallTile = new Vector3I(-1,1,0); // PathfindingService.AxialToCube(new Vector2I(1,0))

        AssertThat(service.ValidateTile(validTile)).IsTrue();
        
        AssertThrown(() => service.GetChunkAtTile(wallTile))
            .IsInstanceOf<Exception>()
            .HasMessage("chunk not found !");
    }

    // ==========================================
    // 3. TARGET SEEKING (MoveToClosestThing)
    // ==========================================

    [TestCase]
    [RequireGodotRuntime]
    public void Test_MoveToClosestThing_PathDistanceTwo_ReturnsObject()
    {
        var service = CreateLineService(2, out var cubes);
        var item = CreateMockEntity(new[] { "food" }, cubes[1]);
        service.GetChunkAtTile(cubes[1]).AddToChunk(item);

        var result = service.MoveToClosestThing(cubes[0], new Godot.Collections.Array { "food" });

        // Path length is <= 2, it returns the target object itself
        AssertThat((GodotObject)result).IsEqual(item);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_MoveToClosestThing_PathGreaterThanTwo_ReturnsNextStep()
    {
        var service = CreateLineService(5, out var cubes);
        var item = CreateMockEntity(new[] { "food" }, cubes[4]);
        service.GetChunkAtTile(cubes[4]).AddToChunk(item);

        var result = service.MoveToClosestThing(cubes[0], new Godot.Collections.Array { "food" });

        // Path length is > 2, it returns the next step in the AStar path
        AssertThat((Vector3I)result).IsEqual(cubes[1]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_MoveToClosestThing_DifferentChunk_ReturnsNextStep()
    {
        // 15 hexes away will definitely span multiple chunks/areas
        var service = CreateLineService(15, out var cubes);
        var item = CreateMockEntity(new[] { "food" }, cubes[14]);
        service.GetChunkAtTile(cubes[14]).AddToChunk(item);

        var result = service.MoveToClosestThing(cubes[0], new Godot.Collections.Array { "food" });

        // Even though it had to span chunks to find it, it correctly extracts step 1
        AssertThat((Vector3I)result).IsEqual(cubes[1]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_MoveToClosestThing_NoPath_ReturnsDefault()
    {
        var service = CreateLineService(5, out var cubes);
        var item = CreateMockEntity(new[] { "food" }, cubes[4]);
        service.GetChunkAtTile(cubes[4]).AddToChunk(item);
        
        // Break the AStar connection right at the start so AStar literally can't find a path
        // to the next chunk in the chunk graph.
        var astar = service.Tilemap.Get("astar").As<AStar2D>();
        long id0 = (long)service.Tilemap.Call("pathfinding_get_point_id", new Vector2I(0, 0));
        long id1 = (long)service.Tilemap.Call("pathfinding_get_point_id", new Vector2I(1, 0));
        astar.DisconnectPoints(id0, id1); 

        var result = service.MoveToClosestThing(cubes[0], new Godot.Collections.Array { "food" });

        AssertThat(result.VariantType).IsEqual(Variant.Type.Nil);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_MoveToClosestThing_NoTarget_ReturnsDefault()
    {
        var service = CreateLineService(5, out var cubes);
        
        // No item added! SearchForThings will safely return null.
        var result = service.MoveToClosestThing(cubes[0], new Godot.Collections.Array { "food" });

        AssertThat(result.VariantType).IsEqual(Variant.Type.Nil);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void Test_MoveToClosestThing_AlreadyOnTarget_ReturnsDefault()
    {
        var service = CreateLineService(2, out var cubes);
        var item = CreateMockEntity(new[] { "food" }, cubes[0]);
        service.GetChunkAtTile(cubes[0]).AddToChunk(item);

        var result = service.MoveToClosestThing(cubes[0], new Godot.Collections.Array { "food" });

        // Since we are ON the item, AStar Path length is 1. Should return default to stop moving.
        AssertThat(result.VariantType).IsEqual(Variant.Type.Nil);
    }


    // ==========================================
    // 4. DANGER & PREY PATHFINDING
    // ==========================================

    [TestCase]
    [RequireGodotRuntime]
    public void Test_AmIInDanger_FindsClosestThreat()
    {
        var service = CreateLineService(5, out var cubes);
        
        // Threat 1 at dist 3. Score = 5 - 1 = 4 > 1. Valid threat, but further away.
        var threat1 = CreateMockEntity(new[] { "critter" }, cubes[3], intimidation: 5);
        service.GetChunkAtTile(cubes[3]).AddToChunk(threat1);
        
        // Threat 2 at dist 1. Score = 10 - 1 = 9 > 1. Valid threat, closer.
        var threat2 = CreateMockEntity(new[] { "critter" }, cubes[1], intimidation: 10);
        service.GetChunkAtTile(cubes[1]).AddToChunk(threat2);

        var result = service.AmIInDanger(cubes[0], fightscore: 1, morale: 1);

        // It should pick the closer threat
        AssertThat((GodotObject)result).IsEqual(threat2);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_AmIInDanger_Filtered_IgnoresThreat()
    {
        var service = CreateLineService(3, out var cubes);
        var threat = CreateMockEntity(new[] { "critter", "friendly" }, cubes[1], intimidation: 10);
        service.GetChunkAtTile(cubes[1]).AddToChunk(threat);

        var filter = new Godot.Collections.Array { "friendly" };
        var result = service.AmIInDanger(cubes[0], fightscore: 1, morale: 1, filter);

        AssertThat(result.VariantType).IsEqual(Variant.Type.Nil);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_MoveAwayFrom_FindsFurthestTile()
    {
        var service = CreateLineService(3, out var cubes);
        // Map: 0 - 1 - 2. Scary is at 2. We are at 1. Valid moves: 0, 2.
        var scary = CreateMockEntity(new[] { "scary" }, cubes[2]);
        
        var move = service.MoveAwayFrom(cubes[1], scary);
        
        // Dist to 0 is 2, dist to 2 is 0. Expected move is 0.
        AssertThat(move).IsEqual(cubes[0]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_GetNearestPrey_FindsPreyViaBFS()
    {
        var service = CreateLineService(15, out var cubes);
        var prey = CreateMockEntity(new[] { "critter" }, cubes[10], foodChain: 1);
        service.GetChunkAtTile(cubes[10]).AddToChunk(prey);

        var result = service.GetNearestPrey(cubes[0], foodChain: 3);

        AssertThat((GodotObject)result).IsEqual(prey);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_GetNearestPrey_IgnoresHigherFoodChain()
    {
        var service = CreateLineService(5, out var cubes);
        var strongPrey = CreateMockEntity(new[] { "critter" }, cubes[2], foodChain: 5); 
        service.GetChunkAtTile(cubes[2]).AddToChunk(strongPrey);

        var result = service.GetNearestPrey(cubes[0], foodChain: 2);

        // Target's foodchain is 5, our foodchain is 2. It's too strong to eat.
        AssertThat(result.VariantType).IsEqual(Variant.Type.Nil);
    }

    // ==========================================
    // 5. CHUNK STATE LOGIC & ADJACENCY
    // ==========================================

    [TestCase]
    [RequireGodotRuntime]
    public void Test_AmISwitchingChunks()
    {
        var service = CreateLineService(20, out var cubes);
        var thing = CreateMockEntity(new[] { "entity" }, cubes[0]);
        service.GetChunkAtTile(cubes[0]).AddToChunk(thing);

        // Same chunk 
        bool switch1 = service.AmISwitchingChunks(thing, cubes[0], cubes[1]);
        AssertThat(switch1).IsFalse();
        
        // Move extremely far - definitively a different chunk
        bool switch2 = service.AmISwitchingChunks(thing, cubes[1], cubes[19]);
        AssertThat(switch2).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_AreChunksAdjacent()
    {
        var service = CreateLineService(15, out var cubes);
        var chunk1 = service.GetChunkAtTile(cubes[0]);
        var chunk2 = service.GetChunkAtTile(cubes[14]);
        
        AssertThat(service.AreChunksAdjacent(chunk1, chunk2)).IsFalse();
        
        // Find structurally adjacent chunk dynamically
        PathfindingService.Chunk adjacentChunk = null;
        for(int i = 1; i < 15; i++) {
            var c = service.GetChunkAtTile(cubes[i]);
            if(c != chunk1) { adjacentChunk = c; break; }
        }
        
        AssertThat(adjacentChunk).IsNotNull();
        AssertThat(service.AreChunksAdjacent(chunk1, adjacentChunk)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_DoesChunkHaveTag()
    {
        var service = CreateLineService(2, out var cubes);
        var item = CreateMockEntity(new[] { "magic" }, cubes[0]);
        var chunk = service.GetChunkAtTile(cubes[0]);
        chunk.AddToChunk(item);

        AssertThat(chunk.DoesChunkHaveTag("magic")).IsTrue();
        AssertThat(chunk.DoesChunkHaveTag("mundane")).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_AreaAndChunkDestruction()
    {
        var service = CreateLineService(2, out var cubes);
        var area = service.GetAreaFromTile(cubes[0]);
        
        // Call Destroy safely handles cleanup without errors or crashes
        area.Destroy();
        AssertThat(true).IsTrue(); 
    }

    // ==========================================
    // 6. MATH & UTILITIES
    // ==========================================
    
    [TestCase]
    [RequireGodotRuntime]
    public void Test_AxialToCube_MathIsCorrect()
    {
        Vector3I resultCube = PathfindingService.AxialToCube(new Vector2I(1, 2));
        AssertThat(resultCube).IsEqual(new Vector3I(-3, 2, -1));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_CubeToAxial_MathIsCorrect()
    {
        Vector2I resultAxial = PathfindingService.CubeToAxial(new Vector3I(-3, 2, -1));
        AssertThat(resultAxial).IsEqual(new Vector2I(1, 2));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_OddQConversions_MathIsCorrect()
    {
        var oddQ = PathfindingService.CubeToOddQ(1, -2, 1);
        AssertThat(oddQ.X).IsEqual(1);
        AssertThat(oddQ.Y).IsEqual(-2); // oddY logic
        
        var oddQAxial = PathfindingService.FromAxialToOddQ(2, 3);
        AssertThat(oddQAxial.X).IsEqual(-1); 
        AssertThat(oddQAxial.Y).IsEqual(2); 
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_BigAndSmallHexConversions()
    {
        var service = AutoFree(new PathfindingService());
        
        var bigHex = new Vector2I(1, -1);
        var smallHex = service.BigHexToSmall(bigHex);
        var backToBig = service.SmallHexToBig(smallHex);
        
        AssertThat(backToBig).IsEqual(bigHex);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_DisableTile_NonExistent_LogsErrorAndDoesNotCrash()
    {
        var service = CreateLineService(1, out var cubes);
        
        // Pass a totally disconnected random ID that doesn't exist
        service.DisableTile(new Vector3I(100, 100, -200));
        
        // Assert true because Godot didn't crash
        AssertThat(true).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Test_HighlightTiles_DoesNotCrash()
    {
        var service = CreateLineService(1, out var cubes);
        
        // Highlight tiles calls down to mocked map_to_cube/get_cell etc
        service.HighlightTiles(cubes, 1);
        
        AssertThat(true).IsTrue();
    }
}