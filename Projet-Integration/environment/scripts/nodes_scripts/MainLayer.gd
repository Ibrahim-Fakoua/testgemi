@tool
class_name MainLayer extends HexagonTileMapLayer

func _pathfinding_does_tile_connect(tile: Vector2i, neighbor: Vector2i) -> bool:
	var data1 = self.get_cell_tile_data(tile)
	var data2 = self.get_cell_tile_data(neighbor)
	if data1 and data2:
		return not (data1.get_custom_data("Impassable") or data2.get_custom_data("Impassable"))
	else:
		return false

func _ready() -> void:
	super._ready()

	Controller.GDSubscribe("Terrarium_Virtuel.signals.MapDeleted", _on_clear)
	#Controller.GDSubscribe("Terrarium_Virtuel.scripts.world_generation.WorldGenCompleteSignal", _on_map_ready)
	Controller.GDSubscribe(" Terrarium_Virtuel.signals.NewCellsToAnimateSignal", _on_new_cells_to_animate)

	

func _on_new_cells_to_animate(_NewCellsToAnimateSignal):
	var cellslist = _NewCellsToAnimateSignal.Info
	for celldata in cellslist:
		set_cell(Vector2i(celldata[0],celldata[1]),celldata[6],Vector2i(celldata[4],celldata[5]),0)


func _on_clear(_nothing):
	self.clear()



func _on_new_cell(cell):
	if (!cell.finished):
		cell = cell.info
		#add_one_lable(cell[0],cell[1], cell[2],cell[3])
		set_cell(Vector2i(cell[0],cell[1]),cell[6],Vector2i(cell[4],cell[5]),0)

func _on_map_ready(signal_obj):
	# signal_obj is the C# 'MapGeneratedSignal' instance


	var data = signal_obj.MapData # This is an Array
	
	for row in data:             # 'row' is an Array
		for cell in row:         # 'cell' is a PackedInt32Array
			var x = cell[0]
			var y = cell[1]
			var atlas_x = cell[4]
			var atlas_y = cell[5]
			var source_id = cell[6]
			#add_one_lable(x,y,cell[2],cell[3])
			# Now you can use these to set tiles on a TileMap
			set_cell(Vector2i(x,y),source_id,Vector2i(atlas_x,atlas_y),0)
	
