extends GenericAction

class_name MoveToAction

var searched_tag: Array[String]
var excluded_tags: Array[String]
var can_panic: bool
var panicked_sprite_filepath: String

func _init(critter:Critter, action_sprite_filepath: String, searched_tag: Array[String], excluded_tags:Array[String]=[], can_panic:bool=false, panicked_sprite_filepath: String="") -> void :
	super._init(critter, CritterConst.ActionNames.MOVE_TO, action_sprite_filepath)
	self.searched_tag = searched_tag
	self.excluded_tags = excluded_tags
	self.can_panic = can_panic
	self.panicked_sprite_filepath = panicked_sprite_filepath

func startup() -> void :
	var destination_pos = critter.pathfinding.move_to_closest_thing(critter.position, searched_tag, excluded_tags)
	if destination_pos != null or !can_panic: 
		critter.change_action_sprite(action_sprite_filepath)
		schedule_default_startup(critter.get_mov_speed())
	else :
		critter.change_action_sprite(panicked_sprite_filepath)
		schedule_default_startup(critter.get_panic_speed())

func activate() -> void :
	var destination_pos = critter.pathfinding.move_to_closest_thing(critter.position, searched_tag, excluded_tags)
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
	else :
		end()


func register_action() -> void:
	DatabaseManager.RegisterLogAction(critter.creature_id, action_name, str(critter.position), (Controller.Scheduler as Scheduler).current_timestamp)
