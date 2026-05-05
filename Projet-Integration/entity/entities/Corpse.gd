extends GenericFoodEntity

class_name Corpse

func _init(pathfinding, position: Vector3i) -> void :
	super._init(pathfinding, 2, position, DroppedFoodBehavior.new(self))
	self.tags.append("meat")
	_initialize_sprite("res://assets/entities/Corpse.png")
	portion_food_content = 1500
