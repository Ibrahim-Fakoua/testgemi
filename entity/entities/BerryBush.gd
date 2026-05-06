extends ReplinishingFoodSource

class_name BerryBush

func _init(pathfinding:PathfindingService, position:Vector3i) -> void :
	var max_ressource_capacity = 5
	var regeneration_time = 400
	super._init(pathfinding, position, 5, 400, "res://assets/entities/BerryBush.png", "res://assets/entities/Bush.png")
	self.tags.append("bush")
	self.tags.append("fruit")
	self.portion_food_content = 1500
	type = "BerryBush"
