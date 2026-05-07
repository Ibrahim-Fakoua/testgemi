extends GenericAction

class_name Flee

func _init(critter:Critter) -> void :
	super._init(critter, CritterConst.ActionNames.FLEE, "res://assets/action_icons/RunAway.png")

func startup() -> void:
	schedule_default_startup(critter.get_mov_speed() * CritterConst.FLEEING_SPEED_BOOST)

func activate() -> void:
	var possible_danger = critter.pathfinding.AmIInDanger(critter.position, critter.get_fightscore(), critter.get_morale(), [critter.get_species()])
	if possible_danger :
		startup()
		critter.move(critter.pathfinding.MoveAwayFrom(critter.position, possible_danger))
	else :
		end()

func end() -> void :
	super.end()
	critter.behavior.on_become_safe()

func register_action() -> void:
	DatabaseManager.RegisterLogAction(critter.creature_id, action_name, "", (Controller.Scheduler as Scheduler).current_timestamp)
