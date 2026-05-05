extends GenericCondition

class_name FatigueUnder

var fatigue_percentage: int

func _init(fatigue_percentage:int) -> void :
	self.condition_type = CritterConst.ConditionTypes.FATIGUE_UNDER
	is_unique_condition = false
	self.fatigue_percentage = fatigue_percentage

func setup_condition_event(critter:Critter) -> void:
	critter.schedule_event(1, TypedEvent.new(func() : critter.behavior.on_fatigue_under(fatigue_percentage), "fatigueUnder" + str(fatigue_percentage), [EventEnums.EventType.STATE_EVENT]))
