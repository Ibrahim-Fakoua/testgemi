extends GenericEntity

## An entity incapable of movement that contains water or food
class_name GenericFoodEntity

## Quantity of ressource still in the entity
var ressource_stock: int
## How much each portion of the ressource fills up hunger
var portion_food_content: int = 1000
## Maximum quantity of ressources in the entity
var max_ressource_capacity: int

func _init(pathfinding, max_ressource_capacity:int, position: Vector3i, behavior: GenericFoodEntityBehavior) -> void :
	super._init(pathfinding, position, behavior, "food")
	self.max_ressource_capacity = max_ressource_capacity
	ressource_stock = max_ressource_capacity
	self.type = "FoodEntity"
