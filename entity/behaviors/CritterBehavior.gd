extends GenericBehavior

class_name CritterBehavior

## Triggers the end of the critter's current action
func on_action_activated() -> void :
	entity.current_action.activate()

## Makes the critter switch state
func on_state_change(state:State) -> void :
	entity.cancel_events_of_type(EventEnums.EventType.STATE_EVENT)
	entity.current_state = state
	entity.current_state.setup_conditions()
	entity.change_action(entity.current_state.action_loop[0])

## Triggers itself when the critter reaches a specified hunger treshold
## [br][br] Causes the critter's death if the hunger treshold is 100 or more, except if the critter is doing an action that prevents starvation
func on_hunger_reached(hunger_percentage:int) -> void :
	if hunger_percentage >= 100 and entity.current_action.action_name == CritterConst.ActionNames.EAT : 
		entity.schedule_event(10, HungerEvent.new(func() : on_hunger_reached(100), "starvation", 100))
	elif hunger_percentage >= 100 :
		entity.schedule_event(1, TypedEvent.new(func() : on_death(), "death", [], EventEnums.Priority.LAST))
	entity.current_state.on_hunger_reached(hunger_percentage)

## Triggers itself when the critter checks if its hunger is under a certain amount
func on_hunger_under(current_hunger_percentage:int) -> void :
	entity.current_state.on_hunger_under(current_hunger_percentage)

func on_fatigue_reached(fatigue_percentage:int) -> void :
	if entity.current_action.action_name != CritterConst.ActionNames.SLEEP :
		if fatigue_percentage >= 100 :
			entity.schedule_event(1, TypedEvent.new(func() : on_death(), "death", [], EventEnums.Priority.LAST))
		entity.current_state.on_fatigue_reached(fatigue_percentage)

func on_fatigue_under(current_fatigue_percentage:int) -> void :
	entity.current_state.on_fatigue_under(current_fatigue_percentage)

func on_able_to_breed() -> void :
	entity.current_state.on_able_to_breed()

func on_done_breeding() -> void :
	entity.current_state.on_done_breeding()

func on_get_injured() -> void :
	if entity.injury_level >= 10 :
		entity.schedule_event(1, TypedEvent.new(func() : on_death(), "death", [], EventEnums.Priority.LAST))
	else :
		entity.current_state.on_get_injured()
		on_injury_over(entity.injury_level)

func on_injury_over(current_injury_level:int) :
	entity.current_state.on_injury_over(current_injury_level)

func on_sense_danger() :
	entity.current_state.on_sense_danger()

func on_become_safe() :
	entity.current_state.on_become_safe()

## Triggers itself once the critter arrives at the end of its lifespan
func on_lifespan_end() -> void :
	entity.schedule_event(1, TypedEvent.new(func() : on_death(), "death", [], EventEnums.Priority.LAST))

func on_death() -> void :
	if is_instance_valid(entity):
		entity.pathfinding.disable_tile(entity.position,false)
		var chunk_of_critter = entity.pathfinding.get_chunk_at_tile(entity.position)
		if chunk_of_critter : 
			entity.pathfinding.get_chunk_at_tile(entity.position).remove_from_chunk(entity)
		for event in entity.scheduled_events.get_events().duplicate() :
			entity.cancel_event(event.id)
			
		Corpse.new(entity.pathfinding, entity.position)
		var critter = entity as Critter
		#print("homie died... rip")
		DatabaseManager.RegisterCreatureDeath(critter.creature_id, (Controller.Scheduler as Scheduler).current_timestamp)
		entity.queue_free()
		
