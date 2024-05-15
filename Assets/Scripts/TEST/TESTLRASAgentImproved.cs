using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

//Current Issues
//When lots of agents are stuck without a path framerate dips as they all recalculate constantly
//Limitation using unity NavMeshAgent
//Some agents will path back and forth from the same 2 nodes
//TODO


public class TESTLRASAgentImproved : MonoBehaviour
{
    //Unity NavMeshAgent
    UnityEngine.AI.NavMeshAgent agent;

    //GameObject Variables
    GameObject[] allNodes;
    GameObject[] friendlyAgents;
    
    //LayerMask
    public LayerMask LevelComponents;
    public LayerMask AIAgents;

    //Boolean Variables
    bool inList;
    public bool recalculatePath = false;
    public bool validPath;
    public bool reachedDestination = false;

    //Integer Variables
    public int destinationCount;
    public int failureCount;
    public int agentID;
    public int failedPathCount;

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
    public levelNode destinationLocation = null;
    public levelNode start = null;
    public levelNode currentNode;
    public levelNode previousNode;
    levelNode finalNode;

    //List<levelNode> Variables
    public List<levelNode> path = new List<levelNode>();
    List<levelNode> openList;
    List<levelNode> closedList;
    List<levelNode> nodesToIgnore = new List<levelNode>();
    List<levelNode> finalNodes = new List<levelNode>();

    

    //Status for other agents to read to help them decide what to do0
    public enum Status
    {
        Waiting,
        Moving,
        Fighting,
        Hiding,
    }

    Status currentStatus;

    //Picks a random noode from allNodes to be the destination of pathfinding
    levelNode generateTargetLocation()
    {
        if(allNodes != null)
        {
            randomNode = allNodes[UnityEngine.Random.Range(0,allNodes.Length)].GetComponent<levelNode>();
        
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
                    nodesToIgnore.Clear();
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

                if(nodesToIgnore.Count > 0)
                {
                    if(nodesToIgnore.Contains(n))
                    {
                        nodesToIgnore.Remove(n);
                        closedList.Add(n);
                        continue;
                    }
                }

                if(finalNodes.Count > 0)
                {
                    if(finalNodes.Contains(n))
                    {
                        closedList.Add(n);
                        continue;
                    }
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
        gameObject.tag = "temp";
        friendlyAgents = GameObject.FindGameObjectsWithTag("FriendlyAgentTestLRAStarV2");
        gameObject.tag = ("FriendlyAgentTestLRAStarV2");
        destinationLocation = generateTargetLocation();
        agentID = Mathf.Abs(gameObject.GetInstanceID());
    }

    void FixedUpdate()
    {
        if(destinationCount < 25)
        {
            if(agent.remainingDistance > 2f)
            {
                agent.destination = transform.position;
                recalculatePath = true;
            }
            if(destinationLocation.nodeType == levelNode.NodeType.Wall)
            {
                destinationLocation = generateTargetLocation();
            }
            
            //checks to see if a path exists to follow
            else if(path.Count == 0 || !validPath)
            {
                RaycastHit info;
	            Vector3 direction = new Vector3(0, -1, 0);
	            if (Physics.Raycast(transform.position, direction, out info))
	            {
		            start = info.collider.GetComponent<levelNode>();
                    if(currentNode == destinationLocation)
                    {
                        path.Clear();
                        destinationCount += 1;
                        totalTime += timeTaken;
                        timeTaken = 0f;
                        destinationLocation = generateTargetLocation();
                    }
	            }
                validPath = FindPath(start, destinationLocation);
                if(!validPath)
                {
                    failedPathCount += 1;
                }
                foreach(GameObject agent in friendlyAgents)
                {
                    //logic to try and get an agent unstuck should it become trapped
                    TESTLRASAgentImproved a = agent.GetComponent<TESTLRASAgentImproved>();
                    if(failedPathCount > 50 && a.path.Count > 0)
                    {
                        if(currentNode == a.path[0] && currentNode)
                        {
                            foreach(levelNode node in currentNode.allAdjacent)
                            {
                                if(!(node.nodeType == levelNode.NodeType.Wall || node.IsOccupied()))
                                {
                                    path.Insert(0, node);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            //if a path exists, follows
            else if(timeTaken >= 40f)
            {
                path.Clear();
                totalTime += timeTaken;
                timeTaken = 0f;
                failureCount += 1;
                destinationCount += 1;
            }
            else if(validPath)
            {
                //checks to see if the cost of any nodes on its path have changed
                foreach(levelNode node in path)
                {
                    if(node.nodeType != node.previousType)
                    {
                        //if a node has changed its value, recalculate the path and break from the loop as there is no point checking later nodes on its path
                        recalculatePath = true;
                        break;
                    }
                }
                //detects all agents inside of a spherical radius of 5 to reduce comparisons done
                foreach(GameObject agent in friendlyAgents)
                {
                    TESTLRASAgentImproved a = agent.GetComponent<TESTLRASAgentImproved>();
                    if(a.reachedDestination && !finalNodes.Contains(a.finalNode))
                    {
                        finalNodes.Add(a.finalNode);
                    }
                    if(a.path.Count > 1 && path.Count > 1)
                    {
                        if((a.transform.position - transform.position).magnitude < 6)
                        {
                            if(a.path.Count > 7 && path.Count > 7)
                            {
                                //checks for agents pathing opposite directions into each other, accounts for both even and odd number of nodes apart
                                //the agent with the lowest id gets priority for pathfinding
                                //if there is not a valid path for an agent with a lower priority then an agent will find a new route
                                for(int i = 0; i < 6; i++)
                                {    
                                    if((a.path[i] == path[5-i] || a.path[i] == path[6-i]) && a.agentID < agentID)
                                    {
                                        nodesToIgnore.Add(path[i]);
                                    }
                                    else
                                    {
                                        nodesToIgnore.Clear();
                                    }
                                }
                                //Checks up to 6 nodes in advance for collisions
                                for(int i = 0; i < 6; i++)
                                {
                                    //add any potential collisions to nodesToIgnore
                                    if(a.path[i] == path[i] && a.agentID < agentID)
                                    {
                                        nodesToIgnore.Add(path[i]);
                                    }
                                }
                            }
                            //backup check from original LRAS
                            if(((a.path[0] == path[0]) && a.agentID < agentID))
                            {
                                nodesToIgnore.Add(path[0]);
                            }
                            if(((path[0] == a.previousNode && a.path[0] == previousNode) && a.agentID < agentID))
                            {
                                nodesToIgnore.Add(path[0]);
                            }
                            if((a.path[0] == path[0]) && (!(a.validPath)))
                            {
                                nodesToIgnore.Add(path[0]);
                            }
                        }
                        //edge cases where multiple agents have the same destination and reach it at the same time
                        if(path.Count == 1)
                        {
                            if(a.path[0] == path[0] && (a.agentID < agentID || a.reachedDestination))
                            {
                                start = destinationLocation;
                                nodesToIgnore.Clear();
                                break;
                            }
                        }
                    }
                }
                
                if(nodesToIgnore.Count > 0 || recalculatePath)
                {
                    validPath = FindPath(currentNode, destinationLocation);
                }
            }
                //when the agent gets close enough to its target node, sets the next node in path as its target
            if(path.Count > 0 && validPath)
            {
                try
                {
                    agent.destination = new Vector3((path[0]).transform.position.x,(path[0]).transform.position.y + 1.5f, (path[0]).transform.position.z);
                    if(agent.remainingDistance < 0.5f)
                    {
                        previousNode = path[0];
                        path.RemoveAt(0);
                        if(path.Count > 0)
                        {
                            currentNode = path[0];
                        }
                    }
                }
                catch
                {
                    //blank as this is a duct tape fix for an issue I couldnt solve
                } 
            }
        }
        else
        {
            validPath = false;
            reachedDestination = true;
            GetComponent<Renderer>().material.color = Color.black;
            if(path.Count == 0)
            {
                finalNode = currentNode;
                if(path.Count > 0)
                {
                    agent.destination = new Vector3((finalNode).transform.position.x,(finalNode).transform.position.y + 1.5f, (finalNode).transform.position.z);
                }
                currentNode = finalNode;
                previousNode = finalNode;
            }
        }
        timeTaken += Time.deltaTime;
        
    }
}