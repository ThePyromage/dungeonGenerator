using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage : MonoBehaviour
{
	[Header("Stage Variables")]
	[Tooltip("The size of the stage")]
	[SerializeField] private Vector2Int m_stageSize;
	[Tooltip("The size of the tiles")]
	[SerializeField] private Vector2 m_tileSize;

	[Header("Tile Prefabs")]
	[Tooltip("The wall prefab ")]
	[SerializeField] private GameObject m_wallPrefab;
	[Tooltip("The empty prefab")]
	[SerializeField] private GameObject m_emptyPrefab;
	[Tooltip("The door prefab ")]
	[SerializeField] private GameObject m_doorPrefab;

	//The array of tiles
	public Tile[,] m_tiles;

	/// <summary>
	/// Returns the stage size
	/// </summary>
	/// <returns></returns>
	public Vector2Int GetStageSize() { return m_stageSize; }

	/// <summary>
	/// Sets all tiles to be walls
	/// </summary>
	public void Initialise()
	{
		m_tiles = new Tile[m_stageSize.x, m_stageSize.y];
		for (int x = 0; x < m_stageSize.x; x++)
		{
			for (int y = 0; y < m_stageSize.y; y++)
			{
				m_tiles[x, y] = new Tile();
				m_tiles[x, y].Create(m_wallPrefab, m_emptyPrefab, m_doorPrefab, transform);
				m_tiles[x, y].SetPosition(new Vector3(x * m_tileSize.x, 0.0f, y * m_tileSize.y) + transform.position);
				m_tiles[x, y].SetTileType(TileType.WALL);
			}
		}
	}

	/// <summary>
	/// Despawns all the tiles on this stage
	/// </summary>
	public void DespawnStage()
	{
		for (int x = 0; x < m_stageSize.x; x++)
		{
			for (int y = 0; y < m_stageSize.y; y++)
			{
				m_tiles[x, y].DespawnTile();
			}
		}
	}
}
