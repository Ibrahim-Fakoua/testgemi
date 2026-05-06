extends GenericFoodEntity

class_name ApplePile

func _init(pathfinding:PathfindingService, position: Vector3i) -> void :
	super._init(pathfinding, 2, position, DroppedFoodBehavior.new(self))
	self.tags.append("fruit")
	_initialize_sprite("res://assets/entities/ApplePile.png")
	portion_food_content = 1500
	type = "ApplePile"
