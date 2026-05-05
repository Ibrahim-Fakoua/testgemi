extends GenericBehavior

# The horrible verbose name is kinda self-explanatory
# Abstract class
class_name GenericFoodEntityBehavior

func on_eaten() -> void :
	if entity.ressource_stock >= 1 :
		entity.ressource_stock -= 1
	if entity.ressource_stock <= 0 : 
		on_ressource_emptied()

func on_ressource_emptied() -> void :
	pass
