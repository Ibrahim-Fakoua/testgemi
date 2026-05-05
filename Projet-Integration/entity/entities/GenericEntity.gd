extends Node

## Anything in the simulation which has a position on a tile and the ability to schedule events
class_name GenericEntity

## The position on the tilemap of the entity
var position: Vector3i
## Stores all events scheduled by the entity 
## [br][br] Events are stored and removed from here dynamically
## [br][br] The events are classified into lists depending on their type
var scheduled_events: TypedEventList = TypedEventList.new()
## Determines how the entity reacts to external triggers
var behavior: GenericBehavior
## The entity's sprite to be displayed on the tilemap
var sprite: Sprite2D
## The pathfinding service that lets the entity know its position on the tilemap
var pathfinding: PathfindingService
var tags: Array[String] = []
var main_tag: String

var type = "Entity"

func _init(pathfinding_service: PathfindingService, position: Vector3i, behavior: GenericBehavior, main_tag: String="entity") :
	self.behavior = behavior
	self.main_tag = main_tag
	self.tags.append(main_tag)
	self.position = position
	pathfinding = pathfinding_service
	pathfinding.disable_tile(position,true)
	
	if not pathfinding.validate_tile(position):
		push_error("POSITION DOES NOT EXIST")
	
	var chunk = pathfinding.get_chunk_at_tile(position)
	if chunk: 
		chunk.add_to_chunk(self)
	pathfinding.tilemap.add_child(self)

## Initializes the entity's sprite image to be displayed appropriately
func _initialize_sprite(filepath:String) -> void :
	sprite = Sprite2D.new()
	var texture: Texture2D = load(filepath)
	sprite.texture = texture
	sprite.apply_scale(Vector2(CritterConst.SPRITE_SCALE, CritterConst.SPRITE_SCALE))
	sprite.offset = Vector2(CritterConst.SPRITE_X_OFFSET, CritterConst.SPRITE_Y_OFFSET)
	add_child(sprite)
	sprite.position = pathfinding.tilemap.map_to_local(pathfinding.tilemap.cube_to_map(position))

## Schedules an event in the scheduler
## [br][br] ONLY USE THIS METHOD TO SCHEDULE EVENTS WHEN AN ENTITY SCHEDULES AN EVENT
## DO NOT USE THE VERSION OF THE METHOD IN Scheduler.gd
func schedule_event(timestamp:int, event:TypedEvent) -> void :
	scheduled_events.add_event(event)
	(Controller.Scheduler as Scheduler).schedule_event(timestamp, event)

## Cancels an event the entity has previously scheduled
## [br][br] ONLY USE THIS METHOD TO CANCEL EVENTS WHEN AN ENTITY CANCELS AN EVENT
## DO NOT USE THE VERSION OF THE METHOD IN Scheduler.gd
func cancel_event(event_id:String) :
	var target_event = scheduled_events.find_by_id(event_id)
	scheduled_events.remove_event(target_event)
	(Controller.Scheduler as Scheduler).cancel_event(target_event)

## Cancels all events of a certain type that the entity has previously scheduled
func cancel_events_of_type(event_type:String) :
	var target_events = scheduled_events.get_events_of_type(event_type)
	scheduled_events.remove_events_of_type(event_type)
	for event in target_events.duplicate() :
		(Controller.Scheduler as Scheduler).cancel_event(event)
