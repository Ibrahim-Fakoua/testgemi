extends Node

var last_critter_id:int = 0

func get_critter_id() :
	last_critter_id += 1
	return last_critter_id

# Species :
# Specie 1 : Average in every facet
# Specie 2 : A potent predator with a lot of stamina who sleeps a lot
# Specie 3 : Intimidating Omnivore who prefers meat over fruit
# Specie 4 : Omnivore who likes confrontation but often hits above his weight and flees. 
# Specie 5 : Weak slow herbivore who has a long lifespan. Does not get hungry often.
# Specie 6 : Scaredy quick herbivore who avoids interaction with other species to survive. Does not sleep much.
# Specie 7 : Carnivore whose main source of food is carcasses left by other animals. Quick
# Specie 8 : Slow herbivore who uses camouflage to seem more intimidating to its predators. Sleeps a lot. Long lifespan.
# Specie 9 : A quick herbivore prey to everything and not smart enough to run in most situations. Reproduces a lot. Very short lifespan.
# Invasive Specie 1 : Hungry super predator to any other critter, eats and scares away every critter
# Invasive Specie 2 : Excessively quick herbivore that reproduces and eats excessively. Ruins ecosystems, leading to critters struggling to feed themselves
# Called when the node enters the scene tree for the first time.

enum Species {
	SPECIE_1,
	SPECIE_2,
	SPECIE_3,
	SPECIE_4,
	SPECIE_5,
	SPECIE_6,
	SPECIE_7,
	SPECIE_8,
	SPECIE_9,
	INVASIVE_SPECIE_1,
	INVASIVE_SPECIE_2
}

var SpecieSprite:Dictionary[Species, String] = {
	Species.SPECIE_1: "res://assets/entities/CritterVariant1.png",
	Species.SPECIE_2: "res://assets/entities/CritterVariant2.png",
	Species.SPECIE_3: "res://assets/entities/CritterVariant3.png",
	Species.SPECIE_4: "res://assets/entities/CritterVariant4.png",
	Species.SPECIE_5: "res://assets/entities/CritterVariant5.png",
	Species.SPECIE_6: "res://assets/entities/CritterVariant6.png",
	Species.SPECIE_7: "res://assets/entities/CritterVariant7.png",
	Species.SPECIE_8: "res://assets/entities/CritterVariant8.png",
	Species.SPECIE_9: "res://assets/entities/CritterVariant9.png",
	Species.INVASIVE_SPECIE_1: "res://assets/entities/InvasiveCritter1.png",
	Species.INVASIVE_SPECIE_2: "res://assets/entities/InvasiveCritter2.png"
}

var SpecieAttributes:Dictionary[Species, Callable] = {
	Species.SPECIE_1: func(att) : pass,
	Species.SPECIE_2: func(att) : 
		att.lifespan_modifier = 1.3
		att.base_intimidation = 7
		att.fightscore = 8
		att.morale = 3
		att.food_chain = 6
		att.stamina = 30
		att.fatigue_rate = 1.6
		att.sleep_efficiency = 1.5,
	Species.SPECIE_3: func(att) : 
		att.lifespan_modifier = 1.5
		att.speed = 1.3
		att.base_intimidation = 9
		att.fightscore = 6
		att.morale = 3
		att.food_chain = 5
		att.breeding_cooldown = 2
		att.litter_size = 1,
	Species.SPECIE_4: func(att) : 
		att.speed = 0.8
		att.morale = 2
		att.food_chain = 6
		att.fightscore = 4
		att.stamina = 50,
	Species.SPECIE_5: func(att) : 
		att.speed = 1.8
		att.lifespan_modifier = 1.6
		att.food_chain = 2
		att.fightscore = 5
		att.breeding_cooldown = 2
		att.litter_size = 2
		att.hunger_rate = 1.6,
	Species.SPECIE_6: func(att) : 
		att.lifespan_modifier = 0.8
		att.base_intimidation = 2
		att.morale = 1
		att.fightscore = 4
		att.food_chain = 2
		att.sleep_efficiency = 1.4,
	Species.SPECIE_7: func(att) : 
		att.speed = 0.8
		att.base_intimidation = 2
		att.fightscore = 6
		att.morale = 2
		att.food_chain = 4
		att.litter_size = 2,
	Species.SPECIE_8: func(att) : 
		att.speed = 1.8
		att.lifespan_modifier = 1.4
		att.base_intimidation = 8
		att.food_chain = 3,
	Species.SPECIE_9: func(att) : 
		att.speed = 0.6
		att.lifespan_modifier = 0.6
		att.base_intimidation = 5
		att.fightscore = 1
		att.food_chain = 1
		att.litter_size = 3
		att.breeding_cooldown = 0.8,
	Species.INVASIVE_SPECIE_1: func(att) : 
		att.speed = 0.7
		att.base_intimidation = 10
		att.fightscore = 10
		att.morale = 5
		att.stamina = 30
		att.lifespan_modifier = 1.5
		att.fatigue_rate = 1.2
		att.hunger_rate = 0.7,
	Species.INVASIVE_SPECIE_2: func(att) : 
		att.speed = 0.7
		att.breeding_cooldown = 0.5
		att.litter_size = 4
		att.gestation_time = 0.6
		att.hunger_rate = 0.6
}

var SpecieTags:Dictionary[Species, Array] = {
	Species.SPECIE_1: ["omnivore"],
	Species.SPECIE_2: ["carnivore", "predator"],
	Species.SPECIE_3: ["omnivore"],
	Species.SPECIE_4: ["omnivore"],
	Species.SPECIE_5: ["herbivore", "forager"],
	Species.SPECIE_6: ["herbivore", "forager"],
	Species.SPECIE_7: ["carnivore", "forager"],
	Species.SPECIE_8: ["herbivore", "forager"],
	Species.SPECIE_9: ["omnivore", "forager"],
	Species.INVASIVE_SPECIE_1: ["carnivore", "predator"],
	Species.INVASIVE_SPECIE_2: ["herbivore", "forager"]
}

var SpecieStateModifications:Dictionary[Species, Callable] = {}
# Called every frame. 'delta' is the elapsed time since the previous frame.
