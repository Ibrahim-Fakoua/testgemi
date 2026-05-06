extends Node

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_Q and event.ctrl_pressed:
			get_tree().reload_current_scene()
