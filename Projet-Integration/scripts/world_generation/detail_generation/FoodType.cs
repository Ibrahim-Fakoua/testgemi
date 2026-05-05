 using System;
  using System.Collections.Generic;
  using Godot;
  using Microsoft.CSharp.RuntimeBinder;

  namespace Terrarium_Virtuel.scripts.world_generation;

  public enum BiomeType
  {

      Desert,
      Tundra,
      Grasslands,
      Forest,
      Water
  }

  public enum FoodType
  {
      None,
      // Desert
      SaguaroFruit,
      SandBeetles,
      // Tundra
      ArcticBerries,
      Lemmings,
      // Grasslands
      GrainHeads,
      CloverPatches,
      GroundEggs,
      WildCarrots,
      // Forest
      WildRaspberries,
      RedMushroom,
      WildApple
  }

  public static class FoodTypeExtensions
  {

      public const int FoodTypeCount = 12;

      public static Dictionary<FoodType, float> Probabilities = new()
      {
          { FoodType.None, 18f },
          { FoodType.SaguaroFruit, 8f }, { FoodType.SandBeetles, 12f },
          { FoodType.ArcticBerries, 15f }, { FoodType.Lemmings, 10f },
          { FoodType.GrainHeads, 25f }, { FoodType.CloverPatches, 18f }, { FoodType.GroundEggs, 2f }, { FoodType.WildCarrots, 15f },
          { FoodType.WildRaspberries, 20f }, { FoodType.RedMushroom, 8f }, { FoodType.WildApple, 10f }
      };

      public static Dictionary<BiomeType, List<FoodType>> BiomeToFood = new Dictionary<BiomeType, List<FoodType>>
      {
          {
              BiomeType.Desert,
              new List<FoodType> { FoodType.None, FoodType.SaguaroFruit, FoodType.SandBeetles }
          },
          {
              BiomeType.Tundra,
              new List<FoodType> { FoodType.None, FoodType.ArcticBerries, FoodType.Lemmings }
          },
          {
              BiomeType.Grasslands,
              new List<FoodType> { FoodType.None, FoodType.GrainHeads, FoodType.CloverPatches, FoodType.GroundEggs, FoodType.WildCarrots }
          },
          {
              BiomeType.Forest,
              new List<FoodType> { FoodType.None, FoodType.WildRaspberries, FoodType.RedMushroom, FoodType.WildApple }
          },
          {
              BiomeType.Water,
              new List<FoodType>(){}
          }
      };

      public static Dictionary<FoodType, (float PeakHeight, float Spread)> ElevationProfiles = new()
      {
       
          { FoodType.None, (0.5f, 10f) },

          // Desert foods 
          { FoodType.SandBeetles, (0.3f, 0.05f) },
          { FoodType.SaguaroFruit, (0.32f, 0.08f) },

          // Grassland foods 
          { FoodType.CloverPatches, (0.34f, 0.1f) },
          { FoodType.GrainHeads, (0.35f, 0.15f) },
          { FoodType.WildCarrots, (0.33f, 0.1f) },
          { FoodType.GroundEggs, (0.36f, 0.05f) },

          // Forest foods
          { FoodType.RedMushroom, (0.39f, 0.08f) },
          { FoodType.WildRaspberries, (0.4f, 0.12f) },
          { FoodType.WildApple, (0.42f, 0.1f) },

          // Tundra foods 
          { FoodType.Lemmings, (0.45f, 0.1f) },
          { FoodType.ArcticBerries, (0.48f, 0.12f) }
      };

      public static Dictionary<BiomeType, Dictionary<FoodType, float>> BiomeToFoodProbabilities = new()
      {
          {
              BiomeType.Desert, new Dictionary<FoodType, float>
              {
                  { FoodType.None, Probabilities[FoodType.None] },
                  { FoodType.SaguaroFruit, Probabilities[FoodType.SaguaroFruit] },
                  { FoodType.SandBeetles, Probabilities[FoodType.SandBeetles] }
              }
          },
          {
              BiomeType.Tundra, new Dictionary<FoodType, float>
              {
                  { FoodType.None, Probabilities[FoodType.None] },
                  { FoodType.ArcticBerries, Probabilities[FoodType.ArcticBerries] },
                  { FoodType.Lemmings, Probabilities[FoodType.Lemmings] }
              }
          },
          {
              BiomeType.Grasslands, new Dictionary<FoodType, float>
              {
                  { FoodType.None, Probabilities[FoodType.None] },
                  { FoodType.GrainHeads, Probabilities[FoodType.GrainHeads] },
                  { FoodType.CloverPatches, Probabilities[FoodType.CloverPatches] },
                  { FoodType.GroundEggs, Probabilities[FoodType.GroundEggs] },
                  { FoodType.WildCarrots, Probabilities[FoodType.WildCarrots] }
              }
          },
          {
              BiomeType.Forest, new Dictionary<FoodType, float>
              {
                  { FoodType.None, Probabilities[FoodType.None] },
                  { FoodType.WildRaspberries, Probabilities[FoodType.WildRaspberries] },
                  { FoodType.RedMushroom, Probabilities[FoodType.RedMushroom] },
                  { FoodType.WildApple, Probabilities[FoodType.WildApple] }
              }
          }
      };


      public static (float PeakHeight, float Spread) GetElevationProfile(this FoodType type)
      {
          return ElevationProfiles[type];
      }

 
      public static void ModifyProbabilitiesCuzElevation(FoodCellData cellData, float currentElevation)
      {
          float sum = 0;
          float weight;
          foreach (FoodType foodType in cellData.GetValidFoodTypes())
          {
        
              if (foodType == FoodType.None) continue;

              weight = GetElevationWeight(foodType, currentElevation * 1.2f);
              cellData.MultiplyFoodTypeByWeight(foodType, (float)Math.Pow(weight + 1, 20));
              sum += cellData.GetFoodTypeProbability(foodType);
          }

    
          if (sum > 0)
          {
              foreach (FoodType foodType in cellData.GetValidFoodTypes())
              {
                  if (foodType == FoodType.None) continue;
                  cellData.MultiplyFoodTypeByWeight(foodType, 1 / sum);
              }
          }
      }

      public static float GetElevationWeight(this FoodType type, float currentElevation)
      {
          var profile = ElevationProfiles[type];
          float numerator = (currentElevation - profile.PeakHeight) * (currentElevation - profile.PeakHeight);
          float denominator = 2f * (profile.Spread * profile.Spread);
          return Mathf.Exp(-numerator / denominator);
      }

      public static float GetProb(this FoodType type)
      {
          return Probabilities[type];
      }

      public static void SetProb(this FoodType type, float newProb)
      {
          Probabilities[type] = newProb;
      }

      // Atlas coordinates for rendering food sprites
      public static Vector2I ToAtlasCoords(this FoodType type)
      {
          return type switch
          {
              FoodType.None => new Vector2I(-1, -1),
              FoodType.SaguaroFruit => new Vector2I(4, 1),
              FoodType.SandBeetles => new Vector2I(5, 1),
              FoodType.ArcticBerries => new Vector2I(2, 2),
              FoodType.Lemmings => new Vector2I(4,2),
              FoodType.GrainHeads => new Vector2I(1, 0),
              FoodType.CloverPatches => new Vector2I(2, 0),
              FoodType.GroundEggs => new Vector2I(3, 0),
              FoodType.WildCarrots => new Vector2I(4, 0),
              FoodType.WildRaspberries => new Vector2I(4,1),
              FoodType.RedMushroom => new Vector2I(5, 1),
              FoodType.WildApple => new Vector2I(6, 1),
              _ => throw new RuntimeBinderException(null, null)
          };
      }



      public static int GetSourceId(this FoodType type)
      {
          return type switch
          {
              FoodType.None => -1,
              FoodType.SaguaroFruit or FoodType.SandBeetles => 4,
              FoodType.ArcticBerries or FoodType.Lemmings => 5,
              FoodType.GrainHeads or FoodType.CloverPatches or FoodType.GroundEggs or FoodType.WildCarrots => 6,
              FoodType.WildRaspberries or FoodType.RedMushroom or FoodType.WildApple => 7,
              _ => throw new RuntimeBinderException(null, null)
          };
      }
  }
