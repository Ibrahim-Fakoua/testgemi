extends GenericCondition

## This condition checks if a critter's hunger reaches a certain level
## [br] To do so, it schedules a hunger event in a certain amount of time upon starting to exist
class_name HungerReached

## The hunger percentage that the condition tracks
var hunger_percentage: int

func _init(hunger_percentage:int) -> void :
	condition_type = CritterConst.ConditionTypes.HUNGER_REACHED
	is_unique_condition = false
	self.hunger_percentage = hunger_percentage

func setup_condition_event(critter:Critter) -> void:
	critter.schedule_event(critter.get_time_until_hunger(hunger_percentage), HungerEvent.new(func() : critter.behavior.on_hunger_reached(hunger_percentage), "hungerTimer" + str(hunger_percentage), hunger_percentage, [EventEnums.EventType.STATE_EVENT]))
