extends Camera2D


@export var zoom_factor: float = 1.1  
@export var min_zoom: float = 0.001 
@export var max_zoom: float = 10
@export var speed = 1200
var is_panning: bool = false
func _process(delta: float) -> void:

	var direction = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")


	position += direction * speed * delta
func _unhandled_input(event: InputEvent) -> void:

	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_MIDDLE:
			is_panning = event.is_pressed()
	

	if event is InputEventMouseMotion and is_panning:

		position -= event.relative / zoom
		

	if event is InputEventMouseButton and event.is_pressed():
		var target_zoom = zoom
		
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:

			target_zoom *= zoom_factor
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:

			target_zoom /= zoom_factor
			

		zoom = target_zoom.clamp(Vector2(min_zoom, min_zoom), Vector2(max_zoom, max_zoom))
