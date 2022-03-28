using UnityEngine;


public static class HeightMapGenerator
{
	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 center)
	{
		float[,] values = NoiseHandler.ProcessNoiseMap(width, height, settings.NoiseSettings, center);
		AnimationCurve animationCurve = new AnimationCurve(settings.HeightCurve.keys);
		float minV = float.MaxValue;
		float maxV = float.MinValue;
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				values[i, j] *= animationCurve.Evaluate(values[i, j]) * settings.HeightMultiplier;
				if (values[i, j] > maxV)
					maxV = values[i, j];
				if (values[i, j] < minV)
					minV = values[i, j];
			}
		}
		return new HeightMap(values, minV, maxV);
	}
}

public struct HeightMap
{
	public readonly float[,] values;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap(float[,] values, float minValue, float maxValue)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}

