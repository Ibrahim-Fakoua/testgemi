extends Node

## This class contains all the triggers that could cause actions in any entity
class_name GenericBehavior

var entity: GenericEntity

func _init(entity: GenericEntity) -> void :
	self.entity = entity


func on_lifespan_end() -> void :
	pass

func on_death() -> void :
	if is_instance_valid(entity) : 
		entity.pathfinding.disable_tile(entity.position,false)
		entity.pathfinding.get_chunk_at_tile(entity.position).remove_from_chunk(entity)
		for event in entity.scheduled_events.get_events().duplicate() :
			entity.cancel_event(event.id)
		entity.queue_free()
