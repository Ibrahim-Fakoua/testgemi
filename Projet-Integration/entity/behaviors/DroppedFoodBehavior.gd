extends GenericFoodEntityBehavior

class_name DroppedFoodBehavior

func on_ressource_emptied() -> void:
	entity.schedule_event(1, TypedEvent.new(func() : on_death(), "death", [], EventEnums.Priority.LAST))
