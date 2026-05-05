extends TypedEvent

class_name FatigueEvent

var fatigue_percentage: int

func _init(event:Callable, id:String, fatigue_percentage:int, event_types:Array[String]=[], priority:EventEnums.Priority=EventEnums.Priority.NO_PRIORITY) -> void :
	if !event_types.has(EventEnums.EventType.FATIGUE_TIMER) :
		event_types.append(EventEnums.EventType.FATIGUE_TIMER)
	self.fatigue_percentage = fatigue_percentage
	if fatigue_percentage >= 100 :
		priority = EventEnums.Priority.LAST
	super._init(event, id, event_types, priority)
