extends MoveToAction

class_name MoveToFood

func _init(critter:Critter) -> void :
	var action_icon_filepath:String
	var searched_tags:Array[String] = ["food"]
	var excluded_tags:Array[String] = ["empty"]
	if critter.tags.has("carnivore") : 
		action_icon_filepath = "res://assets/action_icons/SeekFoodCarnivore.png"
		searched_tags.append("meat")
	elif critter.tags.has("herbivore") :
		action_icon_filepath = "res://assets/action_icons/SeekFoodHerbivore.png"
		searched_tags.append("fruit")
	else :
		action_icon_filepath = "res://assets/action_icons/SeekFoodCarnivore.png"
	super._init(critter, action_icon_filepath, searched_tags, excluded_tags, true, "res://assets/action_icons/PanicFood.png")
