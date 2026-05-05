namespace Terrarium_Virtuel.scripts.world_generation;
using Godot;
using System;
public class HeightNoiseMap
{
    public FastNoiseLite Noise;
    public float Frequency;
    public (int x, int y) GridSize;
    public float[,] HeightMap;
    public HeightNoiseMap((int x ,int y ) gridSize, RandomNumberGenerator rng)
    {
        GridSize = gridSize;
        Noise = new FastNoiseLite();
        Noise.Seed = (int) rng.RandiRange(0, int.MaxValue);
        Frequency = 0.01f;
        Noise.Frequency = Frequency; 
        Noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;

        Noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;


        Noise.FractalOctaves = 5; 


        Noise.FractalLacunarity = 2.0f; 
        Noise.FractalGain = 0.5f;       
        
    }

    public void ChangeNoiseType()
    {
        
        Noise.NoiseType = (FastNoiseLite.NoiseTypeEnum)(((int)Noise.NoiseType + 1) % 6);
        GD.Print("Noise Type: " + Noise.NoiseType);
        Update();
    }
    public void Update()
    {
        Noise.Frequency = Frequency;
        SendToDisplay(CreateTheList(Noise, (GridSize.x, GridSize.y)));
    }
    public void SendToDisplay(float[,] noiseValues)
    {
        if (GenerationManager.isItDebugMode)
        {
            Variant result = Controller.Instance.WorldManager.Get("heightMapRef");
        
        
            HeightMapTest mapNode = result.As<HeightMapTest>();
            mapNode.DisplayMap(noiseValues);
        }

    }
    public float[,] CreateTheList(FastNoiseLite noise, (int x, int y) gridSize)
    {

        float[,] noiseValues = new float[gridSize.y, gridSize.x ];


        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
      
                float value = noise.GetNoise2D(x, y);
                
                noiseValues[y, x] = (value + 1.0f) / 2.0f;
            }
        }
        HeightMap = noiseValues;
        return noiseValues;
    }

    public void Smooth()
    {
        HeightMap = SmoothHeightMap(HeightMap, 1, 1);
        SendToDisplay(HeightMap);
    }
    private float[,] SmoothHeightMap(float[,] map, int radius = 1, int iterations = 1)
    {
        int height = map.GetLength(0); 
        int width = map.GetLength(1);  
    

        float[,] resultMap = (float[,])map.Clone();
        float[,] tempMap = new float[height, width];

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    int count = 0;

          
                    for (int offsetY = -radius; offsetY <= radius; offsetY++)
                    {
                        for (int offsetX = -radius; offsetX <= radius; offsetX++)
                        {
                            int sampleX = x + offsetX;
                            int sampleY = y + offsetY;

                            if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                            {
                                sum += resultMap[sampleY, sampleX];
                                count++;
                            }
                        }
                    }


                    tempMap[y, x] = sum / count;
                }
            }

            Array.Copy(tempMap, resultMap, tempMap.Length);
        }

        return resultMap;
    }
}