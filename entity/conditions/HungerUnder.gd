extends GenericCondition

## This condition checks if a critter's hunger is under a certain percentage
## [br] To do so, it checks immediately upon starting to exist and then the condition gets checked whenever relevant
class_name HungerUnder

## The hunger percentage that the condition tracks
var hunger_percentage: int

func _init(hunger_percentage:int) -> void :
	self.condition_type = CritterConst.ConditionTypes.HUNGER_UNDER
	is_unique_condition = false
	self.hunger_percentage = hunger_percentage

func setup_condition_event(critter:Critter) -> void:
	critter.schedule_event(1, TypedEvent.new(func() : critter.behavior.on_hunger_under(critter.get_hunger_percentage()), "hungerUnder" + str(hunger_percentage), [EventEnums.EventType.STATE_EVENT]))
