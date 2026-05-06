@abstract
extends Node

## ABSTRACT CLASS - The blueprint of an action a critter can do
class_name GenericAction

## The critter doing the action
## [br][br] This is necessary to make the action aware of which critter is using it
var critter: Critter
## The name of the action being executed. For debugging reasons mostly
## [br][br] !!! This variable MUST be defined in each action's _init() !!!
## [br] Due to Godot enums being kinda stupid, the typing of this is only String.
## However, this variable MUST contain a value gotten from CritterConst.ActionNames
var action_name: String
## The filepath of the action sprite that is displayed when this action is the critter's current action
## [br][br] !!! This variable MUST be defined in each action's _init() !!!
## [br] Action sprites are found in res://assets/action_icons
var action_sprite_filepath: String

## Has to be called in the child's _init()
func _init(critter:Critter, action_name: String, action_sprite_filepath: String) :
	self.critter = critter
	self.action_name = action_name
	self.action_sprite_filepath = action_sprite_filepath

## Schedules an event in the scheduler
## [br][br] ONLY USE THIS METHOD TO SCHEDULE EVENTS WHEN AN ACTION SCHEDULES AN EVENT
## DO NOT USE THE VERSION OF THE METHOD IN Scheduler.gd OR GenericEntity.gd
func schedule_action_event(timestamp:int, event:Callable, event_id:String, event_types:Array[String]=[], priority:EventEnums.Priority=EventEnums.Priority.NO_PRIORITY) -> void :
	event_types.append(EventEnums.EventType.ACTION_EVENT)
	critter.schedule_event(timestamp, TypedEvent.new(event, event_id, event_types, priority))

## Schedules the default startup event to the scheduler at the precised timestamp
## [br][br] Made as a shortcut for actions with a simplistic startup() for readability
func schedule_default_startup(timestamp:int) -> void :
	schedule_action_event(timestamp, func() : critter.behavior.on_action_activated(), "Action \"" + action_name + "\" activation")

@abstract
## Schedules the action_activate() event at the right timestamp depending on the situation.
## [br][br] Also checks any conditions for the action when it applies, and changes the critter's action sprite
func startup() -> void

@abstract
## Makes the critter do the action in question
## [br][br] MUST ALWAYS CALL end() or startup() AT THE END OF ITS EXECUTION
## [br][br] If the critter achieved its goal, call end()
## [br] If the critter has not achieved its goal, call startup() 
func activate() -> void

## Checks if the critter has achieved the goal of the action
func end() -> void : 
	register_action()
	#print("Critter" + str(critter.creature_id) + " finished action : " + action_name)
	critter.next_action()
	
@abstract
## Registers the action's completion to the DB. 
## This makes event tracking possible. 
## EVERY registered action must include the following values :
## 	- ActorId (where the Id is either the Id of the Critter OR 0, which is the user / simulation ID.)
## 	- CurrentTick (the tick where the action is RESOLVED or COMPLETED. This aligns with the above end() method.)
func register_action() -> void
	
