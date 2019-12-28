using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerator : MonoBehaviour
{
    public static int width, height;
    static Texture2D heightmap;

    public static Texture2D GetTexture()
    {
        return heightmap;
    }

    public static void Generate(int seed)
    {
        heightmap = new Texture2D(width, height);
        float h;
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++){
                h = Mathf.PerlinNoise((x + seed) * 0.03f, (y + seed) * 0.03f);
                heightmap.SetPixel(x, y, new Color(0, h, 0, 1));
            }        

        MakeCrack(45, width / 2, height / 2, 20);

        heightmap.filterMode = FilterMode.Point;

        heightmap.Apply();
    }

    static void MakeCrack(int angle, int pointX, int pointY,int length)
    {
        AddCrack(angle, pointX, pointY, length, 8);
        AddCrack(angle + Random.Range(140, 220), pointX, pointY, length, 8);
    }

    static void Swap(ref int l, ref int m)
    {
        int t = l;
        l = m;
        m = t;
    }

    static void AddCrack(int angle, int pointX, int pointY, int length, int thickens)
    {
        if(thickens < 0) thickens = 0;
        if(length < 1|| pointX < 0 || pointY < 0 || pointX >= width || pointY >= height)
            return;

        float radians = Mathf.PI * angle / 180f;
        int radius = thickens / 2;
        Color c;

        int x, y;
        x = pointX;
        y = pointY;
        
        for(int l = 0; l < length; l++){
            x = Mathf.RoundToInt(pointX + l * Mathf.Cos(radians));
            y = Mathf.RoundToInt(pointY + l * Mathf.Sin(radians));
            if(x < 0 || y < 0 || x >= width || y >= height) break;

            // Draws square - thicknes
            for(int cx = x - radius; cx <= x + radius; cx++){
                if(cx < 0 || cx >= width) continue;
                for(int cy = y - radius; cy <= y + radius; cy++)
                {
                    if(cy < 0 || cy >= height) break;
                    c = heightmap.GetPixel(cx,cy);
                    if(c.r != 0) continue; 
                    heightmap.SetPixel(cx, cy, new Color(c.g, 0, 0, 1));
               }
            }
        }

        int crackCount = Random.Range(1, 5);
        int newLength = (int)(length / 1.25f);
        int newThickness = thickens - 1;
        if(crackCount == 1 || crackCount == 2)
            AddCrack( angle + /*55,*/Random.Range(15, 56),   x, y,  newLength,  newThickness );
        if(crackCount >= 2)
            AddCrack( angle + /*-55,*/Random.Range(-55, -15),  x, y,  newLength,  newThickness );
    }
}
