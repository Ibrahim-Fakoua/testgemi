extends GenericAction

class_name Sleep

var starting_timestamp: int = 0
var time_until_completely_rested: int = 0
var woke_up_by_itself: bool = false

func _init(critter:Critter) -> void :
	super._init(critter, CritterConst.ActionNames.SLEEP, "res://assets/action_icons/Sleep.png")

func startup() -> void :
	starting_timestamp = (Controller.Scheduler as Scheduler).current_timestamp
	time_until_completely_rested = -critter.get_time_until_fatigue(0) * 0.5 * critter.attributes.sleep_efficiency
	schedule_default_startup(time_until_completely_rested)
# This is an abomination
func activate() -> void :
	woke_up_by_itself = true
	recover_fatigue(false)
	end()

func get_fatigue_recovery() -> int :
	var time_sleeping = (Controller.Scheduler as Scheduler).current_timestamp - starting_timestamp
	return time_sleeping

func recover_fatigue(is_waken_up_by_force:bool) -> void :
	var time_sleeping = (Controller.Scheduler as Scheduler).current_timestamp - starting_timestamp
	if !critter.scheduled_events.get_events().has(critter.scheduled_events.find_by_id("coma")) and time_sleeping == time_until_completely_rested:
		critter.schedule_event(CritterConst.BASE_SLEEP_DEPRIVATION_TIME * critter.attributes.fatigue_rate, FatigueEvent.new(func() : critter.behavior.on_fatigue_reached(100), "coma", 100))
		critter.behavior.on_fatigue_under(0)
	elif time_sleeping == time_until_completely_rested :
		(Controller.Scheduler as Scheduler).reschedule_event(CritterConst.BASE_SLEEP_DEPRIVATION_TIME * critter.attributes.fatigue_rate, critter.scheduled_events.find_by_id("coma"))
		critter.behavior.on_fatigue_under(0)
	else : 
		is_waken_up_by_force = true
		var fatigue_recovery = time_sleeping * 2 * (1/critter.attributes.sleep_efficiency)
		if !critter.scheduled_events.get_events().has(critter.scheduled_events.find_by_id("coma")) :
			critter.schedule_event(fatigue_recovery, FatigueEvent.new(func() : critter.behavior.on_fatigue_reached(100), "coma", 100))
			critter.behavior.on_fatigue_under(critter.get_fatigue_percentage())
		else :
			critter.decrease_fatigue(fatigue_recovery, is_waken_up_by_force)
			is_waken_up_by_force = false
	starting_timestamp = 0
	time_until_completely_rested = 0

func register_action() -> void:
	DatabaseManager.RegisterLogAction(critter.creature_id, action_name, "eepy", (Controller.Scheduler as Scheduler).current_timestamp)
