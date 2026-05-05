extends Node

## A "state" a critter can be in.
## [br] A critter's state determines what actions they repeatedly do and determines what triggers causes them to enter another state
class_name State

## The critter who is in this state currently
var critter:Critter
## A dictionary that links each exit condition of the state with which state it exits into.
## In other words, when the condition key is achieved, the critter enters the state in the value.
var exit_conditions: ExitConditionMap = ExitConditionMap.new()
## The actions the critter will repeatedly do when it is in this state
var action_loop: Array[GenericAction]
## The index of the current action the critter is in
var current_action_index: int = 0

func _init(action_loop:Array[GenericAction], critter:Critter) -> void :
	self.action_loop = action_loop
	self.critter = critter

func setup_conditions() -> void :
	for condition in exit_conditions.get_all_conditions() :
		condition.setup_condition_event(critter)

func add_state_change(condition:GenericCondition, state:State) -> void :
	exit_conditions.add_state_change(condition, state)

func check_exit_conditions(condition_name:String, comparison:Callable=Callable()) -> void :
	var concerned_conditions: Array[GenericCondition] = exit_conditions.get_condition_of_type(condition_name)
	
	if comparison.is_valid() :
		for condition in concerned_conditions :
			#print(comparison.call(condition))
			if comparison.call(condition) :
				change_state(exit_conditions.get_state(condition))
				return
	else :
		if !concerned_conditions.is_empty() :
			change_state(exit_conditions.get_state(concerned_conditions[0]))

func change_state(state:State) -> void :
	if critter.current_action.action_name == CritterConst.ActionNames.SLEEP : 
		if !critter.current_action.woke_up_by_itself :
			critter.current_action.recover_fatigue(true)
		critter.current_action.woke_up_by_itself = false
	critter.behavior.on_state_change(state)

func next_action() -> GenericAction :
	current_action_index = (current_action_index + 1) % len(action_loop)
	return action_loop[current_action_index]

func on_hunger_reached(hunger_percentage:int) -> void :
	check_exit_conditions(CritterConst.ConditionTypes.HUNGER_REACHED, func(condition) : return condition.hunger_percentage == hunger_percentage)

func on_hunger_under(hunger_percentage:int) -> void :
	check_exit_conditions(CritterConst.ConditionTypes.HUNGER_UNDER, func(condition) : return hunger_percentage <= condition.hunger_percentage)

func on_fatigue_reached(fatigue_percentage:int) -> void :
	check_exit_conditions(CritterConst.ConditionTypes.FATIGUE_REACHED, func(condition) : return condition.fatigue_percentage == fatigue_percentage)

func on_fatigue_under(fatigue_percentage:int) -> void :
	check_exit_conditions(CritterConst.ConditionTypes.FATIGUE_UNDER, func(condition) : return fatigue_percentage < condition.fatigue_percentage)

func on_able_to_breed() -> void :
	check_exit_conditions(CritterConst.ConditionTypes.CAN_BREED)

func on_done_breeding() -> void :
	check_exit_conditions(CritterConst.ConditionTypes.DONE_BREEDING)

func on_get_injured() -> void :
	check_exit_conditions(CritterConst.ConditionTypes.GET_INJURED)

func on_injury_over(current_injury_level:int) -> void :
	check_exit_conditions(CritterConst.ConditionTypes.IS_INJURED, func(condition) : return current_injury_level >= condition.injury_level)

func on_sense_danger() -> void :
	check_exit_conditions(CritterConst.ConditionTypes.SENSE_DANGER)

func on_become_safe() -> void :
	check_exit_conditions(CritterConst.ConditionTypes.SAFE)
