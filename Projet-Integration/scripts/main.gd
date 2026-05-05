extends Node2D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	if Controller:
		# We use 'self' instead of 'this'
		Controller.MainSceneRef = self
		print("[Main] Successfully handed reference to Controller.")
	else:
		printerr("[Main] Could not find Controller instance!")


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	pass
