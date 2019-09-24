using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct RoomPosition
{
	public int x;
	public int y;
}

public class DungeonGenerator : MonoBehaviour
{
	[Header("Dungeon variables")]
	[Tooltip("How tall this dungeon is")]
	[SerializeField] private int m_dungeonHeight;
	[Tooltip("How wide this dungeon is")]
	[SerializeField] private int m_dungeonWidth;
	[Tooltip("The position of the start room")]
	[SerializeField] private RoomPosition m_startPos;
	[Tooltip("The position of the end room")]
	[SerializeField] private RoomPosition m_endPos;

	[Header("Dungeon room variables")]
	[Tooltip("The list of dungeon room prefabs")]
	[SerializeField] private DungeonRoom[] m_dungeonPrefabs;
	[Tooltip("The start room prefab")]
	[SerializeField] private DungeonRoom m_startRoomPrefab;
	[Tooltip("The end room prefab")]
	[SerializeField] private DungeonRoom m_endRoomPrefab;
	[Tooltip("The fallback room prefab, used in case of no other valid room types")]
	[SerializeField] private DungeonRoom m_fallbackRoom;
	[Tooltip("How wide the rooms are")]
	[SerializeField] private float m_roomWidth;
	[Tooltip("How tall the rooms are")]
	[SerializeField] private float m_roomHeight;

	//The list of dungeon rooms
	private DungeonRoom[,] m_dungeonRooms;

	/// <summary>
	/// Called on object awake
	/// </summary>
	private void Awake()
	{
		//initialise rooms array
		m_dungeonRooms = new DungeonRoom[m_dungeonWidth, m_dungeonHeight];

		//check if start position is correct
		if (m_startPos.x == m_endPos.x && m_startPos.y == m_endPos.y)
		{
			Debug.LogError("Start pos and end pos should not be the same!");
			return;
		}
		//sets the start room
		m_dungeonRooms[m_startPos.x, m_startPos.y] = m_startRoomPrefab;
		//sets the end room
		m_dungeonRooms[m_endPos.x, m_endPos.y] = m_endRoomPrefab;

		GenerateRoomLayout(m_startPos);
	}

	/// <summary>
	/// Generates a room layout based on the position provided
	/// </summary>
	/// <param name="position"></param>
	private void GenerateRoomLayout(RoomPosition position)
	{
		ExitType northExit = m_dungeonRooms[position.x, position.y].GetExitType(Direction.NORTH);
		ExitType eastExit = m_dungeonRooms[position.x, position.y].GetExitType(Direction.EAST);
		ExitType southExit = m_dungeonRooms[position.x, position.y].GetExitType(Direction.SOUTH);
		ExitType westExit = m_dungeonRooms[position.x, position.y].GetExitType(Direction.WEST);

		//check if north exit is valid and this is not the top of the map
		if (northExit != ExitType.NO_EXIT && position.y < m_dungeonHeight - 1)
		{
			//check if room is already generated
			if(m_dungeonRooms[position.x, position.y].GetGenerated(Direction.NORTH))
			{
				DungeonRoom northRoom = GenerateRoom(Direction.SOUTH, northExit);
				if(northRoom != null)
				{
					m_dungeonRooms[position.x, position.y + 1] = northRoom;
					m_dungeonRooms[position.x, position.y].SetGenerated(Direction.NORTH);
					m_dungeonRooms[position.x, position.y + 1].SetGenerated(Direction.SOUTH);
				}
			}
		}
		
	}

	/// <summary>
	/// Generates a room from the prefab list for a given direction and exit type
	/// </summary>
	/// <param name="direction">The direction the specified exit type must be in</param>
	/// <param name="exit">The kind of exit this room must have</param>
	/// <returns>The dungeon room prefab</returns>
	private DungeonRoom GenerateRoom(Direction direction, ExitType exit)
	{
		for(int i = 0; i < m_dungeonPrefabs.Length; i++)
		{
			if(m_dungeonPrefabs[i].GetExitType(direction) == exit)
			{
				return m_dungeonPrefabs[i];
			}
		}
		//if no valid rooms, return fallback room
		return m_fallbackRoom;
	}


}
