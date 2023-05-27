using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{

    public enum NormalizeMode { Local, Global};

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // pull each octave from a seeded random location in the 
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        // grows per octave by the persistence value
        float amplitude = 1;
        // grows per octave by the lacunarity value
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }

        // avoid those pesky divide by zero errors
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        // adjusting our x and y by this will cause scale to "zoom" around center, not to upper right
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // grows per octave by the persistence value
                amplitude = 1;
                // grows per octave by the lacunarity value
                frequency = 1;

                // the value for the noise map that combines all the octaves for this x,y
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    // change from 0..1 to -1 .. 1
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // track the range for later normalization
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        // normalize to 0..1
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    // good if we are doing the entire map at once,
                    // but if there are multiple chunks, the min/max for each will differ, and there will be seams
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                } else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 2f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }

            }
        }

        return noiseMap;
    }
}
