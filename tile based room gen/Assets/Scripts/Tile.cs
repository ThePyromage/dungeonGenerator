using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
	WALL,
	EMPTY,
	DOOR
}

public class Tile : MonoBehaviour
{
	//the type of tile this is
	private TileType m_tileType;

	/// <summary>
	/// Gets the current tile type of this tile
	/// </summary>
	/// <returns></returns>
	public TileType GetTileType() { return m_tileType; }

	public void SetTileType(TileType tile) { m_tileType = tile; }
}
