extends GenericAction

class_name Mate

## For DB purposes
var partner_id: int = -1

func _init(critter: Critter) -> void :
	super._init(critter, CritterConst.ActionNames.MATE, "res://assets/action_icons/WayTooLewd.png")

func startup() -> void :
	if critter.tags.has("male") :
		schedule_default_startup(CritterConst.BASE_MATING_SPEED)
	
	if critter.mate == null :
		end()

func activate() -> void :
	if critter.tags.has("male") and is_instance_valid(critter.mate):
		critter.mate.current_action.activate()
	end()

func end() -> void :
	if critter.tags.has("male") :
		critter.finish_mating()
	else :
		critter.bear_child()
	critter.mate = null
	super.end()

func register_action() -> void:
	if partner_id == -1:
		return		
	DatabaseManager.RegisterLogAction(critter.creature_id, action_name, str(partner_id), (Controller.Scheduler as Scheduler).current_timestamp)
	
