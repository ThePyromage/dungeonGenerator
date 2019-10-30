using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
	WALL,
	EMPTY,
	DOOR
}

public class Tile
{
	//the type of tile this is
	private TileType m_tileType;

	/// <summary>
	/// Gets the current tile type of this tile
	/// </summary>
	/// <returns>The tile type</returns>
	public TileType GetTileType() { return m_tileType; }

	/// <summary>
	/// Sets a new tile type for this tile, and respawns the tile to match the new tile type
	/// </summary>
	/// <param name="tile">The new type of tile</param>
	public void SetTileType(TileType tile)
	{
		m_tileType = tile;
		DespawnTile();
		SpawnTile();
	}

	//The wall prefab
	private GameObject m_wallPrefab;
	//The empty tile prefab
	private GameObject m_emptyPrefab;
	//The door prefab
	private GameObject m_doorPrefab;

	//the transform of the stage
	private Transform m_stageTransform;

	/// <summary>
	/// Sets all the prefabs
	/// </summary>
	/// <param name="wallPrefab"></param>
	/// <param name="emptyPrefab"></param>
	/// <param name="doorPrefab"></param>
	public void Create(GameObject wallPrefab, GameObject emptyPrefab, GameObject doorPrefab, Transform stage)
	{
		m_wallPrefab = wallPrefab;
		m_emptyPrefab = emptyPrefab;
		m_doorPrefab = doorPrefab;
		m_stageTransform = stage;
	}

	//The instantiated tile
	private GameObject m_spawnedTile;

	//the position of the tile
	private Vector3 m_position;

	/// <summary>
	/// Gets the current position of the tile
	/// </summary>
	/// <returns>The position</returns>
	public Vector3 GetPosition() { return m_position; }

	/// <summary>
	/// Sets a new position for the tile
	/// </summary>
	/// <param name="newPos">The new position</param>
	public void SetPosition(Vector3 newPos) { m_position = newPos; }

	/// <summary>
	/// Spawns the initial tile
	/// </summary>
	public void SpawnTile()
	{
		switch(m_tileType)
		{
			case (TileType.WALL):
				m_spawnedTile = Object.Instantiate(m_wallPrefab, m_position, Quaternion.identity, m_stageTransform);
				return;
			case (TileType.EMPTY):
				m_spawnedTile = Object.Instantiate(m_emptyPrefab, m_position, Quaternion.identity, m_stageTransform);
				return;
			case (TileType.DOOR):
				m_spawnedTile = Object.Instantiate(m_doorPrefab, m_position, Quaternion.identity, m_stageTransform);
				return;
		}
	}

	/// <summary>
	/// Despawns the tile
	/// </summary>
	public void DespawnTile()
	{
		Object.Destroy(m_spawnedTile);
	}
}
