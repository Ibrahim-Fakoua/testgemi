// extends Node
//
// class_name PathfindingService
//
// #radius of an area, changing the constant breaks everything as i have the calculations hardcoded at 5
// const radius = 4
// var tilemap : MainLayer 
//
// #only instantiate this bad boy when the map is fully made
//
//
// func _init(current_tilemap : MainLayer) -> void:
// 	tilemap = current_tilemap
//
// func create_pathfinding():
// 	var areas = designate_areas()
// 	create_areas(areas)
// 	fix_adjacency_for_chunks(areas)
//
// ##returns an array of vectors for all portions of the tilemap that can house a valid area
// func designate_areas() -> Array[Vector2i]:
// 	var increment = 0
// 	var valid_areas : Array[Vector2i] = []
// 	while true:
// 		var possible_areas : Array[Vector3i] = tilemap.cube_ring(Vector3i(0,0,0),increment)
// 		var continue_ring = false
// 		for area_3i in possible_areas:
// 			var area_2i = tilemap.cube_to_map(area_3i)
// 			var valid = validate_area(area_2i)
// 			if valid:
// 				valid_areas.append(area_2i)
// 				continue_ring = true
// 		if continue_ring == false:
// 			break
// 		increment = increment + 1
// 	return valid_areas
//
// func create_areas(areas_to_make: Array[Vector2i]):
// 	for area_vector in areas_to_make:
// 		var index = areas_pos.bsearch(area_vector)
// 		if areas_pos.size() == index or (areas_pos.get(index) != area_vector):
// 			areas_pos.insert(index,area_vector)
// 			var new_area = Area.new(tilemap)
// 			areas.insert(index,new_area)
// 		elif (areas_pos.get(index) == area_vector):
// 			var new_area = Area.new(tilemap)
// 			var area = areas.get(index)
// 			area.destroy()
// 			areas.set(index,area)
// 		
// 		var current_area = areas.get(index)
// 		var tiles_in_area = get_all_tiles_in_area(area_vector)
// 		
// 		current_area.rebuild_chunks(area_vector,tiles_in_area)
//
//
// func fix_adjacency_for_chunks(areas_to_check: Array[Vector2i]):
// 	for area_vector in areas_to_check:
// 		var index = areas_pos.bsearch(area_vector)
// 		var area = areas.get(index)
// 		var neighbors = tilemap.cube_neighbors(tilemap.map_to_cube(area_vector))
// 		var chunk_queue : Array[Chunk] = []
// 		
// 		for initial_chunk : Chunk in area.chunks:
// 			for destination_chunk : Chunk in area.chunks:
// 				if initial_chunk != destination_chunk:
// 					if not destination_chunk.are_you_my_neighbor(initial_chunk):
// 						if are_chunks_adjacent(initial_chunk,destination_chunk):
// 							initial_chunk.neighbors.append(destination_chunk)
// 							destination_chunk.neighbors.append(initial_chunk)
// 			
// 			for neighboring_area in neighbors:
// 				var neighboring_area_2i = tilemap.cube_to_map(neighboring_area)
// 				var actual_neighboring_area_index = areas_pos.bsearch(neighboring_area_2i)
// 				if neighboring_area_2i == areas_pos.get(actual_neighboring_area_index):
// 					var actual_neighboring_area = areas.get(actual_neighboring_area_index) 
// 					
// 					for chunk in actual_neighboring_area.chunks:
// 						chunk_queue.append(chunk)
// 			
// 			for destination_chunk in chunk_queue:
// 				if not destination_chunk.are_you_my_neighbor(initial_chunk):
// 					if are_chunks_adjacent(initial_chunk,destination_chunk):
// 						initial_chunk.neighbors.append(destination_chunk)
// 						destination_chunk.neighbors.append(initial_chunk)
//
//
// ## takes a tile and returns all possible moves from this tile's position, returns an empty array if no moves are possible
// func possible_moves(origin : Vector3i): 
// 	disable_tile(origin,false)
// 	if validate_tile(origin):
// 		var origin_id = tilemap.pathfinding_get_point_id(tilemap.cube_to_map(origin))
// 		var potentially_valid_move = tilemap.astar.get_point_connections(origin_id)
// 		var valid_moves : Array[Vector3i] = []
// 		for move in potentially_valid_move:
// 			if not tilemap.astar.is_point_disabled(move):
// 				var cube_move = tilemap.local_to_cube(tilemap.astar.get_point_position(move))
// 				if validate_tile(cube_move): 
// 					valid_moves.append(cube_move)
// 		disable_tile(origin,true)
// 		return valid_moves
// 	else:
// 		push_warning("Origin tile does not exist")
//
//
// ##returns the tile the moving entity needs to go to to get closes to the given tags
// ##returns null if the target is too far, not reachable or non existent
// func move_to_closest_thing(origin : Vector3i, tags : Array[String],filter : Array[String] = []):
// 	var found_chunk : Chunk = search_for_things(origin,12,tags,filter)
// 	if found_chunk:
// 		var origin_chunk = get_chunk_at_tile(origin)
// 		if origin_chunk == found_chunk and origin_chunk != null:
// 			var thing : GenericEntity = found_chunk.find_nearest_thing(tilemap,origin,tags)
// 			if thing != null:
// 				var point1 = tilemap.pathfinding_get_point_id(tilemap.cube_to_map(origin))
// 				var point2 = tilemap.pathfinding_get_point_id(tilemap.cube_to_map(thing.position))
// 				var path = pathfind(point1,point2)
// 				if path and path.size() > 1:
// 					if path.size() <= 2:
// 						return thing
// 					elif path.size() > 2:
// 						return tilemap.local_to_cube(path.get(1))
// 				else:
// 					return null
// 			else:
// 				return null
// 		else: 
// 			var point1 = tilemap.pathfinding_get_point_id(tilemap.cube_to_map(origin))
// 			var point2 = tilemap.pathfinding_get_point_id(tilemap.cube_to_map(found_chunk.tiles.get(0)))
// 			var path = pathfind(point1,point2)
// 			if path and path.size() > 0:
// 				return tilemap.local_to_cube(path.get(1))
// 			else:
// 				return null
// 	else: 
// 		return null
//
// ## returns null if the target is not in danger or returns the closest threat
// func am_i_in_danger(starting_position : Vector3i, fightscore : int,morale : int, filter : Array[String] = []):
// 	var chunk : Chunk = get_chunk_at_tile(starting_position)
// 	var closest_danger : GenericEntity
// 	var chunks_to_check : Array[Chunk] = [chunk]
// 	for neighboring_chunks : Chunk in chunk.neighbors:
// 		chunks_to_check.append(neighboring_chunks)
// 	
// 	for individual_chunk in chunks_to_check:
// 		var potential_dangers = individual_chunk.get_all_with_tags(starting_position,["critter"])
// 		for danger : GenericEntity in potential_dangers:
// 			var valid = true
// 			for filtered_tag : String in filter:
// 				if danger.tags.has(filtered_tag):
// 					valid = false
// 					break
// 			if valid and tilemap.cube_distance(starting_position,danger.position) <= 8:
// 				var danger_score = danger.get_intimidation() - fightscore
// 				if danger_score > morale:
// 					if closest_danger:
// 						
// 						if tilemap.cube_distance(starting_position,danger.position) < tilemap.cube_distance(starting_position,closest_danger.position):
// 							closest_danger = danger
// 					else:
// 						closest_danger = danger
// 	
// 	if closest_danger:
// 		return closest_danger
// 	else:
// 		return null
//
// func move_away_from(origin : Vector3i, scary : GenericEntity):
// 	var moves = possible_moves(origin)
// 	var furthest_moves : Array[Vector3i] 
// 	var test = []
// 	
// 	var d = 0
// 	for move in moves:
// 		var d1 = tilemap.cube_distance(move,scary.position)
// 		if d1 > d:
// 			d = d1
// 			furthest_moves.clear()
// 			furthest_moves.append(move)
// 		elif d1 == d:
// 			furthest_moves.append(move)
// 	
// 	var far_move = furthest_moves.get(randi() % furthest_moves.size())
// 	return far_move
//
// ## returns null if i cannot find a prey within 12 chunks, otherwise, it returns an critter that has a lower foodchain score than this critter
// func get_nearest_prey(origin : Vector3i, food_chain : int, filter : Array[String] = []):
// 	var chunk = get_chunk_at_tile(origin)
// 	var queue : Array[Chunk] = [chunk]
// 	var chunks_to_reset : Array[Chunk] = [chunk]
// 	chunk.reset()
// 	chunk.from = [chunk]
// 	
// 	var next_queue : Array[Chunk] = []
// 	var max_depth = 12
// 	var depth = 0
//
// 	var results : Array[GenericEntity] = []
//
// 	while depth <= max_depth:
// 		if queue.is_empty():
// 			if results.is_empty():
// 				depth = depth+1
// 				if next_queue.is_empty():
// 					break
// 				else:
// 					for next_chunk in next_queue:
// 						queue.append(next_chunk)
// 						next_chunk.depth = depth
// 					next_queue.clear()
// 			else :
// 				break
// 		else: 
// 			var source : Chunk = queue.get(0)
// 			var targets = source.get_all_with_tags(origin,["critter"],filter)
// 			for critter in targets:
// 				if critter.get_food_chain() >= food_chain:
// 					targets.erase(critter)
// 			if targets.size() > 0:
// 				for target in targets:
// 					results.append(target)
// 			else:
// 				for neighbor in source.neighbors:
// 					if neighbor.unexplored and not next_queue.has(neighbor):
// 						next_queue.append(neighbor)
// 						neighbor.unexplored = false
// 						neighbor.depth = depth+1
// 						chunks_to_reset.append(neighbor)
// 					if depth < 1:
// 						neighbor.from.append(neighbor)
// 					else:
// 						for i in source.from:
// 							if neighbor.depth == depth+1 and not neighbor.from.has(i):
// 								neighbor.from.append(i)
// 			queue.remove_at(0)
// 	
// 	var final_choice : GenericEntity
// 	if results.is_empty():
// 		final_choice = null
// 	else:
// 		var d = 9999999
// 		var closest_prey = []
// 		for potential_prey in results:
// 			if tilemap.cube_distance(potential_prey.position,origin) < d:
// 				d = tilemap.cube_distance(potential_prey.position,origin)
// 				closest_prey.clear()
// 				closest_prey.append(potential_prey)
// 			elif tilemap.cube_distance(potential_prey.position,origin) == d:
// 				closest_prey.append(potential_prey)
// 		
// 		final_choice = closest_prey.get(randi() % closest_prey.size())  
// 	
// 	for chunk_to_fix in chunks_to_reset:
// 		chunk_to_fix.reset()
// 	
// 	return final_choice
//
// func search_for_things(starting_position : Vector3i,max_depth : int, things : Array[String], filter : Array[String]):
// 	var chunk = get_chunk_at_tile(starting_position)
// 	var queue : Array[Chunk] = [chunk]
// 	var chunks_to_reset : Array[Chunk] = [chunk]
// 	chunk.reset()
// 	chunk.from = [chunk]
// 	
// 	var next_queue : Array[Chunk] = []
// 	var depth = 0
//
// 	var results : Array[Chunk] = []
//
// 	while depth <= max_depth:
// 		if queue.is_empty():
// 			if results.is_empty():
// 				depth = depth+1
// 				if next_queue.is_empty():
// 					break
// 				else:
// 					for next_chunk in next_queue:
// 						queue.append(next_chunk)
// 						next_chunk.depth = depth
// 					next_queue.clear()
// 			else :
// 				break
// 		else: 
// 			var source : Chunk = queue.get(0)
// 			var targets = source.get_all_with_tags(starting_position,things,filter)
// 			if targets.size() > 0:
// 				results.append(source)
// 			else:
// 				for neighbor in source.neighbors:
// 					if neighbor.unexplored and not next_queue.has(neighbor):
// 						next_queue.append(neighbor)
// 						neighbor.unexplored = false
// 						neighbor.depth = depth+1
// 						chunks_to_reset.append(neighbor)
// 					if depth < 1:
// 						neighbor.from.append(neighbor)
// 					else:
// 						for i in source.from:
// 							if neighbor.depth == depth+1 and not neighbor.from.has(i):
// 								neighbor.from.append(i)
// 			queue.remove_at(0)
// 	
// 	var final_choice : Chunk
// 	if results.is_empty():
// 		final_choice = null
// 	else:
// 		var chosen_chunk = results.get(randi() % results.size())
// 		final_choice = chosen_chunk.from.get(randi() % chosen_chunk.from.size())
// 	
// 	for chunk_to_fix in chunks_to_reset:
// 		chunk_to_fix.reset()
// 	
// 	return final_choice
//
//
//
// func highlight_tiles(tiles_to_highlight: Array[Vector3i],variant):
// 	for tile in tiles_to_highlight:
// 		var tile_2i = tilemap.cube_to_map(tile)
// 		tilemap.set_cell(tile_2i,0,Vector2i(variant,1))
//
// func small_to_big_unit_test(areas_to_check : Array[Vector2i]):
// 	for area_2i in areas_to_check:
// 		var tiles_to_check = get_all_tiles_in_area(area_2i)
// 		for tile in tiles_to_check:
// 			var supposed_area = small_hex_to_big(tile)
// 			if not is_tile_in_area(tile,supposed_area):
// 				highlight_tiles([tile],3)
//
// #Hardcoded to tiles with a radius of 5 and a diameter of 9 tiles (goofy i know)
// func big_hex_to_small(big_hex: Vector2i) -> Vector3i:
// 	var x = big_hex.x*Vector3i(9,-5,-4)
// 	var y = big_hex.y*Vector3i(-5,-4,9)
// 	
// 	return x + y
//
// func small_hex_to_big(small_hex: Vector3i) -> Vector2i:
// 	const area : float = 61
// 	const shift : float = 14
// 	var x : float = small_hex.x 
// 	var z : float = small_hex.y
// 	var y : float = small_hex.z
// 	var xh = floor((y+shift*x) / area)
// 	var yh = floor((z+shift*y) / area)
// 	var zh = floor((x+shift*z) / area)
// 	var i = floor( (1+xh-yh) / 3)
// 	var j = floor( (1+yh-zh) / 3)
// 	var k = floor( (1+zh-xh) / 3)
// 	var result = Vector3i(i,j,k)
// 	return tilemap.cube_to_map(result)
//
// func disable_tile(coords: Vector3i,disable : bool = true):
// 	var id = tilemap.pathfinding_get_point_id(tilemap.cube_to_map(coords))
// 	tilemap.astar.set_point_disabled(id,disable)
//
// func disable_point(id: int,disable : bool = true):
// 	tilemap.astar.set_point_disabled(id,disable)
//
// func pathfind(point1,point2):
// 	disable_point(point1,false)
// 	disable_point(point2,false)
// 	var path = tilemap.astar.get_point_path(point1,point2)
// 	disable_point(point1,true)
// 	disable_point(point2,true)
// 	return path
//
// func validate_tile(coords: Vector3i) -> bool:
// 	var tile2i = tilemap.cube_to_map(coords)
// 	if tilemap.get_cell_tile_data(tile2i) != null:
// 		return true
// 	else:
// 		return false
//
// class Area:
// 	var chunks : Array[Chunk] = []
// 	var remaining_tiles : Array[Vector3i]
// 	var tilemap : MainLayer
// 	
// 	func _init(new_tilemap):
// 		tilemap = new_tilemap
// 	
// 	func filter_for_chunks(tile : Vector3i):
// 		var data1 = tilemap.get_cell_tile_data(tilemap.cube_to_map(tile))
// 		if data1 and not data1.get_custom_data("Impassable"):
// 			for i in remaining_tiles:
// 				if i == tile:
// 					return true
// 		return false
// 	
// 	func destroy():
// 		for chunk in chunks:
// 			chunk.destroy()
// 	
// 	func rebuild_chunks(area_vector: Vector2i,tiles_in_area : Array[Vector3i]):
// 		remaining_tiles = tiles_in_area.duplicate()
// 		while not remaining_tiles.is_empty():
// 			var start_tile = remaining_tiles.get(0)
// 			var data = tilemap.get_cell_tile_data(tilemap.cube_to_map(start_tile))
// 			if not data or data.get_custom_data("Impassable"):
// 				remaining_tiles.erase(start_tile)
// 			else:
// 				var results = tilemap.cube_explore(start_tile,filter_for_chunks)
// 				if not results.is_empty():
// 					var new_chunk = Chunk.new(area_vector,results)
// 					self.chunks.append(new_chunk)
//
// 					for result_tile in results:
// 						remaining_tiles.erase(result_tile)
// 				else:
// 					push_error("Nothing to explore")
//
// var areas_pos : Array[Vector2i] = []
// var areas : Array[Area] = []
//
// func get_area_from_tile(tile : Vector3i) -> Area:
// 	if validate_tile(tile):
// 		var area_coord = small_hex_to_big(tile)
// 		var index = areas_pos.bsearch(area_coord)
// 		if index >= 0:
// 			return areas.get(index)
// 		else:
// 			push_error("Area cannot be fetched")
// 	else:
// 		push_error("Tile does not exist")
// 	return null
//
// func Area_test(area_array : Array[Vector2i]):
// 	var counter = 1
// 	for area_2i in area_array:
// 		var tiles = get_all_tiles_in_area(area_2i)
// 		highlight_tiles(tiles,counter)
// 		counter = counter+1
//
// func get_all_tiles_in_area(coords: Vector2i) -> Array[Vector3i]:
// 	var center = big_hex_to_small(coords)
// 	return tilemap.cube_range(center,radius)
// 	
// func validate_area(coords: Vector2i) -> bool:
// 	var possible_tiles = get_all_tiles_in_area(coords)
// 	for tile in possible_tiles:
// 		if validate_tile(tile):
// 			return true
// 	return false
//
// func is_tile_in_area(tile : Vector3i, area : Vector2i) -> bool:
// 	var tiles = get_all_tiles_in_area(area)
// 	for potential_tile in tiles:
// 		if potential_tile == tile:
// 			return true
// 	return false
//
// class Chunk:
// 	var tiles : Array[Vector3i]
// 	var contents = []
// 	var depth = 0
// 	var unexplored = true
// 	var from = []
// 	
// 	var area : Vector2i
// 	var neighbors : Array[Chunk] 
// 	
// 	func reset():
// 		depth = 0
// 		unexplored = true
// 		from = []
// 	
// 	func destroy():
// 		for neighbor in neighbors:
// 			neighbor.neighbors.erase(self)
// 	
// 	func content_sort(a : GenericEntity, b : GenericEntity):
// 		if a.tags.size() == 0 or b.tags.size() == 0:
// 			push_warning("Creature has no tags!") 
// 		elif a.tags.get(0) < b.tags.get(0):
// 			return true
// 		return false
// 	
// 	func add_to_chunk(thing : GenericEntity):
// 		var index = contents.bsearch_custom(thing, content_sort)
// 		contents.insert(index, thing)
// 	
// 	func does_chunk_have_tag(thing) -> bool:
// 		for entity : GenericEntity in contents:
// 			for tag in entity.tags:
// 				if tag == thing:
// 					return true
// 		return false
// 	
// 	func get_all_with_tags(starting_position : Vector3i, searched_tags : Array[String], filter : Array[String] = []):
// 		var result = []
// 		for entity : GenericEntity in contents:
// 			if entity.position != starting_position and not result.has(entity):
// 				var count = 0
// 				for searched_tag in searched_tags:
// 					if entity.tags.has(searched_tag):
// 						count = count+1
// 				if count >= searched_tags.size():
// 					result.append(entity)
// 		for entity in result:
// 			for tag in filter:
// 				if entity.tags.has(tag):
// 					result.erase(entity)
// 		return result
// 	
// 	func remove_from_chunk(thing : GenericEntity):
// 		contents.erase(thing)
// 	
// 	func move_to_chunk(thing,destination : Chunk):
// 		contents.erase(thing)
// 		destination.add_to_chunk(thing)
// 	
// 	func are_you_my_neighbor(potential_neighbor : Chunk) -> bool:
// 		for neighbor in neighbors:
// 			if potential_neighbor == neighbor:
// 				return true
// 		return false
// 	
// 	func is_tile_in_chunk(potential_tile : Vector3i) -> bool:
// 		for tile in self.tiles:
// 			if tile == potential_tile:
// 				return true
// 		return false
// 	
// 	func find_nearest_thing(tilemap : MainLayer, origin : Vector3i, searched_tags):
// 		var Distance = 9999
// 		var chosen_thing
// 		for thing : GenericEntity in get_all_with_tags(origin,searched_tags):
// 			var d = tilemap.cube_distance(origin,thing.position)
// 			if d < Distance:
// 				Distance = d
// 				chosen_thing = thing
// 		if chosen_thing == null:
// 			push_warning("You are searching for nothing.")
// 		return chosen_thing
// 		
// 	func _init(area: Vector2i, tiles_in_chunk: Array[Vector3i]):
// 		self.area = area
// 		self.tiles = tiles_in_chunk
//
// func Area_chunk_test(area_array : Array[Vector2i]):
// 	for area_2i in area_array:
// 		var area_index = areas_pos.bsearch(area_2i)
// 		var area = areas.get(area_index)
// 		var counter = 0
// 		for i in area.chunks:
// 			highlight_tiles(i.tiles,counter)
// 			counter = counter + 1
//
// func are_chunks_adjacent(entry_chunk : Chunk,exit_chunk : Chunk) -> bool:
// 	if entry_chunk != exit_chunk:
// 		for entry_tile in entry_chunk.tiles:
// 			for exit_tile in exit_chunk.tiles:
// 				var astar_entry_tile = tilemap.pathfinding_get_point_id(tilemap.cube_to_map(entry_tile))
// 				var astar_exit_tile = tilemap.pathfinding_get_point_id(tilemap.cube_to_map(exit_tile))
// 				var result = tilemap.astar.are_points_connected(astar_entry_tile,astar_exit_tile)
// 				if result:
// 					return true
// 	else:
// 		push_warning("Entry chunk is identical to exit chunk")
// 	return false
//
// func am_i_switching_chunks(thing, tile1 : Vector3i,tile2 : Vector3i):
// 	if tile1 == tile2:
// 		push_warning("destination is current position")
// 	var chunk1 = get_chunk_at_tile(tile1)
// 	var chunk2 = get_chunk_at_tile(tile2)
// 	if chunk2 == null:
// 		push_error("Non-existent destination")
// 	if chunk1 != chunk2:
// 		chunk1.move_to_chunk(thing,chunk2)
// 		return true
// 	else:
// 		return false
//
// func get_chunk_at_tile(tile : Vector3i) -> Chunk:
// 	var area = get_area_from_tile(tile)
// 	for chunk : Chunk in area.chunks:
// 		if chunk.is_tile_in_chunk(tile):
// 			return chunk
// 	push_error("TILE NOT IN AREA")
// 	return null