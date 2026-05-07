extends GenericCondition

class_name IsSafe

func _init() -> void :
	self.condition_type = CritterConst.ConditionTypes.SAFE

func setup_condition_event(critter:Critter) -> void :
	var possible_danger = critter.pathfinding.AmIInDanger(critter.position, critter.get_fightscore(), critter.get_morale())
	if possible_danger == null :
		critter.schedule_event(1, TypedEvent.new(func() : critter.behavior.on_become_safe(), "becameSafe", [EventEnums.EventType.STATE_EVENT]))
