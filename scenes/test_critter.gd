extends Node2D

var pathfinding_service : PathfindingService 

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	await get_tree().create_timer(2).timeout
	pathfinding_service = PathfindingService.new(get_node("TileMap/MainLayer"))
	test_function()

func test_function() -> void                                                                                                                                                                                                                                                                                                                                                             :
	var test_areas : Array[Vector2i] = pathfinding_service.designate_areas()
	pathfinding_service.create_areas(test_areas)
	pathfinding_service.fix_adjacency_for_chunks(test_areas)
	var test_critter = Critter.new(CritterAttributes.new(), Vector3i(25, 35, 100), pathfinding_service)
	add_child(test_critter)
