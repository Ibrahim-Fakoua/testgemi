extends GenericCondition

class_name CanBreed

func _init() -> void:
	condition_type = CritterConst.ConditionTypes.CAN_BREED

func setup_condition_event(critter:Critter) -> void:
	if critter.tags.has("mateable") :
		critter.schedule_event(1, TypedEvent.new(func() : critter.behavior.on_able_to_breed(), "canBreed", [EventEnums.EventType.STATE_EVENT]))
