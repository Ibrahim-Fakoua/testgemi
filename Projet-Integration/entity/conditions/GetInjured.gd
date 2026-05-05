extends GenericCondition

class_name GetInjured

func _init() -> void :
	self.condition_type = CritterConst.ConditionTypes.GET_INJURED

func setup_condition_event(critter:Critter) -> void:
	pass
