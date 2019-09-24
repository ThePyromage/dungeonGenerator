using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExitType
{
	///DO NOT EDIT THIS ENTRY
	NO_EXIT,		//no exit on this edge
	///You can edit the following entries
	CENTER_EXIT,	//one centred exit on this edge
	DOUBLE_EXIT,	//two exits on this edge
	LEFT_EXIT,		//one left exit on this edge
	RIGHT_EXIT,		//one right exit on this edge
}

//DO NOT EDIT THIS ENUM
public enum Direction
{
	NORTH,
	EAST,
	SOUTH,
	WEST
}

public class DungeonRoom : MonoBehaviour
{
	[Header("Exits")]
	[Tooltip("The type of north exit on this room")]
	[SerializeField] private ExitType m_northExit;
	[Tooltip("The type of east exit on this room")]
	[SerializeField] private ExitType m_eastExit;
	[Tooltip("The type of south exit on this room")]
	[SerializeField] private ExitType m_southExit;
	[Tooltip("The type of west exit on this room")]
	[SerializeField] private ExitType m_westExit;

	//Whether or not the respective exits have been generated or not yet
	private bool m_northGenerated = false;
	private bool m_eastGenerated = false;
	private bool m_southGenerated = false;
	private bool m_westGenerated = false;

	/// <summary>
	/// Gets the exit type of the specified direction
	/// </summary>
	/// <param name="direction">The direction</param>
	/// <returns>The exit type of the specified direction</returns>
	public ExitType GetExitType(Direction direction)
	{
		switch(direction)
		{
			case (Direction.NORTH):
				return m_northExit;
			case (Direction.EAST):
				return m_eastExit;
			case (Direction.SOUTH):
				return m_southExit;
			case (Direction.WEST):
				return m_westExit;
			default:
				return ExitType.NO_EXIT;
		}
	}

	/// <summary>
	/// Sets a direction to be generated
	/// </summary>
	/// <param name="direction"></param>
	public void SetGenerated(Direction direction)
	{
		switch(direction)
		{
			case (Direction.NORTH):
				m_northGenerated = true;
				return;
			case (Direction.EAST):
				m_eastGenerated = true;
				return;
			case (Direction.SOUTH):
				m_southGenerated = true;
				return;
			case (Direction.WEST):
				m_westGenerated = true;
				return;
			default:
				return;
		}
	}

	/// <summary>
	/// Gets the generated state of a particular direction
	/// </summary>
	/// <param name="direction">The direction to get</param>
	/// <returns>The generated state</returns>
	public bool GetGenerated(Direction direction)
	{
		switch (direction)
		{
			case (Direction.NORTH):
				return m_northGenerated;
			case (Direction.EAST):
				return m_eastGenerated;
			case (Direction.SOUTH):
				return m_southGenerated;
			case (Direction.WEST):
				return m_westGenerated;
			default:
				Debug.LogError("Invalid direction!");
				return true;
		}
	}
}
