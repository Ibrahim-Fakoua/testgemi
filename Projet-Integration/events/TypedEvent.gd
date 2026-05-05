extends GenericEvent

## An event that has different labels, called types, that indicate what it does clearly
class_name TypedEvent

## The labels that describe what kind of event the event is
var event_types: Array[String]

func _init(event:Callable, id:String, event_types:Array[String]=[], priority:EventEnums.Priority=EventEnums.Priority.NO_PRIORITY) -> void :
	super._init(event, id, priority)
	self.event_types = event_types
