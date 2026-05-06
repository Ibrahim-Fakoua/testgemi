@abstract
extends Node

## An object that represents what a creature needs to achieve to go from one state to another
class_name GenericCondition

## The type of condition this is. For debugging purposes mainly, probably removable somehow
var condition_type: String
## This tells whether the condition only depends on checking if an event happens (true) or it needs to also check an additional parameter once the event happens (false) 
var is_unique_condition:bool = true

@abstract
## Schedules any event necessary to be scheduled to trigger the condition
func setup_condition_event(critter:Critter) -> void
