using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


class LevelOfDetailMesh
{
	public event Action UpdateCallback;
	public bool HasRequestedMesh;
	public bool HasMesh;

	private Mesh m_mesh;
	private int m_lod;

	public Mesh Mesh => m_mesh;

	public LevelOfDetailMesh(int lod)
	{
		m_lod = lod;
	}

	void OnMeshDataReceived(object meshDataObject)
	{
		m_mesh = ((MeshThreadable)meshDataObject).CreateMesh();
		HasMesh = true;

		UpdateCallback();
	}

	public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
	{
		HasRequestedMesh = true;
		ThreadHandler.RequestData(() => MeshGenerationHandler.CreateTerrainMesh(heightMap.values, meshSettings, m_lod), OnMeshDataReceived);
	}
}

public class Chunk
{
	private const float COLLIDER_GENERATION_DISTANCE_THRESHOLD = 5;
	
	public event Action<Chunk, bool> OnVisibilityChanged;

	public Vector2 Coordinates;

	private GameObject m_meshObject;
	private Vector2 m_sampleCenter;
	private Bounds m_bounds;
	 
	private MeshRenderer m_meshRenderer;
	private MeshFilter m_meshFilter;
	private MeshCollider m_meshCollider;
	
	private LevelOfDetailInformation[] m_detailLevels;
	private LevelOfDetailMesh[] m_LODMeshes;
	private int m_colliderLODIndex;
	
	private HeightMap m_heightMap;
	private bool m_heightMapReceived;
	private int m_previousLODIndex = -1;
	private bool m_hasSetCollider;
	private float m_maxViewDst;
	
	private HeightMapSettings m_heightMapSettings;
	private MeshSettings m_meshSettings;
	private Transform m_viewerTransform;

	private Vector2 ViewerPosition => new Vector2(m_viewerTransform.position.x, m_viewerTransform.position.z);

	public Chunk(Vector2 chunkRelativePosition, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LevelOfDetailInformation[] lodS, int colliderLODIndex, Transform parent, Transform viewer, Material material)
	{
		Coordinates = chunkRelativePosition;
		m_detailLevels = lodS;
		m_colliderLODIndex = colliderLODIndex;
		m_heightMapSettings = heightMapSettings;
		m_meshSettings = meshSettings;
		m_viewerTransform = viewer;
		m_sampleCenter = chunkRelativePosition * meshSettings.WorldScale / meshSettings.MeshScale;
		Vector2 position = chunkRelativePosition * meshSettings.WorldScale;
		m_bounds = new Bounds(position, Vector2.one * meshSettings.WorldScale);
		m_meshObject = new GameObject("Terrain Chunk");
		m_meshRenderer = m_meshObject.AddComponent<MeshRenderer>();
		m_meshFilter = m_meshObject.AddComponent<MeshFilter>();
		m_meshCollider = m_meshObject.AddComponent<MeshCollider>();
		m_meshRenderer.material = material;
		m_meshObject.transform.position = new Vector3(position.x, 0, position.y);
		m_meshObject.transform.parent = parent;
		SetVisible(false);
		m_LODMeshes = new LevelOfDetailMesh[lodS.Length];
		for (int i = 0; i < lodS.Length; i++)
		{
			m_LODMeshes[i] = new LevelOfDetailMesh(lodS[i].LOD);
			m_LODMeshes[i].UpdateCallback += UpdateTerrainChunk;

			if (i == colliderLODIndex)
				m_LODMeshes[i].UpdateCallback += UpdateColliderMesh;
		}
		m_maxViewDst = lodS[lodS.Length - 1].VisibleDistanceThreshold;
	}
	void OnHeightMapReceived(object heightMapObject)
	{
		m_heightMap = (HeightMap)heightMapObject;
		m_heightMapReceived = true;
		UpdateTerrainChunk();
	}
	public void Load()
	{
		ThreadHandler.RequestData(() => HeightMapGenerator.GenerateHeightMap(m_meshSettings.NumberOfVertices, m_meshSettings.NumberOfVertices, m_heightMapSettings, m_sampleCenter), OnHeightMapReceived);
	}
	public void UpdateColliderMesh()
	{
		if (!m_hasSetCollider)
		{
			float sqrDstFromViewerToEdge = m_bounds.SqrDistance(ViewerPosition);
			if (sqrDstFromViewerToEdge < m_detailLevels[m_colliderLODIndex].SqrVisibleDstThreshold)
			{
				if (!m_LODMeshes[m_colliderLODIndex].HasRequestedMesh)
					m_LODMeshes[m_colliderLODIndex].RequestMesh(m_heightMap, m_meshSettings);
			}
			if (sqrDstFromViewerToEdge < COLLIDER_GENERATION_DISTANCE_THRESHOLD * COLLIDER_GENERATION_DISTANCE_THRESHOLD)
			{
				if (m_LODMeshes[m_colliderLODIndex].HasMesh)
				{
					m_meshCollider.sharedMesh = m_LODMeshes[m_colliderLODIndex].Mesh;
					m_hasSetCollider = true;
				}
			}
		}
	}

	public void UpdateTerrainChunk()
	{
		if (m_heightMapReceived)
		{
			float viewerDstFromNearestEdge = Mathf.Sqrt(m_bounds.SqrDistance(ViewerPosition));
			bool wasVisible = m_meshObject.activeSelf;
			bool visible = viewerDstFromNearestEdge <= m_maxViewDst;
			if (visible)
			{
				int lodIndex = 0;
				for (int i = 0; i < m_detailLevels.Length - 1; i++)
				{
					if (viewerDstFromNearestEdge > m_detailLevels[i].VisibleDistanceThreshold)
						lodIndex = i + 1;
					else
						break;
				}
				if (lodIndex != m_previousLODIndex)
				{
					LevelOfDetailMesh lodMesh = m_LODMeshes[lodIndex];
					if (lodMesh.HasMesh)
					{
						m_previousLODIndex = lodIndex;
						m_meshFilter.mesh = lodMesh.Mesh;
					}
					else if (!lodMesh.HasRequestedMesh)
						lodMesh.RequestMesh(m_heightMap, m_meshSettings);
				}
			}
			if (wasVisible != visible)
			{
				SetVisible(visible);
				OnVisibilityChanged?.Invoke(this, visible);
			}
		}
	}

	public void SetVisible(bool visible)
	{
		m_meshObject.SetActive(visible);
	}
}
