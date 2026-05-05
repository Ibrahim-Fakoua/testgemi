extends GenericCondition

class_name DoneBreeding

func _init() -> void:
	condition_type = CritterConst.ConditionTypes.DONE_BREEDING

func setup_condition_event(critter:Critter) -> void:
	pass
