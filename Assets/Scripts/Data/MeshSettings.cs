using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
	public const int NUMBER_OF_SUPPORTED_LODS = 5;
	public const int NUMBER_OF_SUPPORTED_SIZES = 9;
	public const int NUMBER_OF_SUPPORTED_FLATSHADED_SIZES = 3;
	public static readonly int[] SUPPORTED_CHUNK_SIZES = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

	public float MeshScale = 2.5f;
	public bool UseFlatShading;

	[Range(0, NUMBER_OF_SUPPORTED_SIZES - 1)]
	[SerializeField] private int m_chunkSizeIndex;
	[Range(0, NUMBER_OF_SUPPORTED_FLATSHADED_SIZES - 1)]
	[SerializeField] private int m_flatshadedChunkSizeIndex;

	public int NumberOfVertices => SUPPORTED_CHUNK_SIZES[(UseFlatShading) ? m_flatshadedChunkSizeIndex : m_chunkSizeIndex] + 5;

	public float WorldScale => (NumberOfVertices - 3) * MeshScale;
}
