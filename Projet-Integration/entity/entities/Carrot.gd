extends GenericFoodEntity

class_name Carrot

func _init(pathfinding, position: Vector3i) -> void :
	super._init(pathfinding, 2, position, DroppedFoodBehavior.new(self))
	self.tags.append("fruit")
	_initialize_sprite("res://assets/entities/Carrot.png")
	portion_food_content = 1500
	type = "Carrot"
