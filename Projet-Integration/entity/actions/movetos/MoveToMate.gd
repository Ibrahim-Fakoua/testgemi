extends MoveToAction

class_name MoveToMate

func _init(critter:Critter) -> void :
	if critter.tags.has("male") :
		super._init(critter, "res://assets/action_icons/SeekLove.png", ["female" , "wantsMate", critter.get_species()])
	else :
		super._init(critter, "res://assets/action_icons/SeekLove.png", ["male" , "wantsMate", critter.get_species()])

func startup() -> void :
	if !critter.tags.has("wantsMate") :
		critter.tags.append("wantsMate")
	super.startup()

func end() -> void :
	if critter.tags.has("wantsMate") :
		var possible_mate = _find_mate()
		if possible_mate is Critter :
			critter.mate = possible_mate
			critter.tags.erase("wantsMate")
			possible_mate.mate = critter
			possible_mate.tags.erase("wantsMate")
			possible_mate.current_action.end()
	if critter.mate != null :
		super.end()
	else :
		startup()

func _find_mate() -> Critter :
	var possible_mate
	if critter.tags.has("male") :
		possible_mate = critter.pathfinding.move_to_closest_thing(critter.position, ["wantsMate", "female", critter.get_species()])
		if possible_mate is Critter : 
			return possible_mate
	else :
		possible_mate =  critter.pathfinding.move_to_closest_thing(critter.position, ["wantsMate", "male", critter.get_species()])
		if possible_mate is Critter : 
			return possible_mate
	return null
