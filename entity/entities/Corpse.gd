extends GenericFoodEntity

class_name Corpse

var decay_rate:int = 300
var rotten_sprite:String = "res://assets/entities/Corpse(Rotten).png"

func _init(pathfinding:PathfindingService, position: Vector3i) -> void :
	super._init(pathfinding, 2, position, CorpseBehavior.new(self))
	self.tags.append("meat")
	_initialize_sprite("res://assets/entities/Corpse.png")
	portion_food_content = 1500
	schedule_event(decay_rate, TypedEvent.new(func() : behavior.on_rot(), "rotting"))
