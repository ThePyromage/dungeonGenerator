using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#region Enums and Structs

public enum Direction
{
	UP,
	DOWN,
	LEFT,
	RIGHT,
	NULL
}

public struct Directions
{
	public static Vector2Int up = new Vector2Int(0, 1);
	public static Vector2Int down = new Vector2Int(0, -1);
	public static Vector2Int left = new Vector2Int(-1, 0);
	public static Vector2Int right = new Vector2Int(1, 0);
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
	public static bool operator ==(Room a, Room b)
	{
		if (a.xPos == b.xPos && a.yPos == b.yPos && a.width == b.width && a.height == b.height)
			return true;
		else
			return false;
	}

	public static bool operator !=(Room a, Room b)
	{
		if (a.xPos == b.xPos && a.yPos == b.yPos && a.width == b.width && a.height == b.height)
			return false;
		else
			return true;
	}

	public override bool Equals(object obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	/// <summary>
	/// Checks if two rooms are overlapping
	/// </summary>
	/// <param name="other">The other room to check against</param>
	/// <returns>Whether or not these rooms are overlapping</returns>
	public bool IsOverlapping(Room other)
	{
		if (xPos < other.xPos + other.width && yPos < other.yPos + other.height &&
		xPos + width > other.xPos && yPos < other.yPos + other.height &&
		xPos + width > other.xPos && yPos + height > other.yPos &&
		xPos < other.xPos + other.width && yPos + height > other.yPos)
			return true;
		else
			return false;
	}
}

#endregion
public class NewDungeonGen : MonoBehaviour
{
	[Header("Dungeon Variables")]
	[Tooltip("How many times the generator will try to generate rooms before it stops")]
	[SerializeField] private uint m_roomGenTries;
	[Tooltip("The inverse chance of adding a connector between two regions that have been joined already")]
	[SerializeField] [Range(0, 100)] private int m_extraConnectorChance;
	[Tooltip("The extra size a room may have over default")]
	[SerializeField] private int m_extraRoomSize;
	[Tooltip("The chance a corridoor will be windy")]
	[SerializeField] [Range(0, 100)] private int m_windingPercent;

	[Header("Stage Variables")]
	[Tooltip("The stage")]
	[SerializeField] private Stage m_stage;

	//array of rooms
	private List<Room> m_rooms = new List<Room>();

	//2d array of regions
	//private int[,] m_regions;
	private Dictionary<Vector2Int, int> m_regions;

	//the index of the current region being carved
	private int m_currentRegion = -1;

	private void Awake()
	{
		Generate();
	}

	/// <summary>
	/// Generates a level
	/// </summary>
	public void Generate()
	{
		//stage must be odd sized
		if (m_stage.GetStageSize().x % 2 == 0 || m_stage.GetStageSize().y % 2 == 0)
			Debug.LogError("Stage must be odd-sized.");

		//sets all tiles to be walls
		m_stage.Initialise();

		//init regions
		m_regions = new Dictionary<Vector2Int, int>()/*[m_stage.GetStageSize().x, m_stage.GetStageSize().y]*/;
		for (int regionY = 0; regionY < m_stage.GetStageSize().y; regionY++)
		{
			for(int regionX = 0; regionX < m_stage.GetStageSize().x; regionX++)
			{
				m_regions[new Vector2Int(regionX, regionY)] = -1;
			}
		}

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
			int x = Random.Range(0, (m_stage.GetStageSize().x - width) / 2) * 2 + 1;
			int y = Random.Range(0, (m_stage.GetStageSize().y - height) / 2) * 2 + 1;

			//create room
			Room room = new Room(x, y, width, height);

			//check if room overlaps with any existing rooms
			bool overlaps = false;
			foreach (Room other in m_rooms)
			{
				if(room.IsOverlapping(other) && room != other)
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
		while(cells.Count > 0)
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
						cells.Add(new Vector2Int(cell.x - 2, cell.y));
						break;
					case (Direction.RIGHT):
						Carve(new Vector2Int(cell.x + 1, cell.y));
						Carve(new Vector2Int(cell.x + 2, cell.y));
						cells.Add(new Vector2Int(cell.x + 2, cell.y));
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
		Dictionary<Vector2Int, HashSet<int>> connectorRegions = new Dictionary<Vector2Int, HashSet<int>>();

		for(int x = 1; x < m_stage.GetStageSize().x - 1; x++)
		{
			for(int y = 1; y < m_stage.GetStageSize().y - 1; y++)
			{
				Vector2Int pos = new Vector2Int(x, y);

				//if position is not a wall, skip this position
				if (m_stage.m_tiles[x, y].GetTileType() != TileType.WALL)
					continue;

				HashSet<int> regions = new HashSet<int>();

				//check cardinal directions
				for (int i = 0; i < 4; i++)
				{
				Direction dir = (Direction)i;
					//set region to null value
					int region = -1;
					switch (dir)
					{
						case (Direction.UP):
							region = m_regions[pos + Directions.up];
							break;
						case (Direction.DOWN):
							region = m_regions[pos + Directions.down];
							break;
						case (Direction.LEFT):
							region = m_regions[pos + Directions.left];
							break;
						case (Direction.RIGHT):
							region = m_regions[pos + Directions.right];
							break;
					}
					//if region is not null value, add it to the list
					if(region != -1)
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
		List<Vector2Int> connectors = connectorRegions.Keys.ToList();

		//keep track of which regions have been merged
		//int[] merged = new int[m_currentRegion];
		Dictionary<int, int> merged = new Dictionary<int, int>();

		for(int i = 0; i <= m_currentRegion; i++)
		{
			merged[i] = i;
		}

		//keep connecting regions until we're down to one
		while(connectors.Count > 1)
		{
			Vector2Int connectionPoint = connectors[Random.Range(0, connectors.Count)];

			//merge the connected regions
			//we'll pick one region arbitrarily and map all other regions to it's index
			var regions = connectorRegions[connectionPoint].Select((region) => merged[region]);


			int dest = regions.First();
			var sources = regions.Skip(1).ToList();
			
			//carve the connection
			CarveJunction(connectionPoint, dest);

			//merge all affected regions
			//all regions must be looked at because other regions might've been merged with some of the ones we're merging now
			for(int i = 0; i <= m_currentRegion; i++)
			{
				if(sources.Contains(merged[i]))
				{
					merged[i] = dest;
				}
			}

			//remove unneeded connectors
			connectors.RemoveAll(delegate (Vector2Int pos)
			{
				//if the connector doesn't span different regions, we don't need it
				//var regions2 = connectorRegions[pos].Select((region) => merged[region]).ToHashSet();
				HashSet<int> regions2 = new HashSet<int>(from x in connectorRegions[pos].Select((region) => merged[region]) select x);

				if (regions2.Count > 1) return false;

				//if the connector isn't needed, connect it occasionally anyway so the dungeon isn't singly-connected
				if(Random.Range(0, 100) < m_extraConnectorChance)
				{
					//don't allow connectors right next to each other
					//check cardinal directions
					for (int i = 0; i < 4; i++)
					{
						Direction dir = (Direction)i;
						switch (dir)
						{
							case (Direction.UP):
								if (m_stage.m_tiles[pos.x, pos.y + 1].GetTileType() == TileType.DOOR)
									return true;
								break;
							case (Direction.DOWN):
								if (m_stage.m_tiles[pos.x, pos.y - 1].GetTileType() == TileType.DOOR)
									return true;
								break;
							case (Direction.LEFT):
								if (m_stage.m_tiles[pos.x - 1, pos.y].GetTileType() == TileType.DOOR)
									return true;
								break;
							case (Direction.RIGHT):
								if (m_stage.m_tiles[pos.x + 1, pos.y].GetTileType() == TileType.DOOR)
									return true;
								break;
						}
					}

					//if no connectors are adjacent, add an additional connector
					CarveJunction(pos, dest);
				}

				return true;
			});
		}
	}

	/// <summary>
	/// Flood fills from a point with the region of the position provided
	/// </summary>
	/// <param name="pos">The point to flood fill from</param>
	private void FloodFillRegion(Vector2Int pos)
	{
		if (m_regions[pos] == -1) //if region of position is -1, this position is a wall, and we should not flood fill from here
			return;

		Queue<Vector2Int> tilesList = new Queue<Vector2Int>();

		int region = m_regions[pos];

		tilesList.Enqueue(pos);

		//loops through the adjacent tiles from the point until there are no further points remaining
		while(tilesList.Count > 0)
		{
			Vector2Int currentPosition = tilesList.Dequeue();
			//loops through directions
			for(int i = 0; i < 4; i++)
			{
				switch(i)
				{
					case (0): //up
						int upRegion = m_regions[currentPosition + Directions.up];
						if (upRegion == region) //if region is the same, don't do anything further
							continue;
						else if (upRegion == -1) //if region is -1, this is a wall
							continue;
						else //if region isn't the same, enqueue it
						{
							tilesList.Enqueue(currentPosition + Directions.up);
							m_regions[currentPosition + Directions.up] = region;
						}
						break;
					case (1): //down
						int downRegion = m_regions[currentPosition + Directions.down];
						if (downRegion == region) //if region is the same, don't do anything further
							continue;
						else if (downRegion == -1) //if region is -1, this is a wall
							continue;
						else //if region isn't the same, enqueue it and modify the region
						{
							tilesList.Enqueue(currentPosition + Directions.down);
							m_regions[currentPosition + Directions.down] = region;
						}
						break;
					case (2): //left
						int leftRegion = m_regions[currentPosition + Directions.left];
						if (leftRegion == region) //if region is the same, don't do anything further
							continue;
						else if (leftRegion == -1) //if region is -1, this is a wall
							continue;
						else //if region isn't the same, enqueue it
						{
							tilesList.Enqueue(currentPosition + Directions.left);
							m_regions[currentPosition + Directions.left] = region;
						}
						break;
					case (3): //right
						int rightRegion = m_regions[currentPosition + Directions.right];
						if (rightRegion == region) //if region is the same, don't do anything further
							continue;
						else if (rightRegion == -1) //if region is -1, this is a wall
							continue;
						else //if region isn't the same, enqueue it
						{
							tilesList.Enqueue(currentPosition + Directions.right);
							m_regions[currentPosition + Directions.right] = region;
						}
						break;
				}
			}
		}
	}

	/// <summary>
	/// Removes the dead ends of the dungeon
	/// </summary>
	private void RemoveDeadEnds()
	{
		bool done = false;

		Vector2Int bounds = m_stage.GetStageSize();
		bounds.x -= 1;
		bounds.y -= 1;
		
		//loops through the list of tiles until there is no more dead ends remaining
		while(!done)
		{
			done = true;
			//loops through all the tiles except the edge tiles
			for (int x = 1; x < bounds.x; x++)
			{
				for (int y = 1; y < bounds.y; y++)
				{
					//if tile is a wall, continue
					if (m_stage.m_tiles[x, y].GetTileType() == TileType.WALL)
						continue;

					//if there's only one exit, it's a dead end
					int exits = 0;

					for (int i = 0; i < 4; i++)
					{
						Direction dir = (Direction)i;

						//if tile type of the tile in the specified direction is not a wall, it is a potential exit
						switch (dir)
						{
							case (Direction.UP):
								if (m_stage.m_tiles[x, y + 1].GetTileType() != TileType.WALL)
									exits++;
								break;
							case (Direction.DOWN):
								if (m_stage.m_tiles[x, y - 1].GetTileType() != TileType.WALL)
									exits++;
								break;
							case (Direction.LEFT):
								if (m_stage.m_tiles[x - 1, y].GetTileType() != TileType.WALL)
									exits++;
								break;
							case (Direction.RIGHT):
								if (m_stage.m_tiles[x + 1, y].GetTileType() != TileType.WALL)
									exits++;
								break;
						}
					}

					//if there is more than 1 exit, this is not a dead end
					if (exits > 1)
						continue;

					//otherwise, we are not yet done
					done = false;
					//fill in this tile
					Carve(new Vector2Int(x, y), TileType.WALL);
				}
			}
		}
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
				if (!(pos.x > 0 && pos.y > 0 && pos.y + 2 < m_stage.GetStageSize().y && pos.x < m_stage.GetStageSize().x))
					return false;
				return m_stage.m_tiles[pos.x, pos.y + 2].GetTileType() == TileType.WALL;
			case (Direction.DOWN):
				if (!(pos.x > 0 && pos.y - 2 > 0 && pos.y < m_stage.GetStageSize().y && pos.x < m_stage.GetStageSize().x))
					return false;
				return m_stage.m_tiles[pos.x, pos.y - 2].GetTileType() == TileType.WALL;
			case (Direction.LEFT):
				if (!(pos.x - 2 > 0 && pos.y > 0 && pos.y < m_stage.GetStageSize().y && pos.x < m_stage.GetStageSize().x))
					return false;
				return m_stage.m_tiles[pos.x - 2, pos.y].GetTileType() == TileType.WALL;
			case (Direction.RIGHT):
				if (!(pos.x > 0 && pos.y > 0 && pos.y < m_stage.GetStageSize().y && pos.x + 2 < m_stage.GetStageSize().x))
					return false;
				return m_stage.m_tiles[pos.x + 2, pos.y].GetTileType() == TileType.WALL;
		}
		return false;
	}

	/// <summary>
	/// Carves a door
	/// </summary>
	/// <param name="pos">The position of the door</param>
	/// <param name="region"></param>
	private void CarveJunction(Vector2Int pos, int region)
	{
		m_stage.m_tiles[pos.x, pos.y].SetTileType(TileType.DOOR);
		m_regions[pos] = region;
	}

	/// <summary>
	/// Carves out a new tile, making it empty
	/// </summary>
	/// <param name="pos">The position of the tile to carve</param>
	private void Carve(Vector2Int pos)
	{
		m_stage.m_tiles[pos.x, pos.y].SetTileType(TileType.EMPTY);
		m_regions[pos] = m_currentRegion;
	}

	/// <summary>
	/// Carves out a new tile, setting it to the specified type
	/// </summary>
	/// <param name="pos">The position of the tile to carve</param>
	/// <param name="type">The type of tile this is</param>
	private void Carve(Vector2Int pos, TileType type)
	{
		m_stage.m_tiles[pos.x, pos.y].SetTileType(type);
		m_regions[pos] = m_currentRegion;
	}
}