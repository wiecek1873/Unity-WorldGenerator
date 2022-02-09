using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Linq;

[CreateAssetMenu()]
public class TextureSettings : UpdatableData
{
	[Serializable]
	public class ChunkLayer
	{
		public Texture2D Texture;
		public Color Tint;
		[Range(0, 1)] public float TintStrength;
		[Range(0, 1)] public float StartHeight;
		[Range(0, 1)] public float BlendStrength;
		public float TextureScale;
	}

	const int textureSize = 512;
	const TextureFormat textureFormat = TextureFormat.RGB565;

	[SerializeField] private ChunkLayer[] m_layers;

	private float m_savedMinHeight;
	private float m_savedMaxHeight;


	public void UpdateHeights(Material material, float minHeight, float maxHeight)
	{
		m_savedMinHeight = minHeight;
		m_savedMaxHeight = maxHeight;
		material.SetFloat("minHeight", minHeight);
		material.SetFloat("maxHeight", maxHeight);
	}
	public void ApplyToMaterial(Material material)
	{
		material.SetInt("layerCount", m_layers.Length);
		material.SetColorArray("baseColors", m_layers.Select(x => x.Tint).ToArray());
		material.SetFloatArray("baseStartHeights", m_layers.Select(x => x.StartHeight).ToArray());
		material.SetFloatArray("baseBlends", m_layers.Select(x => x.BlendStrength).ToArray());
		material.SetFloatArray("baseColorStrength", m_layers.Select(x => x.TintStrength).ToArray());
		material.SetFloatArray("baseTextureScales", m_layers.Select(x => x.TextureScale).ToArray());
		Texture2DArray texturesArray = GenerateTextureArray(m_layers.Select(x => x.Texture).ToArray());
		material.SetTexture("baseTextures", texturesArray);
		UpdateHeights(material, m_savedMinHeight, m_savedMaxHeight);
	}

	Texture2DArray GenerateTextureArray(Texture2D[] textures)
	{
		Texture2DArray texturesArr = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
		for (int i = 0; i < textures.Length; i++)
			texturesArr.SetPixels(textures[i].GetPixels(), i);
		texturesArr.Apply();
		return texturesArr;
	}
}
