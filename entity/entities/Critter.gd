extends GenericEntity

##An entity possessing an evolving finite state machine that affects its behavior. The main entity of the simulation
##[br][br] Many variables, like hunger or sleepiness, are not stored in variables and instead calculated on the fly when needed.
class_name Critter

## List of all the states the critter can enter
var finite_state_machine: Array[State]
## The attributes that impact the critter's physical capabilities that are hereditary and stay constant through its entire life
var attributes: CritterAttributes
## The current state the critter is in
var current_state: State
## The current action the critter is doing
var current_action: GenericAction
## Sprite that shows which action the critter is doing visually
var action_sprite: Sprite2D
var gender_sprite: Sprite2D
var pregnancy_sprite: Sprite2D
var injury_sprite: Sprite2D
var mate: Critter = null
var injury_level: int = 0

## Generic Entity --- Identity
var creature_id: int
var species_id: int = 0
var parent_species_id: int = 0
var is_founder: bool = false


var specie: SpecieData.Species

func _init(specie:SpecieData.Species, position:Vector3i, pathfinding, litter_size:int=1, db_species_id: int = 0) -> void :

	super._init(pathfinding, position, CritterBehavior.new(self), "critter")
	self.specie = specie
	self.tags.append(SpecieData.Species.keys()[specie])
	self.finite_state_machine = finite_state_machine
	self.pathfinding = pathfinding
	creature_id = SpecieData.get_critter_id()
	_initialize_attributes(specie)
	_initialize_tags()
	_initialize_sprite(SpecieData.SpecieSprite[specie])
	sprite.z_index = 1
	_initialize_action_sprite()
	_initialize_default_state_machine(specie)

	self.species_id = db_species_id
	_schedule_startup_events(litter_size)
  
	register_with_database()
	
	type = "Critter"

func _initialize_attributes(specie:SpecieData.Species) -> void : 
	self.attributes = CritterAttributes.new()
	SpecieData.SpecieAttributes[specie].call(attributes)

func register_with_database():
	var loop_node = get_tree().root.find_child("GameLoop", true, false)
	
	if not loop_node:
		push_error("[Critter] Critical: GameLoop not found in scene tree!")
		return
	
	var current_tick = loop_node.TicksPassed
	var db_species_id = DatabaseManager.GetSpeciesDbId(int(specie))
	var handshake = Callable(self, "_on_creature_registered")
	DatabaseManager.RegisterCreature(handshake, 0, current_tick, db_species_id)
	

func _on_species_registered(new_spe_id: int):
	self.species_id = new_spe_id
	#print("Species Registered with ID: ", new_spe_id)
	
	var loop_node = get_tree().root.find_child("GameLoop", true, false)
	
	# NOW that the species exists, register the creature itself!
	var current_tick = loop_node.TicksPassed
	var handshake = Callable(self, "_on_creature_registered")
	DatabaseManager.RegisterCreature(handshake, 0, current_tick, self.species_id)


func _on_creature_registered(new_cre_id: int):
	self.creature_id = new_cre_id
	#print("Creature Registered with ID: ", self.creature_id, " under Species: ", self.species_id)


## Initializes the action sprite to be displayed appropriately
func _initialize_action_sprite() -> void :
	action_sprite = Sprite2D.new()
	action_sprite.apply_scale(Vector2(CritterConst.SPRITE_SCALE, CritterConst.SPRITE_SCALE))
	action_sprite.offset = Vector2(CritterConst.SPRITE_X_OFFSET, CritterConst.SPRITE_Y_OFFSET)
	action_sprite.position = pathfinding.Tilemap.map_to_local(pathfinding.Tilemap.cube_to_map(position))
	action_sprite.z_index = 3
	add_child(action_sprite)
	
	gender_sprite = Sprite2D.new()
	gender_sprite.apply_scale(Vector2(CritterConst.SPRITE_SCALE, CritterConst.SPRITE_SCALE))
	gender_sprite.offset = Vector2(CritterConst.SPRITE_X_OFFSET, CritterConst.SPRITE_Y_OFFSET)
	gender_sprite.position = pathfinding.Tilemap.map_to_local(pathfinding.Tilemap.cube_to_map(position))
	gender_sprite.z_index = 3
	add_child(gender_sprite)
	
	var gender_texture:Texture2D
	if tags.has("male") :
		gender_texture = load("res://assets/status_icons/CritterMale.png")
	else :
		gender_texture = load("res://assets/status_icons/CritterFemale.png")
	gender_sprite.texture = gender_texture
	
	pregnancy_sprite = Sprite2D.new()
	pregnancy_sprite.apply_scale(Vector2(CritterConst.SPRITE_SCALE, CritterConst.SPRITE_SCALE))
	pregnancy_sprite.offset = Vector2(CritterConst.SPRITE_X_OFFSET, CritterConst.SPRITE_Y_OFFSET)
	pregnancy_sprite.position = pathfinding.Tilemap.map_to_local(pathfinding.Tilemap.cube_to_map(position))
	pregnancy_sprite.z_index = 3
	add_child(pregnancy_sprite)
	
	injury_sprite = Sprite2D.new()
	injury_sprite.apply_scale(Vector2(CritterConst.SPRITE_SCALE, CritterConst.SPRITE_SCALE))
	injury_sprite.offset = Vector2(CritterConst.SPRITE_X_OFFSET, CritterConst.SPRITE_Y_OFFSET)
	injury_sprite.position = pathfinding.Tilemap.map_to_local(pathfinding.Tilemap.cube_to_map(position))
	injury_sprite.z_index = 3
	add_child(injury_sprite)

## Schedules the core events any critter has and depends on for functionning.
## [br][br] For now, the events for starvation and the end of lifespan
func _schedule_startup_events(litter_size:int) -> void :
	var lifespan = CritterConst.BASE_LIFESPAN * attributes.lifespan_modifier
	schedule_event(lifespan, TypedEvent.new(func() : behavior.on_lifespan_end(), "lifespanEnd"))
	
	var starting_food_percentage:float = (1.0 / 5.0 / (litter_size as float)) + 0.1
	var starvation_time = floor(CritterConst.BASE_STARVATION_TIME * attributes.hunger_rate * starting_food_percentage )
	schedule_event(starvation_time, HungerEvent.new(func() : behavior.on_hunger_reached(100), "starvation", 100))
	
	var coma_time = floor(CritterConst.BASE_SLEEP_DEPRIVATION_TIME * attributes.fatigue_rate)
	schedule_event(coma_time, FatigueEvent.new(func() : behavior.on_fatigue_reached(100), "coma", 100))
	
	var time_to_adulthood = floor(get_mating_cooldown() * 4)
	schedule_event(time_to_adulthood, TypedEvent.new(func() : set_can_mate(true), "reachAdulthood"))

## Initializes the base state machine all critters' behavior is based on and makes critters enter wandering mode
func _initialize_default_state_machine(specie:SpecieData.Species) -> void :
	var wandering = State.new([Wander.new(self)], self)
	var seeking_food = State.new([MoveToFood.new(self), Eat.new(self)], self)
	var seeking_love = State.new([MoveToMate.new(self), Mate.new(self)], self)
	var sleeping = State.new([Sleep.new(self)], self)
	var hunting = State.new([Hunt.new(self), Eat.new(self)], self)
	var fleeing = State.new([Flee.new(self)], self)
	
	if tags.has("forager") :
		add_state_change(wandering, HungerReached.new(50), seeking_food)
		add_state_change(seeking_food, HungerUnder.new(10), wandering)
		add_state_change(sleeping, HungerReached.new(50), seeking_food)
	elif tags.has("predator") :
		add_state_change(wandering, HungerReached.new(50), hunting)
		add_state_change(hunting, HungerUnder.new(10), wandering)
		add_state_change(hunting, IsInjured.new(6), seeking_food)
		add_state_change(sleeping, HungerReached.new(50), hunting)
	else :
		add_state_change(wandering, HungerReached.new(50), seeking_food)
		add_state_change(seeking_food, HungerUnder.new(10), wandering)
		add_state_change(sleeping, HungerReached.new(50), seeking_food)
	
	add_state_change(wandering, FatigueReached.new(30), sleeping)
	add_state_change(wandering, CanBreed.new(), seeking_love)
	
	add_state_change(sleeping, FatigueUnder.new(30), wandering)
	
	add_state_change(seeking_love, HungerReached.new(70), seeking_food)
	add_state_change(seeking_love, FatigueReached.new(30), sleeping)
	add_state_change(seeking_love, DoneBreeding.new(), wandering)
	
	add_state_change(wandering, SenseDanger.new(), fleeing)
	add_state_change(seeking_food, SenseDanger.new(), fleeing)
	add_state_change(seeking_love, SenseDanger.new(), fleeing)
	add_state_change(hunting, SenseDanger.new(), fleeing)
	
	add_state_change(wandering, GetInjured.new(), fleeing)
	add_state_change(seeking_food, GetInjured.new(), fleeing)
	add_state_change(seeking_love, GetInjured.new(), fleeing)
	add_state_change(sleeping, GetInjured.new(), fleeing)
	
	add_state_change(fleeing, IsSafe.new(), wandering)
	
	schedule_event(1, TypedEvent.new(func() : behavior.on_state_change(wandering), "startupState"))

func _initialize_tags() -> void:
	var gender = randi() % 2
	match gender :
		0:
			tags.append("male")
		1:
			tags.append("female")
	
	tags.append("foodChain" + str(attributes.food_chain))
	for tag in SpecieData.SpecieTags[specie] :
		tags.append(tag)

func add_state_change(start_state:State, condition:GenericCondition, end_state:State) -> void :
	for state in [start_state, end_state] :
		if !finite_state_machine.has(start_state) :
			finite_state_machine.append(start_state)
	start_state.add_state_change(condition, end_state)
	

## Changes the current action sprite to a new one designated by the file path provided in argument
func change_action_sprite(filepath:String) -> void :
	var texture: Texture2D = load(filepath)
	action_sprite.texture = texture

func update_injury_sprite() -> void :
	var texture: Texture2D = null
	var injury_sprite_id:int = ceil((injury_level as float)/3)
	if injury_sprite_id > 3 : injury_sprite_id = 3
	texture = load("res://assets/status_icons/Injury" + str(injury_sprite_id) + ".png")
	injury_sprite.texture = texture

## Returns a percentage representing how hungry the creature is
## [br][br] The percentage is calculated by comparing how much time it takes to starve when full to
## how much time there is left before the critter starve at the moment the method is called
## [br][br] 0 means it is not hungry at all, 100 means it is dead of hunger
func get_hunger_percentage() -> float :
	var total_time_of_starvation = CritterConst.BASE_STARVATION_TIME * attributes.hunger_rate
	var time_until_starvation = 0
	if is_instance_valid(scheduled_events.find_by_id("starvation")) :
		time_until_starvation = scheduled_events.find_by_id("starvation").timestamp - (Controller.Scheduler as Scheduler).current_timestamp
	return (1 - time_until_starvation / total_time_of_starvation) * 100

## Returns in how many scheduler timestamps the critter will reach the percentage of hunger provided in argument
## [br][br] Returns a negative number if the percentage has already been reached
func get_time_until_hunger(percentage:int) -> int :
	var total_time_of_starvation = CritterConst.BASE_STARVATION_TIME * attributes.hunger_rate
	var time_until_starvation = 0
	if is_instance_valid(scheduled_events.find_by_id("starvation")) :
		time_until_starvation = scheduled_events.find_by_id("starvation").timestamp - (Controller.Scheduler as Scheduler).current_timestamp
	var duration_of_percentage: int = total_time_of_starvation * percentage / 100
	var time_until_percentage: int = duration_of_percentage - (total_time_of_starvation - time_until_starvation)
	return time_until_percentage

## Returns whether or not the creature's hugner is under the percentage provided in argument
func check_hunger_under(hunger_percentage:int) -> bool :
	var total_time_of_starvation = CritterConst.BASE_STARVATION_TIME * attributes.hunger_rate
	var time_until_starvation = 0
	if is_instance_valid(scheduled_events.find_by_id("starvation")) :
		time_until_starvation = scheduled_events.find_by_id("starvation").timestamp - (Controller.Scheduler as Scheduler).current_timestamp
	var duration_of_percentage: int = total_time_of_starvation * hunger_percentage/100
	var filled_hunger: int = total_time_of_starvation - time_until_starvation
	return duration_of_percentage > filled_hunger

func get_fatigue_percentage() -> float :
	var total_time_of_coma = CritterConst.BASE_SLEEP_DEPRIVATION_TIME * attributes.fatigue_rate
	var time_until_coma = 0
	if is_instance_valid(scheduled_events.find_by_id("coma")) :
		time_until_coma = scheduled_events.find_by_id("coma").timestamp - (Controller.Scheduler as Scheduler).current_timestamp
	elif current_action.action_name == CritterConst.ActionNames.SLEEP :
		time_until_coma = current_action.get_fatigue_recovery()
	return (1 - time_until_coma / total_time_of_coma) * 100

## Returns in how many scheduler timestamps the critter will reach the percentage of hunger provided in argument
## [br][br] Returns a negative number if the percentage has already been reached
func get_time_until_fatigue(percentage:int) -> int :
	var total_time_of_coma = CritterConst.BASE_SLEEP_DEPRIVATION_TIME * attributes.fatigue_rate
	var time_until_coma = 0
	if is_instance_valid(scheduled_events.find_by_id("coma")) :
		time_until_coma = scheduled_events.find_by_id("coma").timestamp - (Controller.Scheduler as Scheduler).current_timestamp
	elif current_action.action_name == CritterConst.ActionNames.SLEEP :
		time_until_coma = current_action.get_fatigue_recovery()
	var duration_of_percentage: int = total_time_of_coma * percentage / 100
	var time_until_percentage: int = duration_of_percentage - (total_time_of_coma - time_until_coma)
	return time_until_percentage

func set_can_mate(value:bool) -> void :
	if value :
		tags.append("mateable")
		behavior.on_able_to_breed()
	else :
		tags.erase("mateable")

## Returns how many scheduler timestamps the critter takes to move one tile
func get_mov_speed() -> int :
	return (CritterConst.BASE_MOVEMENT_SPEED * attributes.speed as int)

func get_wandering_speed() -> int :
	return (get_mov_speed() * randf_range(3.0, 5.0) as int)

func get_mating_cooldown() -> int : 
	return (CritterConst.BASE_MATING_COOLDOWN * attributes.breeding_cooldown as int)

## Returns how many scheduler timestamps the critter takes to move one tile while it panics
func get_panic_speed() -> int :
	return (CritterConst.BASE_MOVEMENT_SPEED * attributes.speed * 0.5 as int)

## Returns how many scheduler timestamps the critter takes to eat one portion of a ressource
func get_eating_speed() -> int  :
	return (CritterConst.BASE_EATING_SPEED * attributes.eating_speed as int)

func get_gestation_time() -> int :
	return (CritterConst.BASE_GESTATION_TIME * attributes.gestation_time as int)

func get_intimidation() -> int :
	return attributes.base_intimidation

func get_fightscore() -> int : 
	return attributes.fightscore

func get_morale() -> int :
	return attributes.morale - ceil(injury_level/2)

func get_species() -> String : 
	return SpecieData.Species.keys()[specie]
	
func get_food_chain() -> int :
	return attributes.food_chain

## Delays all hunger events by the timestamp amount recieved in argument
## [br][br] If timestamp_change is negative, the timestamps are instead un-delayed
## [br][br] Events keep their proportion relative to starvation
## For example, if the timestamp change is delaying by 200 timestamps,
## the starvation event will be delayed by 200 timestamps and a "hunger under 50%" event will be delayed by 100
func decrease_hunger(timestamp_change:int) -> void :
	for event in scheduled_events.get_events_of_type(EventEnums.EventType.HUNGER_TIMER) :
			var total_time_of_starvation = CritterConst.BASE_STARVATION_TIME * attributes.hunger_rate
			var time_until_event = event.timestamp - (Controller.Scheduler as Scheduler).current_timestamp
			var max_hunger_recovery: int =  total_time_of_starvation * ((event as HungerEvent).hunger_percentage/100) - time_until_event
			
			if timestamp_change <= max_hunger_recovery :
				(Controller.Scheduler as Scheduler).reschedule_event(timestamp_change, event)
			else :
				(Controller.Scheduler as Scheduler).reschedule_event(max_hunger_recovery, event)
			if timestamp_change >= 0 : 
				behavior.on_hunger_under(get_hunger_percentage())

func decrease_fatigue(timestamp_change:int, is_waken_up_by_force:bool=false) -> void :
	for event in scheduled_events.get_events_of_type(EventEnums.EventType.FATIGUE_TIMER) :
			var total_time_of_coma = CritterConst.BASE_SLEEP_DEPRIVATION_TIME * attributes.fatigue_rate
			var time_until_event = event.timestamp - (Controller.Scheduler as Scheduler).current_timestamp
			var max_fatigue_recovery: int = total_time_of_coma * ((event as FatigueEvent).fatigue_percentage/100) - time_until_event
			
			if timestamp_change <= max_fatigue_recovery :
				(Controller.Scheduler as Scheduler).reschedule_event(timestamp_change, event)
			else :
				(Controller.Scheduler as Scheduler).reschedule_event(max_fatigue_recovery, event)
			if timestamp_change >= 0 and !is_waken_up_by_force: 
				behavior.on_fatigue_under(get_fatigue_percentage())

func get_injured(injury_level:int) -> void :
	if injury_level <= 0 : injury_level = 1
	self.injury_level += injury_level
	update_injury_sprite()
	behavior.on_get_injured()

## Makes a critter eat from a provided food source, recovering its hunger and delaying all events tracking its hunger
## [br][br]This cannot make a critter be less hungry than 0 hunger
func eat(food:GenericFoodEntity) -> void :
	if food.ressource_stock >= 0 :
		food.behavior.on_eaten()
		decrease_hunger(food.portion_food_content)

## Changes the critter's and its sprites' positions on the map
func move(new_position: Vector3i) -> void :
	pathfinding.DisableTile(position,false)
	pathfinding.DisableTile(new_position,true)
	
	var success = pathfinding.AmISwitchingChunks(self, position, new_position)
	
	if success != null :
		position = new_position
		for specific_sprite in [sprite, action_sprite, pregnancy_sprite, gender_sprite, injury_sprite] :
			specific_sprite.position = pathfinding.Tilemap.map_to_local(pathfinding.Tilemap.cube_to_map(position))
	
		if current_action.action_name != CritterConst.ActionNames.FLEE :
			var possible_danger = pathfinding.AmIInDanger(position, get_fightscore(), get_morale(), [get_species()])
			if possible_danger :
				behavior.on_sense_danger()
			elif current_action.action_name == CritterConst.ActionNames.FLEE :
				behavior.on_become_safe()
	
func finish_mating() -> void :
	set_can_mate(false)
	schedule_event(get_mating_cooldown(), TypedEvent.new(func() : set_can_mate(true), "breedingCooldown"))
	behavior.on_done_breeding()

func bear_child() -> void : 
	set_can_mate(false)
	schedule_event(get_gestation_time(), TypedEvent.new(func() : give_birth(), "giveBirth"))
	var pregnancy_texture: Texture2D = load("res://assets/status_icons/CritterPregnant.png")
	pregnancy_sprite.texture = pregnancy_texture
	behavior.on_done_breeding()

func give_birth() -> void :
	for i in range(attributes.litter_size) :
		var chosen_location: Vector3i
		var possible_spawn_locations = pathfinding.PossibleMoves(position)
		possible_spawn_locations.erase(position)
		if possible_spawn_locations.size() != 0 :
			var random_location = randi() % possible_spawn_locations.size()
			chosen_location = possible_spawn_locations.get(random_location)
			Critter.new(specie, position, pathfinding, attributes.litter_size)
	decrease_hunger(-(CritterConst.BASE_STARVATION_TIME * attributes.hunger_rate * 0.2))
	pregnancy_sprite.texture = null
	schedule_event(get_mating_cooldown(), TypedEvent.new(func() : set_can_mate(true), "breedingCooldown"))

# In a fight, the one with the lowest fightscore is guaranteed to get injured. The one with the highest score risks an injury
func fight(opponent:Critter) -> bool :
	var opponent_died: bool = false
	var winner:Critter
	var loser: Critter
	if get_fightscore() >= opponent.get_fightscore() :
		winner = self
		loser = opponent
	else :
		winner = opponent
		loser = self
	
	var fightscore_difference:float = get_fightscore() - opponent.get_fightscore()
	var chance_of_injury:float = (5 - fightscore_difference)/5
	if randf() > chance_of_injury :
		var min_injury:int = 2 - ceil(fightscore_difference/2)
		var max_injury:int = 4 - ceil(fightscore_difference/2)
		var recieved_injury_winner:int = randi_range(min_injury, max_injury)
		if (winner.injury_level + recieved_injury_winner) >= 10 and winner == opponent:
			opponent_died = true
		winner.get_injured(recieved_injury_winner)
	
	var min_injury_loser:int = 2 - ceil(-fightscore_difference/2)
	var max_injury_loser:int = 4 - ceil(-fightscore_difference/2)
	var recieved_injury_loser:int = randi_range(min_injury_loser, max_injury_loser)
	if (loser.injury_level + recieved_injury_loser) >= 10 and loser == opponent:
		opponent_died = true
	loser.get_injured(recieved_injury_loser)
	
	return opponent_died

## Changes the critters current action to the one provided in argument
## [br][br] Also takes care of canceling any event scheduled by the last action 
## and changing the action sprite accordingly
func change_action(action:GenericAction) -> void :
	current_action = action
	change_action_sprite(current_action.action_sprite_filepath)
	cancel_events_of_type(EventEnums.EventType.ACTION_EVENT)
	current_action.startup()

## Changes switches to the next action in the action loop of the current state
func next_action() -> void :
	change_action(current_state.next_action())
