## When you have seperate regions on the map that points to the same biome, this class is used to distinguish them.
## A BiomeRegion is a Polygon2d that follows the border of a region.
## Each BiomeRegion has a Stats object.
extends Region
class_name  BiomeRegion


## Initializes the biome region by duplicating the biome's base stats and registering itself.
## @param biome: The [Biome] this region belongs to.
func _init(biome : Biome) -> void:
	stats = biome.biome_base_stats.duplicate() as Stats
	stats.parent = biome
	biome.add_region_stats(self.stats)
	
