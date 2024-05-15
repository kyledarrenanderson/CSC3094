using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
//TODO


public class DEMOEnemyAgent : MonoBehaviour
{
    //Unity NavMeshAgent
    UnityEngine.AI.NavMeshAgent agent;

    //GameObject Variables
    GameObject[] allNodes;
    GameObject[] friendlyAgents;
    GameObject[] enemyAgents;
    
    //LayerMask
    public LayerMask LevelComponents;
    public LayerMask AIAgents;

    Vector3 spawnPoint;

    public int agentID;

    //Boolean Variables
    bool inList;
    bool recalculatePath = false;
    bool validPath;

    //Integer Variables
    public int failedPathCount;
    public int health = 100;

    //Float Variables
    public float fieldOfView = 60;
    float newH;
    float newG;
    float newF;
    float checkVisible = 0;
    float respawnTimer = 0;

    //levelNode Variables
    levelNode randomNode;
    levelNode node;
    levelNode n;
    levelNode visibleNode;
    public levelNode destinationLocation = null;
    public levelNode start = null;
    public levelNode currentNode;
    public levelNode previousNode;
    

    //List<levelNode> Variables
    public List<levelNode> path = new List<levelNode>();
    List<levelNode> openList = new List<levelNode>();
    List<levelNode> closedList = new List<levelNode>();
    public List<levelNode> nodesToIgnore = new List<levelNode>();

    Vector3[] lineRenderPositions;
    public LineRenderer route;

    //Status for other agents to read to help them decide what to do0
    public enum Status
    {
        Moving, //agents roam randomly
        Fighting, //agents engage in combat
    }

    public Status currentStatus;

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

        openList.Clear();
        closedList.Clear();

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


                newH = HeuristicCalculator(n, destinationNode);
                newG = node.g + 1;
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

    bool canSeeAgents()
    {
        foreach(GameObject friendlyAgent in friendlyAgents)
        {
            if(Mathf.Abs(Vector3.Angle(transform.forward, friendlyAgent.transform.position - transform.position)) < fieldOfView)
            {
                RaycastHit info;
                if(Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), friendlyAgent.transform.position - transform.position, out info))
                {
                    DEMOStateMachineAgent a = info.collider.GetComponent<DEMOStateMachineAgent>();
                    if(a != null)
                    {
                        if(a.tag == "FriendlyAgentStateMachine")
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    //sets up variables for other functions
    void Start()
    {
        currentStatus = Status.Moving;
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        allNodes = GameObject.FindGameObjectsWithTag("levelNode");
        gameObject.tag = "temp";
        friendlyAgents = GameObject.FindGameObjectsWithTag("FriendlyAgentStateMachine");
        enemyAgents = GameObject.FindGameObjectsWithTag("EnemyAgent");
        gameObject.tag = ("EnemyAgent");
        destinationLocation = generateTargetLocation();
        agentID = gameObject.GetInstanceID();
        spawnPoint = transform.position;
    }

    void FixedUpdate()
    {
        if(currentStatus == Status.Moving)
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
                //generate a path from the current location
                RaycastHit info;
	            Vector3 direction = new Vector3(0, -1, 0);
	            if (Physics.Raycast(transform.position, direction, out info))
	            {
		            start = info.collider.GetComponent<levelNode>();
                    if(start == null)
                    {
                        start = previousNode;
                    }
                    if(currentNode == destinationLocation)
                    {
                        path.Clear();
                        destinationLocation = generateTargetLocation();
                        failedPathCount = 0;
                    }
	            }
                validPath = FindPath(start, destinationLocation);
                if(!validPath)
                {
                    failedPathCount += 1;
                }
                foreach(GameObject agent in enemyAgents)
                {
                    //logic to try and get an agent unstuck should it become trapped
                    DEMOEnemyAgent a = agent.GetComponent<DEMOEnemyAgent>();
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
            else if(validPath)
            {
                //detects all agents inside of a spherical radius of 5 to reduce comparisons done
                foreach(GameObject agent in enemyAgents)
                {
                    DEMOEnemyAgent a = agent.GetComponent<DEMOEnemyAgent>();
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
                            if(a.path[0] == path[0] && a.agentID < agentID )
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
                    recalculatePath = true;
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
                            currentNode.nodeType = levelNode.NodeType.Dangerous;
                            currentNode.shouldSpread = true;
                        }
                    }
                }
                catch
                {
                    //blank as this is a duct tape fix for an issue I couldnt solve
                } 
            }
            
        }
        else if(currentStatus == Status.Fighting)
        {
            path.Clear();
            agent.ResetPath();
        }
        //every 0.2 seconds run scans for whats in front of it
        checkVisible += Time.deltaTime;
        if(checkVisible > 0.2)
        {
            checkVisible = 0;
            if(canSeeAgents())
            {
                currentStatus = Status.Fighting;
            }
            else
            {
                currentStatus = Status.Moving;
            }
        }
        if(health < 0)
        {
            gameObject.transform.position = new Vector3(0,0,0);
            respawnTimer += Time.deltaTime;
            if(respawnTimer > 30)
            {
                gameObject.transform.position = spawnPoint;
                respawnTimer = 0;
                health = 100;
                previousNode = null;
            }
        }
    }
}