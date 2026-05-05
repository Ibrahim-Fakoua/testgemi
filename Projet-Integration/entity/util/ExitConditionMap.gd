extends Node

class_name ExitConditionMap

var all_conditions: Array[GenericCondition] = []
var condition_map: Dictionary[GenericCondition, State] = {}

func remove_condition(condition:GenericCondition) -> void :
	all_conditions.erase(condition)
	condition_map.erase(condition)

func get_state(condition:GenericCondition) -> State :
	return condition_map[condition]

func get_all_conditions() -> Array[GenericCondition] : 
	return all_conditions

func get_condition_of_type(condition_type: String) -> Array[GenericCondition] :
	var concerned_conditions : Array[GenericCondition]
	for condition in all_conditions : 
		if condition.condition_type == condition_type : 
			concerned_conditions.append(condition)
			if condition.is_unique_condition :
				return concerned_conditions
	
	return concerned_conditions

func add_state_change(condition:GenericCondition, state:State) : 
	if condition.is_unique_condition : 
		var same_condition = get_condition_of_type(condition.condition_type)
		if !same_condition.is_empty() :
			remove_condition(same_condition[0])
	
	all_conditions.append(condition)
	condition_map[condition] = state
