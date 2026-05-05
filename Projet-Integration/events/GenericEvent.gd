extends Node

## A thing that happens at a certain moment inside the simulation [br]
## It is always executed by the Scheduler of the simulation
class_name GenericEvent

## The time unit at which the event is stored in the scheduler
var timestamp: int = -1
## The code to be executed once the event comes to happen
var event: Callable 
# Subject to deletion, mainly only useful for debugging
## An id to identify the event
var id: String
## Determines the order in which the event is executed if it is scheduled at the same timestamps as others
var priority: EventEnums.Priority

func _init(event:Callable, id:String, priority:EventEnums.Priority=EventEnums.Priority.NO_PRIORITY) -> void :
	self.event = event
	self.id = id
	self.priority = priority

## Method that makes the event happen, executes the code in the stored Callable
func happen() -> void :
	event.call()
	self.queue_free()
