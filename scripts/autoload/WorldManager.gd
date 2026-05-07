
## This class is a Singleton (Only one instance) / global instance / AutoLoad, it can be accessed from everywhere.

extends Node
class_name WorldManager


## A list containing all loaded biomes.
var biomes_list : Array[Biome] = []
## Global stats of the world.
var world_stats_modifiers : StatsModifiers

const GenerationManagerRef =  preload("res://scripts/world_generation/GenerationManager.cs")
const MainLayerResource: PackedScene = preload("res://environment/scenes/MainLayer.tscn")
const HeightMapTest = preload("res://environment/scenes/HeightMapTest.tscn")
var x;
var heightMapRef;
var main_layer : MainLayer
var pathfinding : PathfindingService
var GenerationManagerInstance
var current_seed : int
	
## Initializes the world by loading biomes from the default folder.
func _init() -> void:
	biomes_list = load_biomes_from_folder("res://environment/biomes/") 
	world_stats_modifiers = StatsModifiers.new()
	Controller.GDSubscribe("Terrarium_Virtuel.signals.WorldGenCompleteSignal", on_map_generated)
	Controller.GDSubscribe("Terrarium_Virtuel.signals.MapDeleted", map_deleted)
	Controller.GDSubscribe("Terrarium_Virtuel.signals.PlaceMapDetailsSignal", _on_place_map_details_received)
	
func _on_place_map_details_received(signal_data):
	for entry in signal_data.Data:
		var x = entry.x
		var y = entry.y
		var food_int = entry.food
		
	


func _ready() -> void:
	Controller.WorldManager = self
	heightMapRef = HeightMapTest.instantiate()
	add_child(heightMapRef)

func on_generate_regions():
	BiomeRegionsGenerator.generate(main_layer,biomes_list, self)
	get_tree().create_timer(1).timeout.connect(make_regions_non_visible)
	
func send_patches_to_generation_manager(patches : Dictionary):
	GenerationManagerInstance.OnReceivePatches(patches.duplicate(true))

func on_map_generated(_nothing):

	get_tree().create_timer(1).timeout.connect(make_regions_non_visible)
	handle_world_gen_signal()
	
func map_deleted(_notheing):
	#print("gone")
	for biome in biomes_list:
		biome.biome_regions= []
		biome._region_data = {}

func make_regions_non_visible():
	for biome in biomes_list:
		for region in biome.biome_regions:
			if region:
				region.hide();

func get_local_to_map() -> Vector3i:
	return main_layer.get_closest_cell_from_mouse()



func create_environment(should_generate_details: bool = true):
	main_layer = MainLayerResource.instantiate()
	add_child(main_layer)
	GenerationManagerInstance = GenerationManagerRef.new()
	GenerationManagerInstance.ShouldGenerateDetails = should_generate_details
	add_child(GenerationManagerInstance)
	GenerationManagerInstance.StartGeneration(100,100, main_layer, current_seed)
	#heightMapRef = HeightMapTest.instantiate()
	#(Controller.MainSceneRef as Node).get_node("TileMap").add_child(heightMapRef)

	# !!! CAUTION: If the debugger says "Invalid call. Nonexistent function 'GenerateWorld' in base 'CSharpScript'."
	# it means that the GenerateWorld function in the c# class threw an error but Godot is 
	# way too stupid to say*  that. it took me 3h of debugging to find this bug.


# This this is where the pathfinding baking actually begins, when it
# receives a signal from the WaveFunctionCollapse class that the world is generated.
# Basically, when the generation is done, this is the first thing to happen afterwards.
func handle_world_gen_signal(_w_signal = null):
	#print("[WorldManager] WorldGenCompleteSignal received, baking pathfinding...")
	
	# This is the magic trick that moves the code to a new thread.
	WorkerThreadPool.add_task(_run_pathfinding_async.bind(main_layer))
	

# The method called to create the pathfinding
func _run_pathfinding_async (layer):
	layer.pathfinding_enabled = true

	var new_pathfinding = PathfindingService.new()
	new_pathfinding.Initialize(layer)
	new_pathfinding.CreatePathfinding()
	
	# because this method is run in a separate thread, it needs call_deferred() to work properly.
	# Essentially, it 'returns' to the original thread.
	_finalize_pathfinding.call_deferred(new_pathfinding)
	
# The method called to finalize the pathfinding generation
func _finalize_pathfinding(service_result):
	pathfinding = service_result # applies the result
	
	# tells the world that it is done.
	var signal_class: Variant = PathfindBakingCompleteSignal
	var signal_envelope = signal_class.new()
	Controller.GDEmit(signal_envelope)

	
## Returns the biome at the given globla position, very efficient ≈ O(1).
## @param position: The coordinates of the tile.
## @return: The biome assigned to the tile, or null if no data is found.
func get_biome_at(global_pos : Vector2) -> Biome:

	var local_pos = main_layer.to_local(global_pos)
	

	var map_pos: Vector2i = main_layer.local_to_map(local_pos)


	var data: TileData = main_layer.get_cell_tile_data(map_pos)
	
	if data != null:
		return data.get_custom_data("biome") as Biome
	return null

func get_region_at(global_pos : Vector2, biome : Biome = null) -> BiomeRegion:
	if biome == null:
		return get_biome_at(global_pos).get_biome_region_at(global_pos)
	else :
		return biome.get_biome_region_at(global_pos)
		
		
func get_modified_stats_at(global_pos : Vector2) -> Stats:

	return get_region_at(global_pos).stats
	
func update_biomes_modifiers() -> void:
	for biome in biomes_list:
		biome.biome_stats_modifiers.add_modifiers(world_stats_modifiers)


## Sets the main layer and triggers biome region generation.
func set_main_layer(layer: MainLayer) -> void:
		self.main_layer = layer





## Loads biome resources from a specified folder path.
## @param folder_path: The path to the folder containing biome resource files.
## @return: An array of loaded Biome resources.
static func load_biomes_from_folder(folder_path: String) -> Array[Biome]:
	var loaded_biomes: Array[Biome] = []
	var dir = DirAccess.open(folder_path)
	
	if dir:

		for file_name in dir.get_files():

			var clean_name = file_name.trim_suffix(".remap")
			
			if clean_name.ends_with(".tres") or clean_name.ends_with(".res"):

				var full_path = folder_path.path_join(clean_name)
				
				var resource = load(full_path) as Biome
				
				if resource:
					loaded_biomes.append(resource)
	else:
		push_error("Could not open directory at: ", folder_path)
		
	return loaded_biomes
	
func get_main_layer() -> MainLayer:
	return main_layer	
