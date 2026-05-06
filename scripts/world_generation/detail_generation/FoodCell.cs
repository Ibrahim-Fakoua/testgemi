using System;
using System.Collections.Generic;
using Godot;

namespace Terrarium_Virtuel.scripts.world_generation;

public class CollapsedFoodCellData
{
    public FoodType CollapsedState;

    public CollapsedFoodCellData(FoodType collapsedState)
    {
        CollapsedState = collapsedState;
    }
}


public class FoodCellData
{
    public (int x, int y) Position;

 
    private Dictionary<FoodType, float> _foodTypeProbabilities;
    private List<FoodType> _validFoodTypes;

    public FoodCellData((int x, int y) position, BiomeType biome)
    {
        Position = position;


        _validFoodTypes = FoodTypeExtensions.BiomeToFood[biome];


        _foodTypeProbabilities = new Dictionary<FoodType, float>();

        foreach (FoodType foodType in _validFoodTypes)
        {
            _foodTypeProbabilities[foodType] = FoodTypeExtensions.Probabilities[foodType];
        }
    }

    public List<FoodType> GetValidFoodTypes()
    {
        return _validFoodTypes;
    }

    public float GetFoodTypeProbability(FoodType foodType)
    {
        if (!_foodTypeProbabilities.ContainsKey(foodType)) return 0;
        return _foodTypeProbabilities[foodType];
    }

    public void SetFoodTypeProbability(FoodType foodType, float probability)
    {
        if (!_foodTypeProbabilities.ContainsKey(foodType)) return;
        _foodTypeProbabilities[foodType] = probability;
    }

    public void MultiplyFoodTypeByWeight(FoodType foodType, float weight)
    {
        if (!_foodTypeProbabilities.ContainsKey(foodType)) return;

        float oldProb = _foodTypeProbabilities[foodType];
        if (oldProb <= 0) return;

        _foodTypeProbabilities[foodType] *= weight;

        if (_foodTypeProbabilities[foodType] < 0)
        {
            _foodTypeProbabilities[foodType] = 0;
        }
    }

    public float GetProbabilitySum()
    {
        float sum = 0;
        foreach (var kvp in _foodTypeProbabilities)
        {
            sum += kvp.Value;
        }
        return sum;
    }
}


public class FoodCell
{
    public bool IsCollapsed = false;
    public FoodCellData FoodCellData;
    public CollapsedFoodCellData CollapsedFoodCellData;
    private RandomNumberGenerator _rng;

    public FoodCell((int x, int y) position, RandomNumberGenerator rng, BiomeType biome)
    {
        _rng = rng;
        FoodCellData = new FoodCellData(position, biome);
    }


    public void PickRandomWeightedFoodType()
    {
        float totalWeight = FoodCellData.GetProbabilitySum();

        if (totalWeight <= 0)
        {
            CollapsedFoodCellData = new CollapsedFoodCellData(FoodType.None);
            IsCollapsed = true;
            return;
        }

        float choice = _rng.Randf() * totalWeight;

        foreach (var foodType in FoodCellData.GetValidFoodTypes())
        {
            float prob = FoodCellData.GetFoodTypeProbability(foodType);
            if (prob <= 0) continue;

            if (choice <= prob)
            {
                CollapsedFoodCellData = new CollapsedFoodCellData(foodType);
                IsCollapsed = true;
                return;
            }

            choice -= prob;
        }


        CollapsedFoodCellData = new CollapsedFoodCellData(FoodType.None);
        IsCollapsed = true;
    }

    public (int q, int r) AxialCoords()
    {
        int x, y;
        if ((FoodCellData.Position.x == 1) && (FoodCellData.Position.y == 0))
        {
            return (1, 0);
        }

        if (FoodCellData.Position.x % 2 == 0)
        {
            x = (FoodCellData.Position.x / 2) + FoodCellData.Position.y;
            y = -(FoodCellData.Position.x / 2) + FoodCellData.Position.y;
        }
        else
        {
            x = (FoodCellData.Position.x / 2) + 1 + FoodCellData.Position.y;
            y = -(FoodCellData.Position.x / 2) + FoodCellData.Position.y;
        }

        return (x, y);
    }
}
