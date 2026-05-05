extends GenericCondition

class_name SenseDanger

func _init() -> void :
	self.condition_type = CritterConst.ConditionTypes.SENSE_DANGER

func setup_condition_event(critter:Critter) -> void:
	var possible_danger = critter.pathfinding.AmIInDanger(critter.position, critter.get_fightscore(), critter.get_morale())
	if possible_danger is Vector3i :
		critter.schedule_event(1, TypedEvent.new(func() : critter.behavior.on_sense_danger(), "sensedDanger", [EventEnums.EventType.STATE_EVENT]))
