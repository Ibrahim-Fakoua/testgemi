extends GenericAction

class_name Eat

var food_pos: Vector3i

func _init(critter:Critter) -> void :
	super._init(critter, CritterConst.ActionNames.EAT, "res://assets/action_icons/EatCarnivore.png")

func startup() -> void :
	schedule_default_startup(critter.get_eating_speed())

func activate() -> void : 
	var possible_food
	if critter.tags.has("herbivore") :
		possible_food = critter.pathfinding.MoveToClosestThing(critter.position, ["food", "fruit"], ["empty"])
	elif critter.tags.has("carnivore") :
		possible_food = critter.pathfinding.MoveToClosestThing(critter.position, ["food", "meat"], ["empty"])
	else :
		possible_food = critter.pathfinding.MoveToClosestThing(critter.position, ["food"], ["empty"])
	if possible_food is GenericFoodEntity :
		food_pos = possible_food.position
		critter.eat(possible_food)
		if is_instance_valid(possible_food) and possible_food.ressource_stock != 0 and critter.current_action.action_name == CritterConst.ActionNames.EAT:
			startup()
			return
	end()

func register_action() -> void:
	DatabaseManager.RegisterLogAction(critter.creature_id, action_name, str(food_pos), (Controller.Scheduler as Scheduler).current_timestamp)
