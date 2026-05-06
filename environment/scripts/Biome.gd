## Represents a distinct environment type (e.g., Forest, Desert, Tundra) with its own stats and regions. 
## A biome is a property that you "paint" on a tile, if this is done then the tile has a reference to the biome you "painted" it with.
## A biome has an array of BiomeRegions that share that same biome.
extends Resource
class_name Biome



@export var biome_name: String = "NameHasNotBeenSet":
	set(value):

		biome_name = value
		resource_name = value

@export var biome_stats_modifiers : StatsModifiers
@export var biome_base_stats : Stats:
	set(value):
		biome_base_stats = value
		biome_base_stats.parent = self

##  The list of regions belonging to this biome.
var biome_regions: Array[BiomeRegion] = []:
	set(value):
		biome_regions = value
		setup_bounding_box()
##  The list of the stats for each region in the biome.
var regions_stats : Array[Stats] = []
## This is for associating a rectagle for each region, it is used for finding the region at a specific point  
var _region_data: Dictionary = {}


func _init() -> void:

	if biome_base_stats == null:
		biome_base_stats = Stats.new(self)


	if biome_stats_modifiers == null:
		biome_stats_modifiers = StatsModifiers.new()
		


func _to_string() -> String:
	return "Biome: %s" % biome_name

## Adds a stats object belonging to a region to the list.
## @param p_stats: The stats of the region to add.
func add_region_stats(p_stats : Stats):
	regions_stats.append(p_stats)


	

func get_biome_region_at(world_coord: Vector2):
	
	for region in _region_data:
		var bounds: Rect2 = _region_data[region]
		

		if bounds.has_point(world_coord):
			

			var local_coord = world_coord - region.global_position
			if Geometry2D.is_point_in_polygon(local_coord, region.polygon):
				return region 
				
	return null
	
func setup_bounding_box():

	for region in biome_regions:
		var points = region.polygon
		var min_x = INF; var max_x = -INF
		var min_y = INF; var max_y = -INF
		for p in points:
			if p.x < min_x: min_x = p.x
			if p.x > max_x: max_x = p.x
			if p.y < min_y: min_y = p.y
			if p.y > max_y: max_y = p.y

		var pos = Vector2(min_x, min_y) + region.global_position
		var size = Vector2(max_x - min_x, max_y - min_y)
		var rect = Rect2(pos, size)

		_region_data[region] = rect
		
func update_regions_stats():
	for region_stats in regions_stats:
		region_stats.modify_stats_using_stats_modifiers(biome_stats_modifiers)
		
