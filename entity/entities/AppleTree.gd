extends SpawnerEntity

class_name AppleTree

func _init(pathfinding, position:Vector3i) -> void :
	var spawn_cooldown = 700
	var spawned_class_filepath = "res://entity/entities/ApplePile.gd"
	super._init(pathfinding, position, spawn_cooldown, spawned_class_filepath)
	_initialize_sprite("res://assets/entities/AppleTree.png")
	type = "AppleTree"
	sprite.z_index = 2
