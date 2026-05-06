extends Node

## The class that is responsible for running the simulation
## [br][br] It stores all in-simulation events and keeps track of which tick (timestamp) they are meant to happen
## Keeps track of the passage of time and executes all event meant to happen at the current tick (timestamp) 
class_name Scheduler

## The amount of timestamps per in-simulation minute
const TIMESTAMPS_PER_MINUTE = 2
## The amount of timestamps per in-simulation day 
## TIMESTAMPS_PER_MINUTE * 1440
const TIMESTAMPS_PER_DAY = 2880

## The simulation schedule
## It stores every event that is set to happen in the simulation and the moment at which the event is meant to happen (That moment is called timestamp)
var _schedule : Dictionary[int, EventList] = {}
## The current in-simulation time. This attribute is incremented by 1 every tick.
var current_timestamp: int = 0
## An optional setting that makes the passage of time irregular but faster by skipping all timestamps where no event is scheduled at all
var _time_jumps : bool = false

var canceled_events : Array[GenericEvent]

## Adds an event in the scheduler dictionary.
## PRIVATE function - for internal use by the Scheduler only.
func _add_event(timestamp:int, event:GenericEvent) -> void :
	if !_schedule.has(timestamp) :
		_schedule[timestamp] = EventList.new()
	
	_schedule[timestamp].add_event(event)

## Call to schedule an event in a set amount of timestamps relative to the current timestamp.
## Setting is_time_difference to false makes the event get scheduled at the exact moment specified in the timestamp argument.
## If an event gets scheduled at an impossible moment (the past), it simply does not get scheduled.
## Returns a boolean that is false if the event scheduling was impossible.
func schedule_event(timestamp:int, event:GenericEvent, is_time_difference:bool = true) -> void :
	var scheduling_timestamp = timestamp
	if scheduling_timestamp < 1 :
		scheduling_timestamp = 1
	if is_time_difference : 
		scheduling_timestamp += current_timestamp
	
	if scheduling_timestamp > current_timestamp : 
		event.timestamp = scheduling_timestamp
		_add_event(scheduling_timestamp, event)
	else :
		event.timestamp = current_timestamp + 1
		_add_event(event.timestamp, event)

## Cancels the event it recieves in argument.
## Canceling removes the event completely from the scheduler, preventing it from ever happening
func cancel_event(event:GenericEvent) -> void :
	if _schedule.keys().has(event.timestamp) :
		var targeted_event_index = _schedule[event.timestamp].get_events().find(event)
		if targeted_event_index != -1 :
			canceled_events.append(_schedule[event.timestamp].get_events().get(targeted_event_index))
			_schedule[event.timestamp].get_events().remove_at(targeted_event_index)

## Reschedules an already scheduled event to a new timestamp.
## is_time_difference determines if the new_timestamp is something that needs to be added to the event's previous timestamp or an exact time.
## new_timestamp can be a negative number to make an event happen earlier than its current timestamp if is_time_difference is true
## If the new timestamp ends up being a timestamp smaller than the current timestamp, it gets set to the next tick
func reschedule_event(new_timestamp, event, is_time_difference:bool = true) -> void : 
	cancel_event(event)
	
	if is_time_difference : 
		schedule_event(event.timestamp + new_timestamp, event, false)
	else : 
		schedule_event(new_timestamp, event, false)


## Passes time and executes all events at the current timestamp.
## Subject to change, because the if else statement for the time jumps option makes it maybe unoptimized
func pass_time(stuff) -> void :
	if !_time_jumps : 
		current_timestamp += 1
		
		if _schedule.has(current_timestamp) :
			for event in _schedule[current_timestamp].get_events().duplicate() :
				if !canceled_events.has(event) :
					event.happen()
			
			canceled_events = []
			_schedule.erase(current_timestamp)

	else :
		while !_schedule.has(current_timestamp) :
			current_timestamp += 1
		
		for event in _schedule[current_timestamp].get_events() :
			event.happen()
		
		_schedule.erase(current_timestamp)

func spawn_entity(entity: GenericEntity) : 
	var new_entity = entity

func let_there_be_creaturas(_stuff) :
	var genesis = func() :
		var positions : Array[Vector3i] = [
			Vector3i(0, 18, -18),
			Vector3i(-3, 19, -16),
			Vector3i(-3, 21, -18),
			Vector3i(-6, 24, -18),
			Vector3i(3, 21, -24),
			Vector3i(0, 23, -23),
			Vector3i(-8, 14, -6),
			Vector3i(-8, 19, -11),
			Vector3i(-1, 21, -20),
			Vector3i(-3, 23, -20),
			Vector3i(-8, 17, -9),
			Vector3i(-11, 22, -11)
			]
		
		#Critter.new(SpecieData.Species.INVASIVE_SPECIE_2,positions.get(0), (Controller.WorldManager as WorldManager).pathfinding)
		#Critter.new(SpecieData.Species.INVASIVE_SPECIE_2,positions.get(1), (Controller.WorldManager as WorldManager).pathfinding)
		#Critter.new(SpecieData.Species.INVASIVE_SPECIE_2,positions.get(5) , (Controller.WorldManager as WorldManager).pathfinding)
		#Critter.new(SpecieData.Species.INVASIVE_SPECIE_2,positions.get(6), (Controller.WorldManager as WorldManager).pathfinding)
		#Carrot.new((Controller.WorldManager as WorldManager).pathfinding,positions.get(2))
		#Carrot.new((Controller.WorldManager as WorldManager).pathfinding,positions.get(3))
		#Carrot.new((Controller.WorldManager as WorldManager).pathfinding,positions.get(4))
		#Carrot.new((Controller.WorldManager as WorldManager).pathfinding,positions.get(9))
		#Carrot.new((Controller.WorldManager as WorldManager).pathfinding,positions.get(10))
		#Carrot.new((Controller.WorldManager as WorldManager).pathfinding,positions.get(11))
	#schedule_event(10, GenericEvent.new(genesis, "genesis"))
	# schedule_event(10, GenericEvent.new(genesis, "genesis"))



func _ready():
	Controller.GDSubscribe("SchedulerSignal", pass_time)
	Controller.GDSubscribe("Terrarium_Virtuel.signals.database.SimulationRegisteredSignal", let_there_be_creaturas)
	## Controller.GDSubscribe("Terrarium_Virtuel.signals.action_items.ActionItemExecutedSignal", _handle_action)
	
	
