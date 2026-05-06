extends TypedEvent

class_name HungerEvent

## The hunger percentage that the event tracks
var hunger_percentage: int

func _init(event:Callable, id:String, hunger_percentage:int, event_types:Array[String]=[], priority:EventEnums.Priority=EventEnums.Priority.NO_PRIORITY) -> void :
	if !event_types.has(EventEnums.EventType.HUNGER_TIMER) :
		event_types.append(EventEnums.EventType.HUNGER_TIMER)
	self.hunger_percentage = hunger_percentage
	if hunger_percentage >= 100 : 
		priority = EventEnums.Priority.LAST
	super._init(event, id, event_types, priority)
