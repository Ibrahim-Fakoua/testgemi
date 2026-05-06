using Godot;
using System;

public partial class HeightMapTest : Sprite2D
{

    public void DisplayMap(float[,] data)
    {
        int width = data.GetLength(0);
        int height = data.GetLength(1);

        Image image = Image.CreateEmpty(width, height, false, Image.Format.L8);


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
        
                float rawValue = data[x, y];
                // float normalized = (rawValue + 1.0f) / 2.0f;
                // normalized = Mathf.Clamp(normalized, 0.0f, 1.0f);

                Color pixelColor = new Color(rawValue, rawValue, rawValue);
                image.SetPixel(x, y, pixelColor);
            }
        }


        ImageTexture texture = ImageTexture.CreateFromImage(image);
        this.Texture = texture;


        this.TextureFilter = TextureFilterEnum.Nearest;
    }
}