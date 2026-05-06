extends GenericCondition

class_name IsInjured

var injury_level: int

func _init(injury_level: int) -> void :
	self.condition_type = CritterConst.ConditionTypes.IS_INJURED
	is_unique_condition = false
	self.injury_level = injury_level

func setup_condition_event(critter:Critter) -> void:
	critter.schedule_event(1, TypedEvent.new(func() : critter.behavior.on_injury_over(critter.injury_level), "isInjured" + str(injury_level), [EventEnums.EventType.STATE_EVENT]))
