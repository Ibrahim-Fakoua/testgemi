extends GenericCondition

class_name FatigueReached

var fatigue_percentage: int

func _init(fatigue_percentage:int) -> void :
	condition_type = CritterConst.ConditionTypes.FATIGUE_REACHED
	is_unique_condition = false
	self.fatigue_percentage = fatigue_percentage

func setup_condition_event(critter:Critter) -> void:
	critter.schedule_event(critter.get_time_until_fatigue(fatigue_percentage), FatigueEvent.new(func() : critter.behavior.on_fatigue_reached(fatigue_percentage), "fatigueTimer" + str(fatigue_percentage), fatigue_percentage, [EventEnums.EventType.STATE_EVENT]))
