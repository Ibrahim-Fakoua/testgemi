extends GenericFoodEntityBehavior

class_name ReplinishingFoodSourceBehavior

func on_eaten() -> void : 
	if entity.is_filled : 
		_schedule_regeneration()
	super.on_eaten()
	entity.is_filled = false
	if entity.ressource_stock <= 0 :
		entity.change_sprite(entity.empty_sprite)
		if !entity.tags.has("empty") :
			entity.tags.append("empty")

func on_regeneration() -> void :
	if entity.ressource_stock < entity.max_ressource_capacity :
		_schedule_regeneration()
	if entity.ressource_stock >= entity.max_ressource_capacity :
		entity.is_filled = true

func _schedule_regeneration() -> void :
	entity.schedule_event(entity.regeneration_time, TypedEvent.new(func() : entity.regenerate(), "foodRegen"))
