## An abstract base class for all regions in the world.
## Regions are defined by a Polygon2D shape and can have associated [Stats].
extends Polygon2D
class_name Region


## The stats associated with this region.
@export var stats: Stats

## Prevents the Region class from being instantiated directly as it is abstract.
func _init() -> void:
	if get_script() == Region:
		assert(false,"Region is abstract and cannot be instantiated directly.")
		free() 


## Whether to show debug vertices for the region in the editor and at runtime.
var show_debug_vertices: bool = false

func _draw() -> void:
	if not show_debug_vertices:
		return

	var vertex_color: Color = Color(1, 0, 0) 
	var radius: float = 0.9 

	for vertex in polygon:

		draw_circle(vertex, radius, vertex_color)

func _process(_delta: float) -> void:

	queue_redraw()
