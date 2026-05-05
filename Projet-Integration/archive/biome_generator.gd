"""
UnUsed File
"""


#@tool
#extends EditorScript
#
#
#enum EnvVars{
	#TEMPERATURE,
	#WALKING_SPEED,
	#HUMIDITY
#}
#
#
#func _run():
#
	#var path: String = "res://environment/biomes/"
	#DirAccess.make_dir_recursive_absolute(path)
	#breakpoint
	#var biomes_to_create: Dictionary[String, Variant] = {
		#"Forest": {EnvVars.TEMPERATURE : 22, EnvVars.WALKING_SPEED : 10, EnvVars.HUMIDITY : 60},
		#"Desert": {EnvVars.TEMPERATURE : 60, EnvVars.WALKING_SPEED : 5, EnvVars.HUMIDITY : 15},
		#"Grassland": {EnvVars.TEMPERATURE : 16, EnvVars.WALKING_SPEED : 14, EnvVars.HUMIDITY : 80},
		#"tundra" :{EnvVars.TEMPERATURE : 0, EnvVars.WALKING_SPEED : 3, EnvVars.HUMIDITY : 10}
	#}
#
#
#
	#for name in biomes_to_create:
			#var data : Dictionary[Variant,Variant] = biomes_to_create[name]
#
#
			#var biome_to_save: Biome = Biome.new(name,data)
			#
			#
			#var file_path = path + name.to_lower() + ".tres"
			#var error: int = ResourceSaver.save(biome_to_save, file_path)
			#
			#if error == OK:
				#print("Created: ", file_path)
			#else:
				#print("Error saving ", name, ": ", error)
#
#
	#EditorInterface.get_resource_filesystem().scan()
