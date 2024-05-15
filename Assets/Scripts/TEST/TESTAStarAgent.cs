using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//TODO


public class TESTAStarAgent : MonoBehaviour
{
    //Unity NavMeshAgent
    UnityEngine.AI.NavMeshAgent agent;

    //GameObject Variables
    GameObject[] allNodes;
    
    //LayerMask
    public LayerMask LevelComponents;

    //Boolean Variables
    bool destF;
    bool inList;
    bool recalculatePath = false;
    public bool reachedDestination = false;

    //Integer Variables
    public int destinationCount;
    public int failureCount;

    //Float Variables
    public float fieldOfView = 45;
    float newH;
    float newG;
    float newF;
    public float totalTime;
    public float timeTaken;

    //levelNode Variables
    levelNode randomNode;
    levelNode node;
    levelNode n;
    levelNode visibleNode;
    public levelNode destination = null;
    public levelNode start = null;
    levelNode currentNode;

    //List<levelNode> Variables
    List<levelNode> path = new List<levelNode>();
    List<levelNode> openList;
    List<levelNode> closedList;

    //Picks a random noode from allNodes to be the destination of pathfinding
    levelNode generateTargetLocation()
    {
        if(allNodes != null)
        {
            randomNode = allNodes[Random.Range(0,allNodes.Length)].GetComponent<levelNode>();
        
            return randomNode;
        }
        return null;
    }

    //Calculates euclidian distance between node and destination node
	float HeuristicCalculator(levelNode n, levelNode destinationNode)
	{
		return(n.transform.position - destinationNode.transform.position).magnitude;
	}

    //A* algorithm to find shortest path to target node from start point
	public bool FindPath(levelNode startNode, levelNode destinationNode)
	{
		path.Clear();
        if(!startNode || !destinationNode)
        {
            return false;
        }
        recalculatePath = false;
        startNode.parent = null;
        startNode.g = 0.0f;
        startNode.h = HeuristicCalculator(startNode, destinationNode);

        openList = new List<levelNode>();
        closedList = new List<levelNode>();

        openList.Add(startNode);

        while(openList.Count > 0)
		{
			node = openList[0];
			for(int i = 1; i < openList.Count; ++i)
			{
				if(openList[i].GenerateFScore(openList[i].g, openList[i].h) < node.GenerateFScore(node.g, node.h))
				{
					node = openList[i];
				}
				if(node == destinationNode)
                {
                    while(node)
                    {
                        path.Add(node);
                        node.previousType = node.nodeType;
                        node = node.parent;
                    }
                    path.Reverse();
                    return true;
                }
			}

			foreach(levelNode n in node.allAdjacent)
            {
                if(closedList.Contains(n))
                {
                    continue;
                }
                
                if(n.IsWall())
                {
                    closedList.Add(n);
                    continue;
                }

                newH = HeuristicCalculator(n, destinationNode);
                newG = node.g + n.traversalCost;
                newF = newG + newH;
                inList = openList.Contains(n);

                if(newF < node.GenerateFScore(node.g, node.h) || !inList)
                {
                    if(!inList)
                    {
                        n.h = newH;
                        openList.Add(n);
                    }
                    n.g = newG;
                    n.h = newH;
                    n.parent = node;
                }
            }
            openList.Remove(node);
            closedList.Add(node);
		}
        return false;
	}

    //sets up variables for other functions
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        allNodes = GameObject.FindGameObjectsWithTag("levelNode");
    }

    void FixedUpdate()
    {
        if(destinationCount < 25)
        {
            //checks to see if a path exists to follow
            if(path.Count == 0)
            {
                RaycastHit info;
	            Vector3 direction = new Vector3(0, -1, 0);
	            if (Physics.Raycast(transform.position, direction, out info))
	            {
		            currentNode = info.collider.GetComponent<levelNode>();
                    if(currentNode == destination)
                    {
                        destinationCount += 1;
                        totalTime += timeTaken;
                        timeTaken = 0f;
                    }
	            }
                
                //if no path to follow generate a destination
                destF = false;
                if(!destF)
                {
                    destination = generateTargetLocation();
                    destF = true;
                }
                //generate a path from the current location
	            if (Physics.Raycast(transform.position, direction, out info))
	            {
		            start = info.collider.GetComponent<levelNode>();
                    FindPath(start, destination);
	            }
                
            }
            else if(timeTaken >= 15f)
            {
                path.Clear();
                totalTime += timeTaken;
                timeTaken = 0f;
                failureCount += 1;
                destinationCount += 1;
            }
            else
            {
                    
                agent.destination = new Vector3((path[0]).transform.position.x,(path[0]).transform.position.y + 1.5f, (path[0]).transform.position.z);
                //when the agent gets close enough to its target node, sets the next node in path as its target
                if(agent.remainingDistance < 0.5f)
                {
                    foreach(levelNode node in path)
                    {
                        if(node.nodeType != node.previousType)
                        {
                            recalculatePath = true;
                        }
                    }
                    if(recalculatePath)
                    {
                        RaycastHit info;
	                    Vector3 direction = new Vector3(0, -1, 0);
	                    if (Physics.Raycast(transform.position, direction, out info))
	                    {
		                    start = info.collider.GetComponent<levelNode>();
	                    }
                        FindPath(start, destination);
                    }
                    else
                    {
                        path.RemoveAt(0);
                            
                    }
                }
            }
        }
        else
        {
            reachedDestination = true;
            GetComponent<Renderer>().material.color = Color.black;
        }
        timeTaken += Time.deltaTime;
    }
}