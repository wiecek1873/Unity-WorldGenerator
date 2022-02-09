using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
[Serializable]
public struct LevelOfDetailInformation
{
	[Range(0, MeshSettings.NUMBER_OF_SUPPORTED_LODS - 1)]
	public int LOD;
	public float VisibleDistanceThreshold;
	public float SqrVisibleDstThreshold => VisibleDistanceThreshold * VisibleDistanceThreshold;
}
public class TerrainCreator : MonoBehaviour
{
	private const float MOVE_THRESHOLD = 25f;
	private const float SQR_MOVE_THRESHOLD = MOVE_THRESHOLD * MOVE_THRESHOLD;
	[Header("Generator")]
	[SerializeField] private int m_colliderLODIndex;
	[SerializeField] private LevelOfDetailInformation[] m_detailLevels;
	[Header("Components")]
	[SerializeField] private Transform m_viewer;
	[SerializeField] private Material m_mapMaterial;
	[Header("Settings")]
	[SerializeField] private MeshSettings m_meshSettings;
	[SerializeField] private HeightMapSettings m_heightMapSettings;
	[SerializeField] private TextureSettings m_textureSettings;
	private Dictionary<Vector2, Chunk> m_chunkDictionary = new Dictionary<Vector2, Chunk>();
	private List<Chunk> m_visibleTerrainChunks = new List<Chunk>();
	private Vector2 m_viewerPosition;
	private Vector2 m_viewerPositionOld;
	private float m_meshWorldSize;
	private int m_chunkVisibleFromDistance;

	void UpdateVisibleChunks()
	{
		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
		for (int i = m_visibleTerrainChunks.Count - 1; i >= 0; i--)
		{
			alreadyUpdatedChunkCoords.Add(m_visibleTerrainChunks[i].Coordinates);
			m_visibleTerrainChunks[i].UpdateTerrainChunk();
		}
		int chunkX = Mathf.RoundToInt(m_viewerPosition.x / m_meshWorldSize);
		int chunkY = Mathf.RoundToInt(m_viewerPosition.y / m_meshWorldSize);
		for (int yOffset = -m_chunkVisibleFromDistance; yOffset <= m_chunkVisibleFromDistance; yOffset++)
		{
			for (int xOffset = -m_chunkVisibleFromDistance; xOffset <= m_chunkVisibleFromDistance; xOffset++)
			{
				Vector2 viewedChunkCoord = new Vector2(chunkX + xOffset, chunkY + yOffset);
				if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
				{
					if (m_chunkDictionary.ContainsKey(viewedChunkCoord))
						m_chunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
					else
					{
						Chunk newChunk = new Chunk(viewedChunkCoord, m_heightMapSettings, m_meshSettings, m_detailLevels, m_colliderLODIndex, transform, m_viewer, m_mapMaterial);
						m_chunkDictionary.Add(viewedChunkCoord, newChunk);
						newChunk.OnVisibilityChanged += OnChunkChange;
						newChunk.Load();
					}
				}

			}
		}
	}

	void OnChunkChange(Chunk chunk, bool isVisible)
	{
		if (isVisible)
			m_visibleTerrainChunks.Add(chunk);
		else
			m_visibleTerrainChunks.Remove(chunk);
	}

	void Start()
	{
		m_textureSettings.ApplyToMaterial(m_mapMaterial);
		m_textureSettings.UpdateHeights(m_mapMaterial, m_heightMapSettings.MinHeight, m_heightMapSettings.MaxHeight);
		float maxViewDst = m_detailLevels[m_detailLevels.Length - 1].VisibleDistanceThreshold;
		m_meshWorldSize = m_meshSettings.WorldScale;
		m_chunkVisibleFromDistance = Mathf.RoundToInt(maxViewDst / m_meshWorldSize);
		UpdateVisibleChunks();
	}

	void Update()
	{
		m_viewerPosition = new Vector2(m_viewer.position.x, m_viewer.position.z);
		if (m_viewerPosition != m_viewerPositionOld)
		{
			foreach (Chunk chunk in m_visibleTerrainChunks)
				chunk.UpdateColliderMesh();
		}
		if ((m_viewerPositionOld - m_viewerPosition).sqrMagnitude > SQR_MOVE_THRESHOLD)
		{
			m_viewerPositionOld = m_viewerPosition;
			UpdateVisibleChunks();
		}
	}
}



