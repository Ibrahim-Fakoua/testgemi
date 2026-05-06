extends Node

class_name ActionShelfManager
var bomb : SpriteFrames
func _ready() -> void:
	Controller.GDSubscribe("Terrarium_Virtuel.signals.action_items.ActionItemExecutedSignal", _handle_action)
	bomb = load("res://assets/Bomb_Sprite_Frames.tres")
	
func _handle_action(item_signal) -> void :
	if (Controller.WorldManager as WorldManager).pathfinding:
		if (Controller.WorldManager as WorldManager).pathfinding.validate_tile(item_signal.Position): 
			if item_signal.CategoryName == "Espèces" :
				handle_critter_signal(item_signal)
			
			if item_signal.CategoryName == "Nourriture":
				handle_food_signal(item_signal)
			
			if item_signal.CategoryName == "Catastrophes":
				handle_event_signal(item_signal)
		else:
			push_warning("Attempted to place something out of bounds")

func handle_critter_signal(item_signal) -> void :
	match (item_signal.Id as int) : 
		0: spawn_critter(SpecieData.Species.SPECIE_1, item_signal.Position)
		1: spawn_critter(SpecieData.Species.SPECIE_2, item_signal.Position)
		2: spawn_critter(SpecieData.Species.SPECIE_3, item_signal.Position)
		3: spawn_critter(SpecieData.Species.SPECIE_4, item_signal.Position)
		4: spawn_critter(SpecieData.Species.SPECIE_5, item_signal.Position)
		5: spawn_critter(SpecieData.Species.SPECIE_6, item_signal.Position)
		6: spawn_critter(SpecieData.Species.SPECIE_7, item_signal.Position)
		7: spawn_critter(SpecieData.Species.SPECIE_8, item_signal.Position)
		8: spawn_critter(SpecieData.Species.SPECIE_9, item_signal.Position)
		9: spawn_critter(SpecieData.Species.INVASIVE_SPECIE_1, item_signal.Position)
		10: spawn_critter(SpecieData.Species.INVASIVE_SPECIE_2, item_signal.Position)

func handle_food_signal(item_signal) -> void:
	var food_filepath:String = "res://entity/entities/Carrot.gd"
	match (item_signal.Id as int) : 
		0: food_filepath = "res://entity/entities/Carrot.gd"
		1: food_filepath = "res://entity/entities/AppleTree.gd"
		2: food_filepath = "res://entity/entities/ApplePile.gd"
		3: food_filepath = "res://entity/entities/BerryBush.gd"
	if food_filepath :
		spawn_food(item_signal.Position, food_filepath)

func handle_event_signal(item_signal) -> void:
	match (item_signal.Id as int) :
		0: explosion(item_signal.Position)

func spawn_food(signal_position, spawned_food_filepath:String) -> void:
	var spawned_food = load(spawned_food_filepath)
	spawned_food.new((Controller.WorldManager as WorldManager).pathfinding, signal_position)

func spawn_critter(specie:SpecieData.Species, signal_position) :
	Critter.new(specie, signal_position, (Controller.WorldManager as WorldManager).pathfinding)

func explosion(position):
	var effect = AnimatedSprite2D.new()
	(Controller.WorldManager as WorldManager).main_layer.add_child(effect)
	effect.sprite_frames = bomb
	effect.position = (Controller.WorldManager as WorldManager).main_layer.cube_to_local(position)
	effect.z_index = 999
	
	effect.play("default")
	effect.animation_finished.connect(func() : effect.queue_free())
	
	var victim : GenericEntity = (Controller.WorldManager as WorldManager).pathfinding.find_entity_at_tile(position)
	if victim:
		for event in victim.scheduled_events.get_events() :
			victim.cancel_event(event.id)
		victim.behavior.on_death()
		#victim.schedule_event(1, TypedEvent.new(func() : victim.behavior.on_death(), "death", [], EventEnums.Priority.LAST)) 
	
