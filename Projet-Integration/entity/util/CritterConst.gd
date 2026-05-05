extends Node

class_name CritterConst

## The base amount of time units it takes before a creature dies of old age
## [br][br]TIMESTAMPS_PER_DAY * 7
const BASE_LIFESPAN: int = 20160
## The base amount of time units it takes for a creature to give birth after becoming pregnant
## [br][br]BASE_LIFESPAN * 2/100
const BASE_GESTATION_TIME: int = 404
## The base amount of time units it takes for a creature to die of thirst
## [br][br]TIMESTAMPS_PER_DAY * 1.5
const BASE_DEHYDRATION_TIME: int = 4320
## The base amount of time units it takes for a creature to die of hunger
## [br][br]TIMESTAMPS_PER_DAY * 1.5
const BASE_STARVATION_TIME: int = 3000
## The base amount of time units it takes for a creature to heal from an injury
## [br][br]TIMESTAMPS_PER_DAY * 3/10
const BASE_HEALING_TIME: int = 864
## The base amount of time units it takes for a creature to die of lack of sleep
## [br][br]TIMESTAMPS_PER_DAY * 2.5
const BASE_SLEEP_DEPRIVATION_TIME = 7200
## The base amount of time units it takes for a creature to move one tile
## [br][br]TIMESTAMPS_PER_MINUTE * 4
const BASE_MOVEMENT_SPEED: int = 8
## The base amount of time units it takes for a creature to eat
## [br][br]TIMESTAMPS_PER_MINUTE * 6
const BASE_EATING_SPEED: int = 12

const BASE_MATING_SPEED: int = 300

const BASE_MATING_COOLDOWN: int = 300


const PREDATOR_SPEED_BOOST: float = 0.7
const FLEEING_SPEED_BOOST: float = 0.8

## The scale applied on entity images
const SPRITE_SCALE: float = 0.1
## The x offset applied on entity images
const SPRITE_X_OFFSET: int = 65
## The y offset applied on entity images
const SPRITE_Y_OFFSET: int = 120

const ActionNames := {
	EAT = "EAT",
	MOVE_TO = "MOVE_TO",
	WANDER = "WANDER",
	MATE = "MATE",
	SLEEP = "SLEEP",
	FLEE = "FLEE",
	HUNT = "HUNT"
}

const ConditionTypes := {
	HUNGER_REACHED = "HUNGER_REACHED",
	HUNGER_UNDER = "HUNGER_UNDER",
	FATIGUE_REACHED = "FATIGUE_REACHED",
	FATIGUE_UNDER = "FATIGUE_UNDER",
	CAN_BREED = "CAN_BREED",
	DONE_BREEDING = "DONE_BREEDING",
	GET_INJURED = "GET_INJURED",
	IS_INJURED = "IS_INJURED",
	SENSE_DANGER = "SENSE_DANGER",
	SAFE = "SAFE"
}
