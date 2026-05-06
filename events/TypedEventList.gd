extends EventList

## A list that stores multiple TypedEvents that are linked together in a way or another
## [br][br] Subclass of EventList
class_name TypedEventList

## This stores the events of the list depending on which type or types they are
var ordered_events : Dictionary[String, Array] = {}

func _init() -> void :
	for event_type in EventEnums.EventType.keys() :
		ordered_events[event_type] = []

## Removes all nulled events from a specific array within ordered_events
## [br][br] PRIVATE function
func cleanup_ordered_events(event_type:String) -> void :
	var typed_events = ordered_events[event_type].duplicate()
	for event in typed_events :
		if !is_instance_valid(event) : 
			ordered_events[event_type].erase(event)

## Adds an event to the array of events in order of events' priority and adds it in the corresponding keys of ordered_events
func add_event(event:GenericEvent) -> void :
	super.add_event(event)
	for event_type in event.event_types : 
		ordered_events[event_type].append(event)

## Removes an event from the array of events and removes it from any key of ordered_events it was stored in
func remove_event(event:GenericEvent) -> void :
	super.remove_event(event)
	for event_type in event.event_types :
		cleanup_ordered_events(event_type)
		ordered_events[event_type].erase(event)

## Removes all events of a certain type from the TypedEventList
func remove_events_of_type(selected_event_type:String) -> void :
	cleanup_ordered_events(selected_event_type)
	for event in ordered_events[selected_event_type].duplicate() :
		remove_event(event)

## Returns all events of a certain type in the TypedEventList
func get_events_of_type(event_type:String) -> Array[GenericEvent] :
	cleanup_ordered_events(event_type)
	var events_of_type: Array[GenericEvent] = []
	for event in ordered_events[event_type] :
		events_of_type.append(event)
	return events_of_type
