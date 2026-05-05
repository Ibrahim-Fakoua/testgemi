## A utility class for generating biome regions based on TileMapLayer data.
extends RefCounted
class_name BiomeRegionsGenerator

## Generates biome regions for the given TileMapLayer and biomes.
## @param layer: The TileMapLayer to process.
## @param biomes: An array of Biome resources to generate regions for.
static func generate(layer: TileMapLayer, biomes: Array[Biome] , worldMan : WorldManager) -> void:
	var biome_cells := _get_list_of_cells_for_biomes(layer, biomes)
	var biome_patches := _separate_cells_by_patches(layer, biome_cells)
	worldMan.send_patches_to_generation_manager(biome_patches)
	_create_outline_from_patch(layer, biome_patches)

	
## Internal method to create BiomeRegion objects and add them to the layer and the biome.



static func _generate_biome_regions(biome : Biome,biome_polygons : Array[PackedVector2Array] , layer : TileMapLayer):

		var patch_number = 0
		for polygon in biome_polygons:

			var region = BiomeRegion.new(biome)
			region.polygon = polygon
			region.name = "%s #%d" % [biome.biome_name,patch_number]

			region.color = Color(randf(), randf(), randf(), 0.5) 
			biome.biome_regions.append(region)
			layer.add_child(region)
			patch_number += 1
## Scans the TileMapLayer to find all cells associated with each biome.
static func _get_list_of_cells_for_biomes(layer: TileMapLayer, biomes: Array[Biome]) -> Dictionary:
	var biome_cells: Dictionary = {}
	for biome in biomes:
		biome_cells[biome] = []
		
	var tileset: TileSet = layer.tile_set
	if not tileset:
		return biome_cells

	for source_id in tileset.get_source_count():
		var real_source_id := tileset.get_source_id(source_id)
		var source: TileSetAtlasSource = tileset.get_source(real_source_id)
		if not source:
			continue
		
		for i in source.get_tiles_count():
			var atlas_coords: Vector2i = source.get_tile_id(i)
			var tile_data: TileData = source.get_tile_data(atlas_coords, 0)
			var biome_resource: Biome = tile_data.get_custom_data("biome")
			
			if biome_resource and biome_cells.has(biome_resource):
				var cells_of_this_type: Array[Vector2i] = layer.get_used_cells_by_id(real_source_id, atlas_coords)
				biome_cells[biome_resource].append_array(cells_of_this_type)

	return biome_cells

## Separates groups of contiguous cells into individual patches (regions) for each biome.
static func _separate_cells_by_patches(layer: TileMapLayer, biome_cells: Dictionary) -> Dictionary:
	var biome_patches: Dictionary = {}
	var hex_layer := layer as HexagonTileMapLayer
	
	if not hex_layer.tile_set:
		push_error("HexagonTileMapLayer does not have a TileSet assigned.")
		return {}

	if not hex_layer._cube_to_map.is_valid() or not hex_layer._map_to_cube.is_valid():
		hex_layer._on_tileset_changed()

	for biome: Biome in biome_cells:
		var cells: Array = biome_cells[biome]
		var patches: Array = []
		var unvisited: Dictionary = {}
		
		for cell: Vector2i in cells:
			unvisited[cell] = true
			
		while not unvisited.is_empty():
			var current_patch: Array[Vector2i] = []
			

			var start_cell: Vector2i
			for k in unvisited:
				start_cell = k
				break
			
			var queue: Array[Vector2i] = [start_cell]
			unvisited.erase(start_cell)
		
			while not queue.is_empty():
				var cell: Vector2i = queue.pop_back()
				current_patch.append(cell)

				var current_cube := hex_layer.map_to_cube(cell)
				var neighbors_cube := HexagonTileMap.cube_neighbors_for_axis(hex_layer.tile_set.tile_offset_axis, current_cube)
		
				for neighbor_cube in neighbors_cube:
					var neighbor := hex_layer.cube_to_map(neighbor_cube)
					if unvisited.has(neighbor):
						unvisited.erase(neighbor)
						queue.append(neighbor)
						
			patches.append(current_patch)
			
		biome_patches[biome] = patches
		
	return biome_patches

## Creates outlines for each biome patch and triggers region generation.
static func _create_outline_from_patch(layer: TileMapLayer, biome_patches: Dictionary) -> void:
	var hex_layer := layer as HexagonTileMapLayer
	var tile_set := hex_layer.tile_set
	var tile_size := tile_set.tile_size
	var inner_radius := maxf(tile_size.x, tile_size.y) / 2.0
	
	for biome: Biome in biome_patches:
		var all_patches: Array = biome_patches[biome]
		var biome_polygons: Array[PackedVector2Array] = []
		
		for patch_cells: Array in all_patches:
			var cube_cells: Array[Vector3i] = []
			for cell: Vector2i in patch_cells:
				cube_cells.append(hex_layer.map_to_cube(cell))
				
			var loops_data := _generate_custom_outlines(hex_layer, cube_cells, inner_radius)

			if loops_data.is_empty():
				continue

			var outer_idx := 0
			var max_area := -1.0
			
			for i in range(loops_data.size()):
				var rect: Rect2 = loops_data[i].rect
				var area := rect.size.x * rect.size.y
				if area > max_area:
					max_area = area
					outer_idx = i

			var base_poly: PackedVector2Array = loops_data[outer_idx].poly

			for i in range(loops_data.size()):
				if i == outer_idx:
					continue
				base_poly = _merge_hole_into_poly(base_poly, loops_data[i].poly, loops_data[i].rect)

			biome_polygons.append(base_poly)
				
		if not biome_polygons.is_empty():
			_generate_biome_regions(biome,biome_polygons, layer)

## Generates a custom outline for the given patch by removing redundant vertices (e.g : a straight line) for a better performance.
## Returns an Array of Dictionaries, each containing "poly" (PackedVector2Array) and "rect" (Rect2).
static func _generate_custom_outlines(layer: HexagonTileMapLayer, cells: Array[Vector3i], radius: float) -> Array[Dictionary]:
	var edges_map: Dictionary = {}
	var tile_set := layer.tile_set
	var axis := tile_set.tile_offset_axis
	var tile_size := tile_set.tile_size
	
	var geometry_methods: Dictionary = HexagonTileMap.get_geometry_methods_for(axis)
	var corners: PackedVector2Array = geometry_methods.tile_corners.call(tile_size)

	var side_directions: Array
	if axis == TileSet.TileOffsetAxis.TILE_OFFSET_AXIS_HORIZONTAL:
		side_directions = HexagonTileMap.cube_horizontal_side_neighbor_directions
	else:
		side_directions = HexagonTileMap.cube_vertical_side_neighbor_directions

	var cells_set: Dictionary = {}
	for cell in cells:
		cells_set[cell] = true
	
	for cell in cells:
		var center := layer.cube_to_local(cell)
		for i in 6:
			var neighbor := HexagonTileMap.cube_neighbor_for_axis(axis, cell, side_directions[i])
			if not cells_set.has(neighbor):
				var p1 := center + corners[i]
				var p2 := center + corners[(i + 1) % 6]
				

				var key := Vector2i((p1 * 100.0).round())
				
				if not edges_map.has(key):
					edges_map[key] = []
				
				edges_map[key].append({
					"p1": p1,
					"p2": p2,
					"center": center
				})
				
	var loops: Array[Dictionary] = []
	
	while not edges_map.is_empty():
		var current_loop := PackedVector2Array()
		var min_pt := Vector2(INF, INF)
		var max_pt := Vector2(-INF, -INF)
		

		var start_key: Vector2i
		for k in edges_map:
			start_key = k
			break
			
		var start_edges_list: Array = edges_map[start_key]
		var start_edge: Dictionary = start_edges_list.pop_back()
		if start_edges_list.is_empty():
			edges_map.erase(start_key)
			
		var current_edge: Dictionary = start_edge
		var loop_edges: Array[Dictionary] = [start_edge]
		
		while true:
			var next_p: Vector2 = current_edge.p2
			var next_key := Vector2i((next_p * 100.0).round())
			
			if edges_map.has(next_key):
				var candidates: Array = edges_map[next_key]
				var next_edge: Dictionary = candidates.pop_back()
				if candidates.is_empty():
					edges_map.erase(next_key)
				
				loop_edges.append(next_edge)
				current_edge = next_edge
				
				if current_edge.p2.distance_squared_to(start_edge.p1) < 1.0:
					break 
			else:
				break
		
		var edge_prev_in_loop: Dictionary = loop_edges.back()
		
		for k in range(loop_edges.size()):
			var edge_k := loop_edges[k]
			var edge_next := loop_edges[(k + 1) % loop_edges.size()]
			
			var c1: Vector2 = edge_k.center 
			var p1: Vector2 = ((edge_k.p1 - c1).normalized() * radius) + c1
			var p2: Vector2 = ((edge_k.p2 - c1).normalized() * radius) + c1
			
			if _should_add_edge(edge_k, edge_next, edge_prev_in_loop):
				current_loop.append(p1)
				current_loop.append(p2)
				
				min_pt.x = minf(min_pt.x, p1.x)
				min_pt.y = minf(min_pt.y, p1.y)
				max_pt.x = maxf(max_pt.x, p1.x)
				max_pt.y = maxf(max_pt.y, p1.y)
				
				min_pt.x = minf(min_pt.x, p2.x)
				min_pt.y = minf(min_pt.y, p2.y)
				max_pt.x = maxf(max_pt.x, p2.x)
				max_pt.y = maxf(max_pt.y, p2.y)
			
			edge_prev_in_loop = edge_k

		if not current_loop.is_empty():
			loops.append({
				"poly": current_loop,
				"rect": Rect2(min_pt, max_pt - min_pt)
			})

	return loops

## Determines if a verticie should be added on this point depending of specified criterias.
## @param edge_k: The current edge.
## @param edge_next: The next edge in the loop.
## @param edge_prev_in_loop: The previous edge in the loop.
## @return: True if the edge should be added, false otherwise.
static func _should_add_edge(edge_k: Dictionary, edge_next: Dictionary, edge_prev_in_loop: Dictionary) -> bool:
	var prev_edge_line: Vector2 = edge_prev_in_loop.p2 - edge_prev_in_loop.p1
	var next_edge_line: Vector2 = edge_next.p2 - edge_next.p1
	
	if is_zero_approx(prev_edge_line.cross(next_edge_line)) and edge_prev_in_loop.p2.is_equal_approx(edge_k.p1):
		return false 
	
	return true

## Merges a hole polygon into a base polygon by creating a bridge between them.
static func _merge_hole_into_poly(base: PackedVector2Array, hole: PackedVector2Array, hole_rect: Rect2) -> PackedVector2Array:
	var min_dist := INF
	var best_base_idx := 0
	var best_hole_idx := 0
	
	var hole_end := hole_rect.end
	var hole_pos := hole_rect.position
	
	for i in range(base.size()):
		var base_pt := base[i]
		

		if min_dist != INF:
			var dx := 0.0
			if base_pt.x < hole_pos.x:
				dx = hole_pos.x - base_pt.x
			elif base_pt.x > hole_end.x:
				dx = base_pt.x - hole_end.x
				
			var dy := 0.0
			if base_pt.y < hole_pos.y:
				dy = hole_pos.y - base_pt.y
			elif base_pt.y > hole_end.y:
				dy = base_pt.y - hole_end.y
				
			if dx * dx + dy * dy >= min_dist:
				continue

		for j in range(hole.size()):
			var dist := base_pt.distance_squared_to(hole[j])
			if dist < min_dist:
				min_dist = dist
				best_base_idx = i
				best_hole_idx = j
			
	var merged := PackedVector2Array()
	
	for i in range(best_base_idx + 1):
		merged.append(base[i])
		
	for i in range(hole.size()):
		var idx := (best_hole_idx + i) % hole.size()
		merged.append(hole[idx])
		
	merged.append(hole[best_hole_idx]) 
	merged.append(base[best_base_idx]) 
	
	for i in range(best_base_idx + 1, base.size()):
		merged.append(base[i])
		
	return merged
