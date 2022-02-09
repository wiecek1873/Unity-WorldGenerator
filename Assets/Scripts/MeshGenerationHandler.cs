using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


public static class MeshGenerationHandler
{

	public struct Coordinates
	{
		public readonly int x;
		public readonly int y;
		public Coordinates(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

	}

	public static MeshThreadable CreateTerrainMesh(float[,] heightArray, MeshSettings meshSettings, int lod)
	{
		int skip = (lod == 0) ? 1 : lod * 2;
		int vertices = meshSettings.NumberOfVertices;
		Vector2 corner = new Vector2(-1, 1) * meshSettings.WorldScale / 2f;
		MeshThreadable meshThreadable = new MeshThreadable(vertices, skip, meshSettings.UseFlatShading);
		int[,] vertex = new int[vertices, vertices];
		int meshVertexInd = 0;
		int outOfMeshVerex = -1;
		for (int y = 0; y < vertices; y++)
		{
			for (int x = 0; x < vertices; x++)
			{
				bool isOutOfMeshVertex = y == 0 || y == vertices - 1 || x == 0 || x == vertices - 1;
				bool isSkippedVertex = x > 2 && x < vertices - 3 && y > 2 && y < vertices - 3 && ((x - 2) % skip != 0 || (y - 2) % skip != 0);
				if (isOutOfMeshVertex)
				{
					vertex[x, y] = outOfMeshVerex;
					outOfMeshVerex--;
				}
				else if (!isSkippedVertex)
				{
					vertex[x, y] = meshVertexInd;
					meshVertexInd++;
				}
			}
		}
		for (int y = 0; y < vertices; y++)
		{
			for (int x = 0; x < vertices; x++)
			{
				bool isSkippedVertex = x > 2 && x < vertices - 3 && y > 2 && y < vertices - 3 && ((x - 2) % skip != 0 || (y - 2) % skip != 0);

				if (!isSkippedVertex)
				{
					bool outOfMesh = y == 0 || y == vertices - 1 || x == 0 || x == vertices - 1;
					bool meshEdge = (y == 1 || y == vertices - 2 || x == 1 || x == vertices - 2) && !outOfMesh;
					bool mainEdge = (x - 2) % skip == 0 && (y - 2) % skip == 0 && !outOfMesh && !meshEdge;
					bool edgeConection = (y == 2 || y == vertices - 3 || x == 2 || x == vertices - 3) && !outOfMesh && !meshEdge && !mainEdge;

					int vertexIndex = vertex[x, y];
					Vector2 difference = new Vector2(x - 1, y - 1) / (vertices - 3);
					Vector2 position = corner + new Vector2(difference.x, -difference.y) * meshSettings.WorldScale;
					float height = heightArray[x, y];

					if (edgeConection)
					{
						bool isVertical = x == 2 || x == vertices - 3;
						int dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skip;
						int dstToMainVertexB = skip - dstToMainVertexA;
						float dstPercentFromAToB = dstToMainVertexA / (float)skip;
						Coordinates coordA = new Coordinates((isVertical) ? x : x - dstToMainVertexA, (isVertical) ? y - dstToMainVertexA : y);
						Coordinates coordB = new Coordinates((isVertical) ? x : x + dstToMainVertexB, (isVertical) ? y + dstToMainVertexB : y);
						float heightMainVertexA = heightArray[coordA.x, coordA.y];
						float heightMainVertexB = heightArray[coordB.x, coordB.y];
						height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
						ConnectionData edgeConnectionVertexData = new ConnectionData(vertexIndex, vertex[coordA.x, coordA.y], vertex[coordB.x, coordB.y], dstPercentFromAToB);
						meshThreadable.SetConnection(edgeConnectionVertexData);
					}
					meshThreadable.SetVertex(new Vector3(position.x, height, position.y), difference, vertexIndex);
					bool checkTriangle = x < vertices - 1 && y < vertices - 1 && (!edgeConection || (x != 2 && y != 2));
					if (checkTriangle)
					{
						int currentIncrement = (mainEdge && x != vertices - 3 && y != vertices - 3) ? skip : 1;
						int a = vertex[x, y];
						int b = vertex[x + currentIncrement, y];
						int c = vertex[x, y + currentIncrement];
						int d = vertex[x + currentIncrement, y + currentIncrement];
						meshThreadable.SetTriangle(a, d, c);
						meshThreadable.SetTriangle(d, a, b);
					}
				}
			}
		}
		meshThreadable.RecalculateMesh();
		return meshThreadable;
	}
}
public class ConnectionData
{
	public int vertexIndex;
	public int mainVertexAIndex;
	public int mainVertexBIndex;
	public float distance;
	public ConnectionData(int vertexIndex, int mainVertexAIndex, int mainVertexBIndex, float distance)
	{
		this.vertexIndex = vertexIndex;
		this.mainVertexAIndex = mainVertexAIndex;
		this.mainVertexBIndex = mainVertexBIndex;
		this.distance = distance;
	}
}
public class MeshThreadable
{
	private Vector3[] m_vertices;
	private int[] m_triangles;
	private Vector2[] m_uvs;
	private Vector3[] m_bakedNormals;
	private Vector3[] m_outOfMeshVertices;
	private int[] m_outOfMeshTriangles;
	private int m_triangleIndex;
	private int m_outOfMeshTriangleIndex;
	private ConnectionData[] m_edgeConnectionVertices;
	private int m_edgeConnectionVertexIndex;
	private bool m_useFlatShading;
	public MeshThreadable(int vertices, int increments, bool flatShading)
	{
		m_useFlatShading = flatShading;

		int edgeVertices = (vertices - 2) * 4 - 4;
		int connectionVertices = (increments - 1) * (vertices - 5) / increments * 4;
		int mainVertices = (vertices - 5) / increments + 1;
		int sqrMainVertices = mainVertices * mainVertices;

		m_vertices = new Vector3[edgeVertices + connectionVertices + sqrMainVertices];
		m_uvs = new Vector2[m_vertices.Length];
		m_edgeConnectionVertices = new ConnectionData[connectionVertices];

		int edgeTriangles = 8 * (vertices - 4);
		int mainTriangles = (mainVertices - 1) * (mainVertices - 1) * 2;
		m_triangles = new int[(edgeTriangles + mainTriangles) * 3];

		m_outOfMeshVertices = new Vector3[vertices * 4 - 4];
		m_outOfMeshTriangles = new int[24 * (vertices - 2)];
	}
	public Mesh CreateMesh()
	{
		Mesh mesh = new Mesh
		{
			vertices = m_vertices,
			triangles = m_triangles,
			uv = m_uvs
		};
		if (m_useFlatShading)
			mesh.RecalculateNormals();
		else
			mesh.normals = m_bakedNormals;
		return mesh;
	}
	public void SetVertex(Vector3 position, Vector2 uv, int index)
	{
		if (index < 0)
			m_outOfMeshVertices[-index - 1] = position;
		else
		{
			m_vertices[index] = position;
			m_uvs[index] = uv;
		}
	}
	public void SetConnection(ConnectionData edgeConnectionVertexData)
	{
		m_edgeConnectionVertices[m_edgeConnectionVertexIndex] = edgeConnectionVertexData;
		m_edgeConnectionVertexIndex++;
	}
	public void SetTriangle(int a, int b, int c)
	{
		if (a < 0 || b < 0 || c < 0)
		{
			m_outOfMeshTriangles[m_outOfMeshTriangleIndex] = a;
			m_outOfMeshTriangles[m_outOfMeshTriangleIndex + 1] = b;
			m_outOfMeshTriangles[m_outOfMeshTriangleIndex + 2] = c;
			m_outOfMeshTriangleIndex += 3;
		}
		else
		{
			m_triangles[m_triangleIndex] = a;
			m_triangles[m_triangleIndex + 1] = b;
			m_triangles[m_triangleIndex + 2] = c;
			m_triangleIndex += 3;
		}
	}
	private void FlatShading()
	{
		Vector3[] flatShadedVertices = new Vector3[m_triangles.Length];
		Vector2[] flatShadedUvs = new Vector2[m_triangles.Length];
		for (int i = 0; i < m_triangles.Length; i++)
		{
			flatShadedVertices[i] = m_vertices[m_triangles[i]];
			flatShadedUvs[i] = m_uvs[m_triangles[i]];
			m_triangles[i] = i;
		}
		m_vertices = flatShadedVertices;
		m_uvs = flatShadedUvs;
	}
	public void RecalculateMesh()
	{
		if (m_useFlatShading)
			FlatShading();
		else
		{
			BakeNormals();
			CheckEdges();
		}
	}
	private void BakeNormals()
	{
		m_bakedNormals = CalculateNormals();
	}
	private Vector3[] CalculateNormals()
	{
		Vector3[] vertexNormals = new Vector3[m_vertices.Length];
		int triangleCount = m_triangles.Length / 3;
		for (int i = 0; i < triangleCount; i++)
		{
			int normalTriangleIndex = i * 3;
			int vertexIndexA = m_triangles[normalTriangleIndex];
			int vertexIndexB = m_triangles[normalTriangleIndex + 1];
			int vertexIndexC = m_triangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = CreateNormals(vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		int borderTriangleCount = m_outOfMeshTriangles.Length / 3;
		for (int i = 0; i < borderTriangleCount; i++)
		{
			int normalTriangleIndex = i * 3;
			int vertexIndexA = m_outOfMeshTriangles[normalTriangleIndex];
			int vertexIndexB = m_outOfMeshTriangles[normalTriangleIndex + 1];
			int vertexIndexC = m_outOfMeshTriangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = CreateNormals(vertexIndexA, vertexIndexB, vertexIndexC);
			
			if (vertexIndexA >= 0)
				vertexNormals[vertexIndexA] += triangleNormal;

			if (vertexIndexB >= 0)
				vertexNormals[vertexIndexB] += triangleNormal;

			if (vertexIndexC >= 0)
				vertexNormals[vertexIndexC] += triangleNormal;
		}

		for (int i = 0; i < vertexNormals.Length; i++)
			vertexNormals[i].Normalize();
		return vertexNormals;
	}

	private void CheckEdges()
	{
		foreach (ConnectionData e in m_edgeConnectionVertices)
			m_bakedNormals[e.vertexIndex] = m_bakedNormals[e.mainVertexAIndex] * (1 - e.distance) + m_bakedNormals[e.mainVertexBIndex] * e.distance;
	}

	private Vector3 CreateNormals(int a, int b, int c)
	{
		Vector3 pointA = (a < 0) ? m_outOfMeshVertices[-a - 1] : m_vertices[a];
		Vector3 pointB = (b < 0) ? m_outOfMeshVertices[-b - 1] : m_vertices[b];
		Vector3 pointC = (c < 0) ? m_outOfMeshVertices[-c - 1] : m_vertices[c];
		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross(sideAB, sideAC).normalized;
	}
}