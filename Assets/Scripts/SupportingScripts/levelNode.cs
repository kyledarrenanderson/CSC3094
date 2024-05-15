using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//TODO

public class levelNode : MonoBehaviour
{
	//All Adjacent Nodes
	public List<levelNode> allAdjacent = new List<levelNode>();
	//All Visable Nodes
	public List<levelNode> allVisible = new List<levelNode>();
	//Cost of travelling over a node
	public float traversalCost;
	
	//A* Setup
	public float g;
	public float h;
	public levelNode parent;
	
	//LayerMask
	[SerializeField]
	public LayerMask levelSetup;

	//nodeType is used to store the current type of node that a node is
	public NodeType nodeType;

	//Raycast output variable
	RaycastHit info;

	//location of where a player camera should be relative to a node
	Vector3 playerCameraPosition;

	//used to manipulate other levelNode objects
	levelNode node;

	//list of all nodes in the level
	GameObject[] allNodes;

	//position of object from allNodes
	Vector3 objectPosition;

	//used to control spread of Dangerous nodes
	public bool shouldSpread = false;

	public NodeType previousType;

	//Nodetype Setup
	public enum NodeType
	{
		Safe, //enemy cannot be occupying or see this node
		Unsafe, //enemy cannot be occupying but may see this node
		Wall, //impassable wall node
		Obscuring, //node is passable but vision beyond it is blocked
		Damaging, //node is passable but additional health cost
		Dangerous, //node may be occupied by an enemy
	}

	//cardinal directions setup
	Vector3[] directions = {
	new Vector3(-1, 0, 0),
	new Vector3(1, 0, 0),
	new Vector3(0, 0, -1),
	new Vector3(0, 0, 1)
	};
	
	//checks to see if a node is a wall
	public bool IsWall()
	{
		Vector3 direction = new Vector3(0, 1, 0);
		if (Physics.Raycast(transform.position, direction, out info))
		{
			if(info.collider.tag == "Wall")
			{
				nodeType = NodeType.Wall;
				return true;
			}
		}
		return false;
	}

	public bool IsOccupied()
	{
		Vector3 direction = new Vector3(0, 1, 0);
		if (Physics.Raycast(transform.position, direction, out info))
		{
			if(info.collider.gameObject.layer == 3)
			{
				return true;
			}
		}
		return false;
	}

	
	public void SetDangerous()
	{
		nodeType = NodeType.Dangerous;
	}

	public void SetSafe()
	{
		nodeType = NodeType.Safe;
	}

	//generates f score for A*
	public float GenerateFScore(float g, float h)
	{
		return g + h;
	}

	//adds Adjacent nodes to allAdjacent
	void FindAdjacent ()
	{
		allAdjacent.Clear();
		foreach(Vector3 direction in directions)
		{
			if (Physics.Raycast(transform.position, direction, out info))
			{
				node = info.transform.gameObject.GetComponent<levelNode>();
				if(node)
				{
					allAdjacent.Add(node);
				}
			}
		}
	}

	//adds visible nodes to allVisible
	void FindVisibleNodes ()
	{
		if(allNodes != null)
		{
			playerCameraPosition = new Vector3(transform.position.x, transform.position.y + 3f, transform.position.z);
			foreach(GameObject nodeObject in allNodes)
			{
				objectPosition = new Vector3(nodeObject.transform.position.x, nodeObject.transform.position.y + 0.5f, nodeObject.transform.position.z);
				if(Physics.Raycast(playerCameraPosition, objectPosition - playerCameraPosition, out info, Mathf.Infinity, levelSetup))
				{
					node = info.transform.gameObject.GetComponent<levelNode>();
					if(node && !IsWall())
					{
						allVisible.Add(node);
					}
				}
			}
		}
	}

	//changes NodeType based on value of nodeType
	void SetupNode()
	{
		switch(nodeType)
		{
			case NodeType.Safe:
			{
				GetComponent<Renderer>().material.color = Color.green;
				traversalCost = 1;
				break;
			}

			case NodeType.Unsafe:
			{
				GetComponent<Renderer>().material.color = Color.yellow;
				traversalCost = 2;
				break;
			}

			case NodeType.Wall:
			{
				GetComponent<Renderer>().material.color = Color.black;
				traversalCost = 99999;
				GetComponent<UnityEngine.AI.NavMeshObstacle>().enabled = true;
				break;
			}

			case NodeType.Obscuring:
			{
				GetComponent<Renderer>().material.color = Color.grey;
				traversalCost = 2;
				break;
			}

			case NodeType.Damaging:
			{
				Color orange = new Color(1.0f, 0.3f, 0.1f, 1.0f);
				GetComponent<Renderer>().material.color = orange;
				traversalCost = 1;
				break;
			}

			case NodeType.Dangerous:
			{
				GetComponent<Renderer>().material.color = Color.red;
				traversalCost = 4;
				break;
			}

			//should never be seen, means there is an error in the logic
			default:
			{
				GetComponent<Renderer>().material.color = Color.magenta;
				traversalCost = 1;
				break;
			}
		}
	}

	//listens for eventhandler to invoke, spreads Dangerous value for nodeType to all adjacent nodes
	public void spreadDanger()
	{
		if(nodeType == NodeType.Dangerous && gameObject.tag != "Wall")
		{
			if(shouldSpread)
			{
				foreach(levelNode node in allAdjacent)
				{
					node.nodeType = NodeType.Dangerous;
					shouldSpread = false;
				}
			}
			shouldSpread = true;
		}
	}

	
	void Start()
	{
		//sets up level for pathfinding
		allNodes = GameObject.FindGameObjectsWithTag("levelNode");
		if(IsWall())
		{
			gameObject.tag = "Wall";
		}
		else
		{
			previousType = NodeType.Safe;
			gameObject.tag = "levelNode";
		}
		FindAdjacent();
		FindVisibleNodes();
	}

	void FixedUpdate()
	{
		//updates node to current nodeType
		SetupNode();
		//if this node is visible from a Dangerous node, update it to be unsafe, otherwise mark it as safe
		if(nodeType != NodeType.Dangerous)
		{
			foreach(levelNode node in allVisible)
			{
				if(node.nodeType == NodeType.Dangerous)
				{
					nodeType = NodeType.Unsafe;
					break;
				}
				else{
					nodeType = NodeType.Safe;
				}
			}
		}
		//reset all nodes be safe
		if(Input.GetKeyDown(KeyCode.C))
		{
			nodeType = NodeType.Safe;
			if(IsWall())
			{
				nodeType = NodeType.Wall;
			}
		}
	}
}