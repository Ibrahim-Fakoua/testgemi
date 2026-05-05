extends GenericFoodEntity

class_name ReplinishingFoodSource

var filled_sprite: String 
var empty_sprite: String
var is_filled: bool = true
var regeneration_time: int

func _init(pathfinding, position:Vector3i, max_ressource_capacity:int, regeneration_time:int, filled_sprite:String, empty_sprite:String) -> void :
	super._init(pathfinding, max_ressource_capacity, position, ReplinishingFoodSourceBehavior.new(self))
	self.filled_sprite = filled_sprite
	self.empty_sprite = empty_sprite
	self.regeneration_time = regeneration_time
	_initialize_sprite(filled_sprite)

func regenerate() -> void :
	if ressource_stock <= 0 :
		change_sprite(filled_sprite)
	if ressource_stock < max_ressource_capacity :
		ressource_stock += 1
	tags.erase("empty")
	behavior.on_regeneration()

func change_sprite(filepath:String) -> void :
	var texture: Texture2D = load(filepath)
	self.sprite.texture = texture
