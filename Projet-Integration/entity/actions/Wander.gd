extends GenericAction

class_name Wander

func _init(_critter:Critter) -> void :
	super._init(_critter, CritterConst.ActionNames.WANDER, "res://assets/action_icons/Wander.png")

func startup() -> void :
	var wandering_delay: int = critter.get_mov_speed() * randf_range(1.0, 3.0)
	schedule_default_startup(wandering_delay)

func activate() -> void :
	var possible_moves : Array = critter.pathfinding.possible_moves(critter.position)
	if possible_moves.size() == 0:
		possible_moves.append(critter.position)
	var random_direction = randi() % possible_moves.size()
	var destination_pos = possible_moves.get(random_direction)
	startup()
	critter.move(destination_pos)
	
func register_action() -> void:
	DatabaseManager.RegisterLogAction(critter.creature_id, action_name, str(critter.position), (Controller.Scheduler as Scheduler).current_timestamp)
