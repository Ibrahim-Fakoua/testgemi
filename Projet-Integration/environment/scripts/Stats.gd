## [Resource] This class represents the stats for a biome or a region.
## It is a [Resource] to simplify manual creation in the editor, but could be 
## changed to [RefCounted] in the future.
extends Resource
class_name Stats

#  If you add a variable you MUST do so for ALL the functions below!!

@export var temperature: float:
	set(value):
		temperature = value
	get():
		

		if get_script() == WorldManager:
			return temperature
		else:
			return (temperature  * parent.biome_stats_modifiers.temperature_multiplier * Controller.WorldManager.world_stats_modifiers.temperature_multiplier) + (
			parent.biome_stats_modifiers.temperature_Adder  + Controller.WorldManager.world_stats_modifiers.temperature_Adder)
@export var humidity: float:
	set(value):
		humidity = value
	get():
		if get_script() == WorldManager:
			return humidity
		else:
			return (humidity  * parent.biome_stats_modifiers.humidity_multiplier * Controller.WorldManager.world_stats_modifiers.humidity_multiplier) + (
			parent.biome_stats_modifiers.humidity_Adder  + Controller.WorldManager.world_stats_modifiers.humidity_Adder)
@export var walking_speed: float:
	set(value):
		walking_speed = value
	get():
		if get_script() == WorldManager:
			return walking_speed
		else:
			return (walking_speed  * parent.biome_stats_modifiers.walking_speed_multiplier * Controller.WorldManager.world_stats_modifiers.walking_speed_multiplier) + (
				parent.biome_stats_modifiers.walking_speed_Adder  + Controller.WorldManager.world_stats_modifiers.walking_speed_Adder)


var parent : Object
func _init(p_parent = null,p_temp: float = 0.0, p_humidity: float = 0.0, p_speed: float = 0) -> void:
	if p_parent != null:
		
		parent = p_parent
	
	temperature = p_temp
	humidity = p_humidity
	walking_speed = p_speed


	

#func modify_stats_using_stats_modifiers(stats_modifiers : StatsModifiers):
	#_temperature = (temperature * stats_modifiers.temperature_multiplier) + stats_modifiers.temperature_Adder
	#_humidity = (humidity * stats_modifiers.humidity_multiplier) + stats_modifiers.humidity_Adder
	#_walking_speed = (walking_speed * stats_modifiers.walking_speed_multiplier)  + stats_modifiers.walking_speed_Adder



# Sums up and normalizes the stats from a list of biomes.
# This Function takes a 2d array if you only have a 1d array just surround it with [].
# @param list_of_biomes: A 2D array of [Stats] objects.
# @return: [Stats] A new object containing the normalized data.
#static func add_and_normalize(list_of_biomes: Array[Array]) -> Stats:
	#var result: Stats = Stats.new()
	#for stats_list: Array in list_of_biomes:
		#for stat: Stats in stats_list:
			#result.temperature += stat.teperature
			#result.humidity += stat.humidity
			#result.walking_speed += stat.walking_speed
	#
	#return result
	
	

























	
