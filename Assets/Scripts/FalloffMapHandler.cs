using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


//todo work on this part
public static class FalloffMapHandler
{
	public static float[,] CreateFalloffMap(int count)
	{
		float[,] map = new float[count, count];
		for (int i = 0; i < count; i++)
		{
			for (int j = 0; j < count; j++)
			{
				float x = i / (float)count * 2 - 1;
				float y = j / (float)count * 2 - 1;
				float returnValue = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
				map[i, j] = Calculate(returnValue);
			}
		}
		return map;
	}

	static float Calculate(float value)
	{
		float a = 4;
		float b = 2.15f;
		return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
	}
}
