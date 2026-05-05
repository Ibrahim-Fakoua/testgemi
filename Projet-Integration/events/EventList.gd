extends Node

## A list that stores multiple events that are linked together in a way or another
class_name EventList

## The list of all events stored by the EventList
var events : Array[GenericEvent] = []

## Removes all the nulled events that have stopped existing from the EventList 
## [br][br] This is because when an event is removed from the simulation using queue_free() once it happens,
## its reference still exists anywhere it was stored, so the freed null elements need to be purged to
## avoid errors.
## [br][br] PRIVATE function
func cleanup_finished_events() -> void :
	for i in range(events.size() - 1, -1, -1) :
		if !is_instance_valid(events[i]) : 
			events.remove_at(i)

## Adds an event to the EventList in order of events' priority
func add_event(event:GenericEvent) -> void :
	cleanup_finished_events()
	if event.priority == EventEnums.Priority.FIRST : 
		events.push_front(event)
	elif event.priority == EventEnums.Priority.LAST : 
		events.append(event)
	else : 
		events.append(event)
		events.sort_custom(func(a, b): return a.priority < b.priority)

## Removes an event from the EventList
## TODO: Could be subject to change, because maybe events should be queue_freed here
func remove_event(event:GenericEvent) -> void : 
	events.erase(event)

## Returns an array of all eventds in the EventList
func get_events() -> Array[GenericEvent] :
	cleanup_finished_events()
	return events

## Returns the first event in the EventList which has the corresponding id
func find_by_id(id:String) -> GenericEvent : 
	cleanup_finished_events()
	for event in events : 
		if event.id == id :
			return event
	return null
