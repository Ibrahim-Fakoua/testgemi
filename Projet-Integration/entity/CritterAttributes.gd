extends Node

# Class which stores all the critter's attributes and information relevant to its current state
class_name CritterAttributes

# ----Critter Attributes----
# These are all assigned at birth, constant throughout the creature's lifespan, and hereditary
# Except a few exceptions that serve to tell the creature's state (can_breed for example)

#PHYSICAL ATTRIBUTES
# Multiplier [0,87 - 0,95] - Determines how much injuries affect the critter's physical capabilities
var injury_resistance: float = 0.90
# Multiplier [0,5 - 2] - Determines how much time it takes for injuries to heal
var healing_capacity: float = 1
# Multiplier [0,5 - 3] - Determines how quickly the critter moves
var speed: float = 1
# Multiplier [0,5 - 2] - Determines how long a creature lives
var lifespan_modifier: float = 1

#HUNTING
# Number from 1 to 10 that determines whether it runs or flees
var base_intimidation: int = 5
# Number from 1 to 10 determining a the critter's fighting ability
var fightscore: int = 5
# The difference in fightscore and intimidation for a creature to flee
var morale: int = 3
# Number from 1 to 7 that determines a creature's place in the food chain (1 = prey to everything, 7 = predator to everything) 
var food_chain: int = 4
# How long a critter can run away from danger or towards a prey
var stamina: int = 15

#SLEEP
# Multiplier [0,5 - 1,75] - Determines how quickly sleeping recovers sleepiness 
var sleep_efficiency: float = 1
# Multiplier [0,5 - 1,2] - Determines how quickly the creature suffers from sleepiness
var fatigue_rate: float = 1

#BREEDING
# Multiplier [0,5 - any] - Determines how much time it takes before a creature is able to breed again
var breeding_cooldown: float = 1
# Determines the amount of offspring per pregnancy
var litter_size: int = 1
# Multiplier [0,5 - 2,5] - Determines the time between breeding and delivery of the offspring
var gestation_time: float = 1

#FOOD
var eating_speed: float = 1
# Multiplier [0,5 - 2] - Determines how much food the creature needs to survive
var hunger_rate: float = 1
# Multiplier [0,5 - 2] - Determines how much meat fills hunger and thirst
var meat_efficiency: float = 1
# Multiplier [0,5 - 2] - Determines how much fruit fills hunger and thirst
var fruit_efficiency: float = 1
