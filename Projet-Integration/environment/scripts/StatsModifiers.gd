extends Resource
class_name StatsModifiers

@export_group("Multipliers")
@export var temperature_multiplier: float

@export var humidity_multiplier: float

@export var walking_speed_multiplier: float



@export_group("Adders")

@export var temperature_Adder: float

@export var humidity_Adder: float

@export var walking_speed_Adder: float


func _init(temperature_mul: float = 1.0, humidity_mul: float = 1.0, speed_mul: float = 1,
temperature_add: float = 0, humidity_add: float =0, speed_add: float = 0 ) -> void:

	temperature_multiplier = temperature_mul
	humidity_multiplier = humidity_mul
	walking_speed_multiplier = speed_mul
	
	temperature_Adder = temperature_add
	humidity_Adder = humidity_add
	walking_speed_Adder = speed_add



		
	
