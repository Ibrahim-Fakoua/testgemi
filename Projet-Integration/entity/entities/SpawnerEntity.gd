@abstract
extends GenericEntity

class_name SpawnerEntity

var spawn_cooldown: int
var entity_spawned: GDScript
var amount_per_spawn: int

func _init(pathfinding, position:Vector3i, spawn_cooldown:int, spawned_class_filepath:String, amount_per_spawn:int=1) -> void :
	super._init(pathfinding, position, SpawnerEntityBehavior.new(self), "spawner")
	self.spawn_cooldown = spawn_cooldown
	self.amount_per_spawn = amount_per_spawn
	entity_spawned = load(spawned_class_filepath)
	behavior.on_spawned()

func spawn() -> void :
	for i in range(amount_per_spawn) :
		var chosen_location: Vector3i
		var possible_spawn_locations = pathfinding.PossibleMoves(position)
		if possible_spawn_locations.size() != 0 :
			var random_location = randi() % possible_spawn_locations.size()
			chosen_location = possible_spawn_locations.get(random_location)
			entity_spawned.new(pathfinding, chosen_location)
	behavior.on_spawned()
