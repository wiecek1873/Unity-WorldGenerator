using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public static class NoiseHandler
{
	public enum NormalizeMode { Local, Global };

	public static float[,] ProcessNoiseMap(int width, int height, NoiseSettings settings, Vector2 center)
	{
		float[,] map = new float[width, height];
		System.Random randomGenerator = new System.Random(settings.RNGSeed);
		Vector2[] offsets = new Vector2[settings.Octaves];
		float maxAcquiredHeight = 0;
		float a = 1;
		float f = 1;
		for (int i = 0; i < settings.Octaves; i++)
		{
			float offsetX = randomGenerator.Next(-212512, 212512) + settings.NoiseOffset.x + center.x;
			float offsetY = randomGenerator.Next(-212512, 212512) - settings.NoiseOffset.y - center.y;
			offsets[i] = new Vector2(offsetX, offsetY);

			maxAcquiredHeight += a;
			a *= settings.Persistance;
		}
		float maxHeight = float.MinValue;
		float minHeight = float.MaxValue;
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				a = 1;
				f = 1;
				float noiseValue = 0;
				for (int i = 0; i < settings.Octaves; i++)
				{
					float sampleX = (x - width / 2f + offsets[i].x) / settings.Scale * f;
					float sampleY = (y - height / 2f + offsets[i].y) / settings.Scale * f;

					float algorithmValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					noiseValue += algorithmValue * a;

					a *= settings.Persistance;
					f *= settings.Lacunarity;
				}
				if (noiseValue > maxHeight)
					maxHeight = noiseValue;
				if (noiseValue < minHeight)
					minHeight = noiseValue;
				map[x, y] = noiseValue;
				if (settings.NormalizationMode == NormalizeMode.Global)
				{
					float settedHeight = (map[x, y] + 1) / (maxAcquiredHeight / 0.9f);
					map[x, y] = Mathf.Clamp(settedHeight, 0, int.MaxValue);
				}
			}
		}

		if (settings.NormalizationMode == NormalizeMode.Local)
		{
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
					map[x, y] = Mathf.InverseLerp(minHeight, maxHeight, map[x, y]);
			}
		}
		return map;
	}
}

[Serializable]
public class NoiseSettings
{
	public NoiseHandler.NormalizeMode NormalizationMode;
	public float Scale = 10;
	public int Octaves = 3;
	[Range(0, 1)] public float Persistance = 0.2f;
	public float Lacunarity = 1;
	public int RNGSeed;
	public Vector2 NoiseOffset;
	public void ValidateValues()
	{
		Scale = Mathf.Max(Scale, 0.01f);
		Octaves = Mathf.Max(Octaves, 1);
		Lacunarity = Mathf.Max(Lacunarity, 1);
		Persistance = Mathf.Clamp01(Persistance);
	}
}