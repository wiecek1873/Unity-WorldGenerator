using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


public class Preview : MonoBehaviour
{
	public enum PreviewMode { NoiseMap, Mesh, FalloffMap };
	[Header("Preview")]
	public bool AutoUpdate;
	[SerializeField] private PreviewMode m_previewMode;
	[Range(0, MeshSettings.NUMBER_OF_SUPPORTED_LODS - 1)] [SerializeField] private int m_editorPreviewLOD;
	[Header("Components")]
	[SerializeField] private Renderer m_textureRenderer;
	[SerializeField] private MeshFilter m_meshFilter;
	[SerializeField] private MeshRenderer m_meshRenderer;
	[SerializeField] private Material m_terrainMaterial;
	[Header("Settings")]
	[SerializeField] private MeshSettings m_meshSettings;
	[SerializeField] private HeightMapSettings m_heightMapSettings;
	[SerializeField] private TextureSettings m_textureData;
	public void DrawPreviewEditor()
	{
		m_textureData.ApplyToMaterial(m_terrainMaterial);
		m_textureData.UpdateHeights(m_terrainMaterial, m_heightMapSettings.MinHeight, m_heightMapSettings.MaxHeight);
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(m_meshSettings.NumberOfVertices, m_meshSettings.NumberOfVertices, m_heightMapSettings, Vector2.zero);

		switch (m_previewMode)
		{
			case PreviewMode.NoiseMap:
				CreateTexture(TextureGenerator.CreateTextureMapFromHeightMap(heightMap)); break;
			case PreviewMode.Mesh:
				CreateMesh(MeshGenerationHandler.CreateTerrainMesh(heightMap.values, m_meshSettings, m_editorPreviewLOD)); break;
			case PreviewMode.FalloffMap:
				CreateTexture(TextureGenerator.CreateTextureMapFromHeightMap(new HeightMap(FalloffMapHandler.CreateFalloffMap(m_meshSettings.NumberOfVertices), 0, 1))); break;
		}
	}
	void OnValuesUpdated()
	{
		if (!Application.isPlaying)
			DrawPreviewEditor();
	}
	void OnTextureUpdates()
	{
		m_textureData.ApplyToMaterial(m_terrainMaterial);
	}
	public void CreateTexture(Texture2D texture)
	{
		m_textureRenderer.sharedMaterial.mainTexture = texture;
		m_textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
		m_textureRenderer.gameObject.SetActive(true);
		m_meshFilter.gameObject.SetActive(false);
	}
	public void CreateMesh(MeshThreadable meshData)
	{
		m_meshFilter.sharedMesh = meshData.CreateMesh();
		m_textureRenderer.gameObject.SetActive(false);
		m_meshFilter.gameObject.SetActive(true);
	}
	void OnValidate()
	{
		if (m_meshSettings != null)
		{
			m_meshSettings.OnDataValuesUpdated -= OnValuesUpdated;
			m_meshSettings.OnDataValuesUpdated += OnValuesUpdated;
		}
		if (m_heightMapSettings != null)
		{
			m_heightMapSettings.OnDataValuesUpdated -= OnValuesUpdated;
			m_heightMapSettings.OnDataValuesUpdated += OnValuesUpdated;
		}
		if (m_textureData != null)
		{
			m_textureData.OnDataValuesUpdated -= OnTextureUpdates;
			m_textureData.OnDataValuesUpdated += OnTextureUpdates;
		}
	}
}
