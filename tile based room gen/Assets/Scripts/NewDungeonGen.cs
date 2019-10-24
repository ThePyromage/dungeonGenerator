using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
	UP,
	DOWN,
	LEFT,
	RIGHT,
	NULL
}

public struct Room
{
	public int xPos;
	public int yPos;
	public int width;
	public int height;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="x">The x position</param>
	/// <param name="y">The y position</param>
	/// <param name="newWidth">The width</param>
	/// <param name="newHeight">The height</param>
	public Room(int x, int y, int newWidth, int newHeight)
	{
		xPos = x;
		yPos = y;
		width = newWidth;
		height = newHeight;
	}

	/// <summary>
	/// Checks if two rooms are overlapping
	/// </summary>
	/// <param name="other">The other room to check against</param>
	/// <returns>Whether or not these rooms are overlapping</returns>
	public bool IsOverlapping(Room other)
	{
		if (xPos < other.xPos + other.width && yPos < other.yPos + other.height ||
		xPos + width > other.xPos && yPos < other.yPos + other.height ||
		xPos + width > other.xPos && yPos + height > other.yPos ||
		xPos < other.xPos + other.width && yPos + height > other.yPos)
			return true;
		else
			return false;
	}
}

public class NewDungeonGen : MonoBehaviour
{
	[Header("Dungeon Variables")]
	[Tooltip("How many times the generator will try to generate rooms before it stops")]
	[SerializeField] private int m_roomGenTries;
	[Tooltip("The inverse chance of adding a connector between two regions that have been joined already")]
	[SerializeField] private int m_extraConnectorChance;
	[Tooltip("The extra size a room may have over default")]
	[SerializeField] private int m_extraRoomSize;
	[Tooltip("TODO: REPLACE THIS TEXT WHEN I LEARN WHAT THIS VARIABLE DOES")]
	[SerializeField] private int m_windingPercent;

	[Header("Stage Variables")]
	[Tooltip("The level stage")]
	[SerializeField] private Stage m_stage;

	//array of rooms
	private List<Room> m_rooms = new List<Room>();

	//2d array of regions
	private int[,] m_regions;

	//the index of the current region being carved
	private int m_currentRegion = -1;

	public void Generate()
	{
		//stage must be odd sized
		if (m_stage.GetStageSize().x % 2 == 0 || m_stage.GetStageSize().y % 2 == 0)
			Debug.LogError("Stage must be odd-sized.");

		//sets all tiles to be walls
		m_stage.Initialise();

		//init regions
		m_regions = new int[m_stage.GetStageSize().x, m_stage.GetStageSize().y];

		//Create the rooms
		AddRooms();

		//Fill in all empty space with mazes
		for (int y = 1; y < m_stage.GetStageSize().y; y += 2)
		{
			for (int x = 1; x < m_stage.GetStageSize().x; x += 2)
			{
				Vector2Int pos = new Vector2Int(x, y);
				if (m_stage.m_tiles[x, y].GetTileType() != TileType.WALL)
					continue;

				//grows a maze at specified position
				GrowMaze(pos);
			}
		}

		//connect all the regions
		ConnectRegions();

		//lowers the amount of dead ends
		RemoveDeadEnds();
	}

	/// <summary>
	/// Places down rooms
	/// </summary>
	private void AddRooms()
	{
		for (int i = 0; i < m_roomGenTries; i++)
		{
			//pick a random room size
			int size = (Random.Range(1, 3 + m_extraRoomSize) * 2) + 1;

			//set how rectangular a room is
			int rectangularity = Random.Range(0, 1 + size / 2) * 2;

			//set width and height values
			int width = size;
			int height = size;

			//randomly increase either width or height by rectangularity value
			if (Random.Range(0, 2) == 0)
				width += rectangularity;
			else
				height += rectangularity;

			//set random position for room
			int x = Random.Range(1, (m_stage.GetStageSize().x - width) / 2) * 2 + 1;
			int y = Random.Range(1, (m_stage.GetStageSize().y - height) / 2) * 2 + 1;

			//create room
			Room room = new Room(x, y, width, height);

			//check if room overlaps with any existing rooms
			bool overlaps = false;
			foreach (Room other in m_rooms)
			{
				if(room.IsOverlapping(other))
				{
					overlaps = true;
					break;
				}
			}

			//if it overlaps, exit this gen attempt
			if (overlaps)
				continue;

			//if it did not overlap, add room to room list
			m_rooms.Add(room);

			//start a new region for the room
			StartRegion();

			//carve out the room
			for (int x2 = 0; x2 < room.width; x2++)
			{
				for (int y2 = 0; y2 < room.height; y2++)
				{
					Carve(new Vector2Int(room.xPos + x2, room.yPos + y2));
				}
			}
		}
	}

	/// <summary>
	/// Maze growing algorithm
	/// </summary>
	/// <param name="position">The position to start growing a maze from</param>
	private void GrowMaze(Vector2Int position)
	{
		//list for cells
		List<Vector2Int> cells = new List<Vector2Int>();
		//the last direction this maze generated in
		Direction lastDir = Direction.NULL;

		//start a new region for the maze
		StartRegion();

		//carve out the starting position
		Carve(position);

		//add starting position to cell list
		cells.Add(position);

		//while there are still cells left unmazed
		while(cells.Count != 0)
		{
			//get a cell from the list of cells
			Vector2Int cell = cells[cells.Count - 1];

			//create list of directions from this cell
			List<Direction> unmadeCells = new List<Direction>();

			//add directions of carvable cells
			for(int i = 0; i < 4; i++)
			{
				if (CanCarve(cell, (Direction)i))
					unmadeCells.Add((Direction)i);
			}

			//init direction marker
			Direction dir = Direction.NULL;

			//if there are carvable directions, select one to carve in
			if (unmadeCells.Count != 0)
			{
				//based on how windy passages are, try to prefer carving in the same direction
				if (unmadeCells.Contains(lastDir) && Random.Range(0, 100) > m_windingPercent)
					dir = lastDir;
				else
					dir = unmadeCells[Random.Range(0, unmadeCells.Count)];

				//carve out two tiles in the specified direction and add the tile two units in the specified direction to the cells list
				switch(dir)
				{
					case (Direction.UP):
						Carve(new Vector2Int(cell.x, cell.y + 1));
						Carve(new Vector2Int(cell.x, cell.y + 2));
						cells.Add(new Vector2Int(cell.x, cell.y + 2));
						break;
					case (Direction.DOWN):
						Carve(new Vector2Int(cell.x, cell.y - 1));
						Carve(new Vector2Int(cell.x, cell.y - 2));
						cells.Add(new Vector2Int(cell.x, cell.y - 2));
						break;
					case (Direction.LEFT):
						Carve(new Vector2Int(cell.x - 1, cell.y));
						Carve(new Vector2Int(cell.x - 2, cell.y));
						cells.Add(new Vector2Int(cell.x + 2, cell.y));
						break;
					case (Direction.RIGHT):
						Carve(new Vector2Int(cell.x + 1, cell.y));
						Carve(new Vector2Int(cell.x + 2, cell.y));
						cells.Add(new Vector2Int(cell.x - 2, cell.y));
						break;
				}
				
				//set last direction to direction
				lastDir = dir;
			}
			//if there are no carvable directions
			else
			{
				//remove cell
				cells.RemoveAt(cells.Count - 1);

				//path has ended
				lastDir = Direction.NULL;
			}
		}
	}

	/// <summary>
	/// Connects all the regions
	/// </summary>
	private void ConnectRegions()
	{
		Dictionary<Vector2Int, List<int>> connectorRegions = new Dictionary<Vector2Int, List<int>>();
		Vector2Int bounds = m_stage.GetStageSize();
		bounds.x -= 1;
		bounds.y -= 1;
		for(int x = 1; x < bounds.x; x++)
		{
			for(int y = 1; y < bounds.y; y++)
			{
				Vector2Int pos = new Vector2Int(x, y);
				//if position is a wall, skip this position
				if (m_stage.m_tiles[x, y].GetTileType() == TileType.WALL)
					continue;

				List<int> regions = new List<int>();

				//check cardinal directions
				for (int i = 0; i < 4; i++)
				{
					Direction dir = (Direction)i;
					//set region to null value
					int region = -1;
					switch (dir)
					{
						case (Direction.UP):
							region = m_regions[pos.x, pos.y + 1];
							break;
						case (Direction.DOWN):
							region = m_regions[pos.x, pos.y - 1];
							break;
						case (Direction.LEFT):
							region = m_regions[pos.x - 1, pos.y];
							break;
						case (Direction.RIGHT):
							region = m_regions[pos.x + 1, pos.y];
							break;
					}
					//if region is not null value and list does not already contain the region, add it to the list
					if(region != -1 && !regions.Contains(region))
					{
						regions.Add(region);
					}
				}

				//if there are less than two regions, continue the loop
				if (regions.Count < 2) continue;

				//otherwise, add region to connector region dictionary
				connectorRegions[pos] = regions;
			}
		}

		//add all connection points to a list
		List<Vector2Int> connectors = new List<Vector2Int>();
		foreach(Vector2Int connector in connectorRegions.Keys)
		{
			connectors.Add(connector);
		}

		//keep track of which regions have been merged
		int[] merged = new int[m_currentRegion];
		List<int> openRegions = new List<int>();

		for(int i = 0; i <= m_currentRegion; i++)
		{
			merged[i] = i;
			if(!openRegions.Contains(i))
				openRegions.Add(i);
		}

		//keep connecting regions until we're down to one
		while(openRegions.Count > 1)
		{
			Vector2Int connectionPoint = connectors[Random.Range(0, connectors.Count)];

			//carve the connection
			CarveJunction(connectionPoint);

			//merge the connected regions
			//we'll pick one region arbitrarily and map all other regions to it's index
			List<int> regions = connectorRegions[connectionPoint];
			
		}
	}

	private void RemoveDeadEnds()
	{

	}

	/// <summary>
	/// Starts a new region
	/// </summary>
	private void StartRegion()
	{
		m_currentRegion++;
	}

	/// <summary>
	/// Checks if a certain tile is carvable
	/// </summary>
	/// <param name="pos">The position of the tile</param>
	/// <param name="dir">The direction of the carved tile from the given position</param>
	/// <returns>Whether this tile is carvable or not</returns>
	private bool CanCarve(Vector2Int pos, Direction dir)
	{
		switch(dir)
		{
			case (Direction.UP):
				if (!(pos.x > 0 && pos.y > 0 && pos.y + 3 < m_stage.GetStageSize().y && pos.x < m_stage.GetStageSize().x))
					return false;
				return m_stage.m_tiles[pos.x, pos.y + 2].GetTileType() == TileType.WALL;
			case (Direction.DOWN):
				if (!(pos.x > 0 && pos.y - 3 > 0 && pos.y < m_stage.GetStageSize().y && pos.x < m_stage.GetStageSize().x))
					return false;
				return m_stage.m_tiles[pos.x, pos.y - 2].GetTileType() == TileType.WALL;
			case (Direction.LEFT):
				if (!(pos.x - 3 > 0 && pos.y > 0 && pos.y < m_stage.GetStageSize().y && pos.x < m_stage.GetStageSize().x))
					return false;
				return m_stage.m_tiles[pos.x - 2, pos.y].GetTileType() == TileType.WALL;
			case (Direction.RIGHT):
				if (!(pos.x > 0 && pos.y > 0 && pos.y < m_stage.GetStageSize().y && pos.x + 3 < m_stage.GetStageSize().x))
					return false;
				return m_stage.m_tiles[pos.x + 2, pos.y].GetTileType() == TileType.WALL;
		}
		return false;
	}

	/// <summary>
	/// Carves a door
	/// </summary>
	/// <param name="pos">The position of the door</param>
	/// <param name="vertical">Whether or not the door is facing up/down</param>
	private void CarveJunction(Vector2Int pos)
	{
		m_stage.m_tiles[pos.x, pos.y].SetTileType(TileType.DOOR);
	}

	/// <summary>
	/// Carves out a new tile, making it empty
	/// </summary>
	/// <param name="pos">The position of the tile to carve</param>
	private void Carve(Vector2Int pos)
	{
		m_stage.m_tiles[pos.x, pos.y].SetTileType(TileType.EMPTY);
		m_regions[pos.x, pos.y] = m_currentRegion;
	}

	/// <summary>
	/// Carves out a new tile, setting it to the specified type
	/// </summary>
	/// <param name="pos">The position of the tile to carve</param>
	/// <param name="type">The type of tile this is</param>
	private void Carve(Vector2Int pos, TileType type)
	{
		//if specified type is wall, don't carve it out
		if (type == TileType.WALL) return;
		m_stage.m_tiles[pos.x, pos.y].SetTileType(type);
		m_regions[pos.x, pos.y] = m_currentRegion;
	}
}