extends GenericAction

class_name Hunt

var prey:Critter = null
var found_food = false
var exhaustion_counter:int = 0

func _init(critter:Critter) -> void :
	super._init(critter, CritterConst.ActionNames.HUNT, "res://assets/action_icons/SeekPrey.png")

func startup() -> void:
	if !found_food :
		var possible_food = critter.pathfinding.move_to_closest_thing(critter.position, ["meat"])
		if possible_food :
			found_food = true
			action_sprite_filepath = "res://assets/action_icons/SeekFoodCarnivore.png"
	
	if !found_food and prey == null :
		prey = critter.pathfinding.get_nearest_prey(critter.position, critter.get_food_chain(), [critter.get_species()])
		if prey is Critter :
			prey.tags.append("prey" + str(critter.creature_id))
			action_sprite_filepath = "res://assets/action_icons/SeekPrey.png"
	
	if prey == null :
		schedule_default_startup(critter.get_mov_speed())
	else :
		schedule_default_startup(critter.get_mov_speed() * CritterConst.PREDATOR_SPEED_BOOST)

func activate() -> void:
	if prey == null and !found_food : 
		var possible_moves : Array = critter.pathfinding.possible_moves(critter.position)
		if possible_moves.size() == 0:
			possible_moves.append(critter.position)
		var random_direction = randi() % possible_moves.size()
		var destination_pos = possible_moves.get(random_direction)
		critter.move(destination_pos)
		startup()
	elif found_food :
		var destination_pos = critter.pathfinding.move_to_closest_thing(critter.position, ["meat"])
		if destination_pos is Vector3i :
			critter.move(destination_pos)
			startup()
		elif destination_pos == null :
			var wandering_pos : Array = critter.pathfinding.possible_moves(critter.position)
			if wandering_pos.size() == 0:
				wandering_pos.append(critter.position)
			var random_direction = randi() % wandering_pos.size()
			destination_pos = wandering_pos.get(random_direction)
			startup()
			critter.move(destination_pos)
			found_food = false
		else :
			end()
	elif prey != null : 
		var destination_pos = critter.pathfinding.move_to_closest_thing(critter.position, ["prey" + str(critter.creature_id)])
		if destination_pos is Vector3i :
			if critter.pathfinding.move_to_closest_thing(critter.position, ["prey" + str(critter.creature_id)]) is Critter :
				_attack_prey()
			else :
				startup()
			critter.move(destination_pos)
		elif destination_pos is Critter : 
			prey = destination_pos
			_attack_prey()
		else :
			startup()

func end() -> void :
	if is_instance_valid(prey) :
		prey.tags.erase("prey" + str(critter.creature_id))
	prey = null
	found_food = false
	super.end()

func _attack_prey() -> void :
	var has_killed_prey:bool = critter.fight(prey)
	if has_killed_prey :
		end()
	else :
		startup()

func register_action() -> void:
	DatabaseManager.RegisterLogAction(critter.creature_id, action_name, "", (Controller.Scheduler as Scheduler).current_timestamp)
