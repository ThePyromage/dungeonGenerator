using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage : MonoBehaviour
{
	[Header("Stage Variables")]
	[Tooltip("The size of the stage")]
	[SerializeField] private Vector2Int m_stageSize;

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
				m_tiles[x, y].SetTileType(TileType.WALL);
			}
		}
	}
}
