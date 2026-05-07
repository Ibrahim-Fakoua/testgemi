extends Node2D

const PathfindingServiceRef = preload("res://scripts/autoload/PathfindingService.cs")
var pathfinding_service

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	await get_tree().create_timer(2).timeout
	pathfinding_service = PathfindingServiceRef.new()
	pathfinding_service.Initialize(get_node("TileMap/MainLayer"))
	test_function()

func test_function() -> void:
	pathfinding_service.CreatePathfinding()
	var test_critter = Critter.new(CritterAttributes.new(), Vector3i(25, 35, 100), pathfinding_service)
	add_child(test_critter)
