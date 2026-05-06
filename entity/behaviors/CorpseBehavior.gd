extends DroppedFoodBehavior

class_name CorpseBehavior

func on_rot() -> void :
	entity.portion_food_content = ceil(entity.portion_food_content/2)
	var rotten_texture:Texture2D = load(entity.rotten_sprite)
	entity.sprite.texture = rotten_texture
	entity.schedule_event(entity.decay_rate * 2, TypedEvent.new(func() : on_death(), "completeRot", [], EventEnums.Priority.LAST))
