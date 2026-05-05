extends GenericBehavior

class_name SpawnerEntityBehavior

func on_spawned() -> void :
	_schedule_spawn()

func _schedule_spawn() -> void :
	entity.schedule_event(entity.spawn_cooldown, TypedEvent.new(func() : entity.spawn(), "spawnEntity"))
